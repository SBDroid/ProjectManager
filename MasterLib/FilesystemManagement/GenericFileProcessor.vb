Imports System.IO

Namespace FilesystemManagement

    ''' <summary>
    ''' Analyzes file metadata and/or file names.
    ''' </summary>
    Public NotInheritable Class GenericFileProcessor

#Region "Declarations"

        ''' <summary>
        ''' Provides status messages that indicate the currently running/last completed actions.
        ''' </summary>
        Private NotInheritable Class StatusMessage

            Private Sub New()
            End Sub

            Public Const InfoFileExtensionChangeStarted As String = "Changing file extensions..."
            Public Const InfoFileExtensionChangeFinished As String = "File extensions changed successfully."
            Public Const ErrorInputDirectory As String = "Input directory doesn't exist..."
            Public Const ErrorFileExtension As String = "New file extension cannot be empty!"

        End Class

        Shared Event OnStatusChanged(message As String, isError As Boolean)

#End Region

#Region "Main"

        ''' <summary>
        ''' 
        ''' </summary>
        ''' <param name="inputDir"></param>
        Shared Sub DuplicateFileFinder(inputDir As String)
            ' Finds and moves all duplicate files found in a directory and all its sub-directories to a predefined destination.
            'TODO check if masterlib needs to be parametrisized
            '<MethodAlias(ConsoleController.KeyMonitorFileDuplicates)>
            ' TODO: method for safe getallsubfolders iteration /access denied/
            'Private Const DEFAULT_DUP_DIRECTORY As String = "Duplicates"


            'Dim dirFiles() As String = GetDirFiles(workDirectory, True, Nothing) ' All source directory files.
            'If dirFiles Is Nothing Then
            '    RaiseEvent OnStatusChanged(StatusMessage.ErrorFileOpDirectory, True)
            '    Exit Sub
            'End If

            'RaiseEvent OnStatusChanged(StatusMessage.FileDupOpStarted, False)
            'Dim dirDuplicates As String = $"{Path.GetFullPath(workDirectory)}_{DEFAULT_DUP_DIRECTORY}"
            'If Directory.Exists(dirDuplicates) Then
            '    Directory.Delete(dirDuplicates, True)
            'End If
            'Directory.CreateDirectory(dirDuplicates)

            'Dim dictSignatures As New Dictionary(Of FileInfo, String)
            'For Each fi As FileInfo In dirFiles.Select(Function(fStr As String) New FileInfo(fStr)).ToArray
            '    dictSignatures.Add(fi, $"{fi.Name}:{fi.Length}")
            'Next

            'For Each fkv As KeyValuePair(Of FileInfo, String) In dictSignatures
            '    If dictSignatures.Values.Where(Function(fileID) fileID = fkv.Value).ToArray.Count > 1 Then
            '        Dim movDirName As String = fkv.Key.Name ' The name of the directory that contains all identified duplicates of a single file.
            '        Dim oFileName As String = Path.GetFileNameWithoutExtension(fkv.Key.FullName) ' The original file name.
            '        Dim oFileExt As String = Path.GetExtension(fkv.Key.FullName) ' The original file's extension.
            '        Dim movFileName As String = oFileName ' The final file name.
            '        Dim movIdx As Integer = 0 ' Added index to the final file name.

            '        If Not Directory.Exists($"{dirDuplicates}\{movDirName}") Then
            '            Directory.CreateDirectory($"{dirDuplicates}\{movDirName}") ' Creating a directory for all the duplicates of a single file.
            '            Directory.CreateDirectory($"{dirDuplicates}\{movDirName}\{fkv.Key.Directory.Name}") ' Creating the first directory a duplicate file was found in.
            '        End If

            '        While File.Exists($"{dirDuplicates}\{movDirName}\{movFileName}{oFileExt}")
            '            movFileName = $"{oFileName}_{movIdx}"
            '            movIdx += 1
            '        End While
            '        File.Move(fkv.Key.FullName, $"{dirDuplicates}\{movDirName}\{movFileName}{oFileExt}")
            '    End If
            'Next
            'RaiseEvent OnStatusChanged(StatusMessage.FileDupsProcessed, False)
        End Sub

        ''' <summary>
        ''' Changes the extensions of all specified files in a directory.
        ''' </summary>
        ''' <param name="workDirectory">Input directory.</param>
        ''' <param name="newExtension">New file extension.</param>
        ''' <param name="fileFilter">Optional file filter. If not specified, all files in the input directory will be changed.</param>
        Shared Sub ChangeFileExtensionsInDirectory(workDirectory As String, newExtension As String, Optional fileFilter As String = "*.*")

            Select Case True
                Case Not Directory.Exists(workDirectory)
                    RaiseEvent OnStatusChanged(StatusMessage.ErrorInputDirectory, True)
                    Exit Sub
                Case String.IsNullOrWhiteSpace(newExtension)
                    RaiseEvent OnStatusChanged(StatusMessage.ErrorFileExtension, True)
                    Exit Sub
            End Select

            RaiseEvent OnStatusChanged(StatusMessage.InfoFileExtensionChangeStarted, False)
            Parallel.ForEach(
                Directory.GetFiles(workDirectory),
                Sub(fileStr As String)
                    Try
                        If Not Path.GetFileName(fileStr) Like fileFilter Then
                            RaiseEvent OnStatusChanged($"Skipped: '{fileStr}'", False)
                            Exit Sub
                        End If

                        If Not newExtension.StartsWith(".") Then
                            newExtension = "." & newExtension
                        End If

                        Dim newFileName As String = $"{Path.GetDirectoryName(fileStr)}\{Path.GetFileNameWithoutExtension(fileStr)}{newExtension.ToLower}"
                        If Not File.Exists(newFileName) Then
                            File.Move(fileStr, newFileName)
                        End If
                    Catch ex As Exception
                        RaiseEvent OnStatusChanged($"Error: '{ex.Message}'", True)
                    End Try
                End Sub)
            RaiseEvent OnStatusChanged(StatusMessage.InfoFileExtensionChangeFinished, False)
        End Sub

        ''' <summary>
        ''' Replaces all invalid characters in a string to enable its usage for a filename.
        ''' </summary>
        ''' <param name="fileName">File name to be cleaned.</param>
        ''' <returns>Returns the clean file name.</returns>
        Shared Function GetCleanFilenameFromString(fileName As String) As String
            Return Path.GetInvalidFileNameChars.Aggregate(fileName, Function(current, c) current.Replace(c.ToString(), String.Empty))
        End Function

#End Region

#Region "Internal"

        ''' <summary>
        ''' Cannot be initialized directly.
        ''' </summary>
        Private Sub New()
        End Sub

#End Region

    End Class

End Namespace


