Imports System.IO
Imports MasterLib.FilesystemManagement

Public Module ModMaster

#Region "Declarations"

    Private Const LOG_DIR As String = "Logs"
    Private Const LOG_UNICODE_TAGS As String = "UnicodeTags.txt"
    Private Const LOG_MISSING_TAGS As String = "MissingTags.txt"
    Private Const LOG_FILES_TO_TAGS As String = "FilesToTags.txt"
    Private Const TRAKTOR_INTERMEDIATE_NML As String = "AtKGP.nml"
    Private Const TRAKTOR_FINAL_NML As String = "CleanForImport.nml"
    Private Const EXT_NML As String = ".NML"
    Private Const EXT_MP3 As String = ".MP3"
    Private Const EXT_FLAC As String = ".FLAC"
    Private Const EXT_M4A As String = ".M4A"

    ''' <summary>
    ''' Provides status messages that indicate the currently running/last completed actions.
    ''' </summary>
    Private NotInheritable Class StatusMessage

        Private Sub New()
        End Sub

        Public Const InfoEnterInputDirectory As String = "Enter the input directory containing all media files: "
        Public Const InfoDisplayIntermediateLogs As String = "Display file names that differ to the artist and title ID3 tags (Y/N): "
        Public Const InfoRepeatRename As String = "Repeat renaming procedures (Y/N): "
        Public Const InfoLoadInTraktor As String = "Load the files in Traktor, add genre, analyze them and modify their ID3 tags, if needed. Export them as a playlist and press Enter to continue..."
        Public Const InfoEnterTraktorFile As String = "Enter the exported Traktor NML playlist file: "
        Public Const InfoTraktorNMLGenerated As String = "The NML playlist file has been generated: "
        Public Const InfoTraktorImportNML As String = "Import it to Traktor and set the ID3 tags from it. Press Enter to continue..."
        Public Const InfoDisplayFinalLogs As String = "Display files with Unicode/Missing ID3 tags (Y/N):"
        Public Const InfoFormattingFinished As String = "Formatting finished!"
        Public Const ErrorInvalidInputDirectory As String = "Input directory was invalid, please enter again or N for exit: "
        Public Const ErrorInvalidChoice As String = "Choice was invalid, please enter again (Y/N): "
        Public Const ErrorInvalidTraktorFile As String = "Input NML file was invalid, please enter again or N for exit: "

    End Class

#End Region

#Region "Main"

    ''' <summary>
    ''' Formats media files and analyzes the results. Resulting file names as well as ID3 tags will be irreversibly modified!
    ''' </summary>
    Sub Main()
        UpdateConsole(StatusMessage.InfoEnterInputDirectory, False)
        Dim inDir As String = Console.ReadLine()
        While Not Directory.Exists(inDir) OrElse Directory.GetFiles(inDir).Where(Function(fName) {EXT_MP3, EXT_FLAC, EXT_M4A}.Contains(Path.GetExtension(fName).ToUpper)).Count = 0
            UpdateConsole(StatusMessage.ErrorInvalidInputDirectory, True)
            inDir = Console.ReadLine()
            If inDir?.ToUpper.Equals("N") Then
                Exit Sub
            End If
        End While

        If Not Directory.Exists($"{My.Application.Info.DirectoryPath}\{LOG_DIR}") Then
            Directory.CreateDirectory($"{My.Application.Info.DirectoryPath}\{LOG_DIR}")
        End If

        AddHandler MediaFileEditor.OnStatusChanged, AddressOf UpdateConsole

        MediaFileEditor.SetTracksFilename(inDir) ' Step 0
        MediaFileEditor.ClearTracksExtraTags(inDir) ' Step 1
        MediaFileEditor.SetTracksArtistAndTitleTagsFromFilename(inDir) ' Step 2
        MediaFileEditor.FormatTracksArtistAndTitleTags(inDir) ' Step 3
        MediaFileEditor.SetTracksCommentTags(inDir) ' Step 4

        Dim stepFlag As Boolean = True
        While True
            UpdateConsole(StatusMessage.InfoDisplayIntermediateLogs, False)
            Dim resp As String = Console.ReadLine
            While Not resp?.ToUpper.Equals("Y") AndAlso Not resp?.ToUpper.Equals("N")
                UpdateConsole(StatusMessage.ErrorInvalidChoice, True)
                resp = Console.ReadLine
            End While

            If resp?.ToUpper.Equals("Y") Then
                My.Computer.FileSystem.WriteAllText($"{My.Application.Info.DirectoryPath}\{LOG_DIR}\{LOG_FILES_TO_TAGS}", MediaFileEditor.GetTracksWithDifferentFilenameToTags(inDir), False)
                Dim psi As ProcessStartInfo = New ProcessStartInfo()
                psi.UseShellExecute = False
                psi.CreateNoWindow = True
                psi.FileName = "notepad.exe"
                psi.Arguments = $"{My.Application.Info.DirectoryPath}\{LOG_DIR}\{LOG_FILES_TO_TAGS}"
                Process.Start(psi)

                UpdateConsole(StatusMessage.InfoRepeatRename, False)
                Dim innerResp As String = Console.ReadLine
                While Not innerResp?.ToUpper.Equals("Y") AndAlso Not innerResp?.ToUpper.Equals("N")
                    UpdateConsole(StatusMessage.ErrorInvalidChoice, True)
                    innerResp = Console.ReadLine
                End While

                If innerResp?.ToUpper.Equals("Y") Then
                    MediaFileEditor.SetTracksFilename(inDir)
                    MediaFileEditor.SetTracksArtistAndTitleTagsFromFilename(inDir)
                    MediaFileEditor.FormatTracksArtistAndTitleTags(inDir)
                End If
            Else
                If stepFlag Then
                    UpdateConsole(StatusMessage.InfoLoadInTraktor, False) ' Step 5, 6
                    Console.ReadLine()
                    stepFlag = False
                Else
                    Exit While
                End If
            End If
        End While

        UpdateConsole(StatusMessage.InfoEnterTraktorFile, False)
        Dim inNML As String = Console.ReadLine()
        While Not File.Exists(inNML) OrElse Not Path.GetExtension(inNML).ToUpper.Equals(EXT_NML)
            UpdateConsole(StatusMessage.ErrorInvalidTraktorFile, True)
            inNML = Console.ReadLine()
            If inNML?.ToUpper.Equals("N") Then
                Exit Sub
            End If
        End While

        AddHandler TraktorNMLEditor.OnStatusChanged, AddressOf UpdateConsole

        TraktorNMLEditor.SetAlbumToKeyGainPair(inNML, $"{My.Application.Info.DirectoryPath}\{TRAKTOR_INTERMEDIATE_NML}") ' Step 7
        TraktorNMLEditor.ClearTracksExtraTags($"{My.Application.Info.DirectoryPath}\{TRAKTOR_INTERMEDIATE_NML}", $"{Path.GetDirectoryName(inNML)}\{TRAKTOR_FINAL_NML}") ' Step 8
        File.Delete($"{My.Application.Info.DirectoryPath}\{TRAKTOR_INTERMEDIATE_NML}")

        UpdateConsole($"{StatusMessage.InfoTraktorNMLGenerated}{Path.GetDirectoryName(inNML)}\{TRAKTOR_FINAL_NML}", False)
        UpdateConsole(StatusMessage.InfoTraktorImportNML, False) ' Step 9
        Console.ReadLine()

        UpdateConsole(StatusMessage.InfoDisplayFinalLogs, False)
        Dim logResp As String = Console.ReadLine
        While Not logResp?.ToUpper.Equals("Y") AndAlso Not logResp?.ToUpper.Equals("N")
            UpdateConsole(StatusMessage.ErrorInvalidChoice, True)
            logResp = Console.ReadLine
        End While

        If logResp?.ToUpper.Equals("Y") Then
            My.Computer.FileSystem.WriteAllText($"{My.Application.Info.DirectoryPath}\{LOG_DIR}\{LOG_UNICODE_TAGS}", MediaFileEditor.GetTracksWithUnicodeTags(inDir), False) ' Step 10
            My.Computer.FileSystem.WriteAllText($"{My.Application.Info.DirectoryPath}\{LOG_DIR}\{LOG_MISSING_TAGS}", MediaFileEditor.GetTracksWithMissingTags(inDir), False) ' Step 11
            For Each logFile In {$"{My.Application.Info.DirectoryPath}\{LOG_DIR}\{LOG_UNICODE_TAGS}", $"{My.Application.Info.DirectoryPath}\{LOG_DIR}\{LOG_MISSING_TAGS}"}
                Dim psi As ProcessStartInfo = New ProcessStartInfo()
                psi.UseShellExecute = False
                psi.CreateNoWindow = True
                psi.FileName = "notepad.exe"
                psi.Arguments = logFile
                Process.Start(psi)
            Next
        End If

        UpdateConsole(StatusMessage.InfoFormattingFinished, False)
    End Sub

#End Region

#Region "Internal"

    Private Sub UpdateConsole(message As String, isErr As Boolean)
        Console.WriteLine($"{Date.Now.ToString("[HH:mm:ss]")} {message}")
    End Sub

#End Region

End Module