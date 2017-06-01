Imports System.ComponentModel
Imports System.Security.Principal

Namespace ProcessManagement

    ''' <summary>
    ''' Provides analytical data for the currently running processes.
    ''' </summary>
    Public NotInheritable Class ProcessAnalyzer

#Region "Declarations"

        ''' <summary>
        ''' Determines whether the current process was started with elevated privileges.
        ''' </summary>
        ''' <returns>Returns true if the current user is an administrator.</returns>
        Shared ReadOnly Property IsUserRoleAdmin As Boolean
            Get
                Return New WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator)
            End Get
        End Property

        ''' <summary>
        ''' Determines whether another instance of the current process calling the method is running.
        ''' </summary>
        ''' <returns>Returns true if an identical process is found.</returns>
        Shared ReadOnly Property IsAnotherCurrentInstanceRunning As Boolean
            Get
                Return Process.GetProcesses.Count(
                    Function(extProc)
                        Try
                            Return extProc.MainModule.FileVersionInfo?.ProductName = Process.GetCurrentProcess.MainModule.FileVersionInfo.ProductName
                        Catch ex As Win32Exception
                            Return False ' Processes with privileged access, 64 bit processes if current process is 32 bit are excluded
                        End Try
                    End Function) > 1
            End Get
        End Property

#End Region

#Region "Main"

        ''' <summary>
        ''' Determines whether another instance of the specified process is running. Compared against a Process object.
        ''' </summary>
        ''' <param name="prc">The specified process.</param>
        ''' <returns>Returns true if an identical process is found.</returns>
        Shared Function AreAnyInstancesRunning(prc As Process) As Boolean
            If Not prc Is Nothing AndAlso Not String.IsNullOrWhiteSpace(prc.StartInfo.FileName) Then
                Dim pName As String = FileVersionInfo.GetVersionInfo(prc.StartInfo.FileName).ProductName
                Return Process.GetProcesses.Count(
                    Function(extProc)
                        Try
                            Return extProc.MainModule.FileVersionInfo?.ProductName = pName
                        Catch ex As Win32Exception
                            Return False ' Processes with privileged access, 64 bit processes if current process is 32 bit are excluded.
                        End Try
                    End Function) > 0
            Else
                Return False
            End If
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


