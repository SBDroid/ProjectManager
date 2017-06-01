Imports System.IO
Imports MasterLib.ProcessManagement

Public Module ModMaster

#Region "Declarations"

    Private Const CHROME_DL_EXT As String = ".CRDOWNLOAD"

    ''' <summary>
    ''' Provides status messages that indicate the currently running/last completed actions.
    ''' </summary>
    Private NotInheritable Class StatusMessage

        Private Sub New()
        End Sub

        Public Const InfoEnterInputDirectory As String = "Enter the input directory containing incomplete Chrome downloads: "
        Public Const InfoShutdownScheduled As String = "Shutdown scheduled. Press Enter to abort..."
        Public Const ErrorInvalidParameters As String = "Invalid input parameters, please enter the directory again or N for exit:"
        Public Const ErrorScheduleFailed As String = "Shutdown couldn't be scheduled."

    End Class

#End Region

#Region "Main"

    ''' <summary>
    ''' Schedules a system shutdown when all Chrome downloads are completed.
    ''' </summary>
    Sub Main()
        UpdateConsole(StatusMessage.InfoEnterInputDirectory, False)
        Dim inDir As String = Console.ReadLine()
        While Not Directory.Exists(inDir) OrElse Directory.GetFiles(inDir).Where(Function(fName) Path.GetExtension(fName).ToUpper.Equals(CHROME_DL_EXT)).Count = 0
            UpdateConsole(StatusMessage.ErrorInvalidParameters, True)
            inDir = Console.ReadLine()
            If inDir?.ToUpper.Equals("N") Then
                Exit Sub
            End If
        End While

        AddHandler ShutdownScheduler.OnStatusChanged, AddressOf UpdateConsole

        ShutdownScheduler.FileExtensionShutdown(inDir, CHROME_DL_EXT)
        If ShutdownScheduler.IsShutdownScheduled Then
            UpdateConsole($" {StatusMessage.InfoShutdownScheduled}", False)
            Console.ReadLine()
            ShutdownScheduler.AbortShutdown()
        Else
            UpdateConsole($" {StatusMessage.ErrorScheduleFailed}", True)
        End If
    End Sub

#End Region

#Region "Internal"

    Private Sub UpdateConsole(message As String, isErr As Boolean)
        Console.WriteLine($"{Date.Now.ToString("[HH:mm:ss]")} {message}")
    End Sub

#End Region

End Module