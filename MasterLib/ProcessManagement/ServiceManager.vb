Imports System.ServiceProcess

Namespace ProcessManagement

    ''' <summary>
    ''' Controls Windows background services.
    ''' </summary>
    Public NotInheritable Class ServiceManager

#Region "Declarations"



#End Region

#Region "Main"

        ''' <summary>
        ''' Starts/Stops an installed Windows service by name.
        ''' </summary>
        ''' <param name="serviceName">Service name.</param>
        ''' <returns>Returns whether the service was stopped/started successfully.</returns>
        Shared Function WinServiceToggle(serviceName As String) As String
            Using sc As New ServiceController()
                sc.ServiceName = serviceName
                Try
                    If sc.Status = ServiceControllerStatus.Stopped Then
                        sc.Start()
                        sc.WaitForStatus(ServiceControllerStatus.Running)
                        Return $"Service {serviceName} started."
                    Else
                        If sc.CanStop Then
                            sc.Stop()
                            sc.WaitForStatus(ServiceControllerStatus.Stopped)
                            Return $"Service {serviceName} successfully stopped."
                        End If
                        Return $"Error: Service {serviceName} couldn't be stopped."
                    End If
                Catch ex As Exception
                    Return $"Error: Service {serviceName} not found or insufficient privileges."
                End Try
            End Using
        End Function

#End Region

#Region "Internal"



#End Region

    End Class

End Namespace
