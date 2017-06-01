Imports Microsoft.Win32

Namespace RegistryManagement

    ''' <summary>
    ''' Let's an application using the class automatically start on startup.
    ''' </summary>
    Public NotInheritable Class StartupManager

#Region "Declarations"

        Private Const REGISTRY_STARTUP_KEY As String = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run"

#End Region

#Region "Main"

        ''' <summary>
        ''' Determines whether MSM should start on Windows startup.
        ''' </summary>
        ''' <param name="startOnStartUp">Startup flag.</param>
        ''' <returns>Returns whether the operation was successful.</returns>
        Shared Function SetApplicationStartUp(startOnStartUp As Boolean) As String
            Dim appName As String = My.Application.Info.AssemblyName
            Using rkApp As RegistryKey = Registry.CurrentUser.OpenSubKey(REGISTRY_STARTUP_KEY, True)
                If startOnStartUp AndAlso rkApp.GetValue(appName) Is Nothing Then
                    rkApp.SetValue(appName, $"""{ My.Application.Info.DirectoryPath}\{appName}.exe""")
                ElseIf Not startOnStartUp AndAlso Not rkApp.GetValue(appName) Is Nothing Then
                    rkApp.DeleteValue(appName)
                End If
                rkApp.Close()
                Return $"{appName} will{IIf(startOnStartUp, " ", " not ")}start on startup."
            End Using
            Return $"Error: {appName} will{IIf(startOnStartUp, " not start ", " start again ")}on startup."
        End Function

#End Region

#Region "Internal"



#End Region

    End Class

End Namespace
