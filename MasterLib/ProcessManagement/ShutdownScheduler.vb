Imports System.IO
Imports System.Threading

Namespace ProcessManagement

    ''' <summary>
    ''' Schedules a system shutdown based on the specified user conditions.
    ''' </summary>
    Public NotInheritable Class ShutdownScheduler

#Region "Declarations"

        ''' <summary>
        ''' Provides status messages that indicate the currently running/last completed actions.
        ''' </summary>
        Private NotInheritable Class StatusMessage

            Private Sub New()
            End Sub

            Public Const InfoNoShutdownScheduled As String = "Unable to abort - no shutdown has been scheduled..."
            Public Const InfoShutdownAbortRequested As String = "Shutdown has been aborted by"
            Public Const InfoShutdownScheduled As String = "Waiting for shutdown conditions to be met..."
            Public Const ErrorShutdownScheduled As String = "Another shutdown has already been scheduled..."
            Public Const ErrorAnotherInstanceRunning As String = "Another instance of the application is running..."
            Public Const ErrorInputDirectory As String = "Invalid input directory or directory doesn't contain any files..."
            Public Const ErrorInputFiles As String = "The input directory doesn't contain files with the extension specified..."

        End Class

        Private Shared _workerTask As Task
        Private Shared _abortToken As CancellationTokenSource

        Shared Event OnStatusChanged(message As String, isError As Boolean)

        Shared ReadOnly Property IsShutdownScheduled As Boolean
            Get
                Return Not _workerTask Is Nothing AndAlso _workerTask.Status = TaskStatus.Running
            End Get
        End Property

#End Region

#Region "Main"

        ''' <summary>
        ''' Schedules a system shutdown that starts when the specified file extension is no longer present in the input directory.
        ''' </summary>
        ''' <param name="inputDir">Input directory, containing the files with the specified extension.</param>
        ''' <param name="fileExt">File extension.</param>
        Shared Sub FileExtensionShutdown(inputDir As String, fileExt As String)

            If Not fileExt.StartsWith(".") Then fileExt = $".{fileExt}"

            Select Case True
                Case IsShutdownScheduled
                    RaiseEvent OnStatusChanged(StatusMessage.ErrorShutdownScheduled, True)
                    Exit Sub
                Case ProcessAnalyzer.IsAnotherCurrentInstanceRunning
                    RaiseEvent OnStatusChanged(StatusMessage.ErrorAnotherInstanceRunning, True)
                    Exit Sub
                Case Not Directory.Exists(inputDir) OrElse Not Directory.GetFiles(inputDir).Count > 0
                    RaiseEvent OnStatusChanged(StatusMessage.ErrorInputDirectory, True)
                    Exit Sub
                Case Not Directory.GetFiles(inputDir).Where(Function(fName) Path.GetExtension(fName).ToUpper = fileExt.ToUpper).Count > 0
                    RaiseEvent OnStatusChanged(StatusMessage.ErrorInputFiles, True)
                    Exit Sub
            End Select

            Dim fExt As String = fileExt.ToUpper
            _abortToken = New CancellationTokenSource
            _workerTask = Task.Run(
                Sub()
                    Try
                        While Directory.GetFiles(inputDir).Where(Function(fName) Path.GetExtension(fName).ToUpper = fExt).Count > 0
                            Thread.Sleep(1000)
                            _abortToken.Token.ThrowIfCancellationRequested()
                        End While
                        Using proc As New Process
                            proc.StartInfo.FileName = "cmd.exe"
                            proc.StartInfo.Arguments = "/c shutdown.exe /s /f /t 0"
                            proc.StartInfo.Verb = "runas"
                            proc.StartInfo.UseShellExecute = True
                            proc.Start()
                            proc.WaitForExit()
                            proc.Close()
                        End Using
                    Catch ex As OperationCanceledException
                    End Try
                End Sub, _abortToken.Token)
            RaiseEvent OnStatusChanged(StatusMessage.InfoShutdownScheduled, False)
        End Sub

        ''' <summary>
        ''' Aborts a scheduled shutdown.
        ''' </summary>
        Shared Sub AbortShutdown()
            If IsShutdownScheduled AndAlso Not _abortToken Is Nothing Then
                _abortToken.Cancel(False)
                RaiseEvent OnStatusChanged($"{StatusMessage.InfoShutdownAbortRequested} {Environment.UserName}.", False)
            Else
                RaiseEvent OnStatusChanged(StatusMessage.InfoNoShutdownScheduled, False)
            End If
        End Sub

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