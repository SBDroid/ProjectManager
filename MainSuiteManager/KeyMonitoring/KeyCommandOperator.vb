Imports System.IO
Imports System.Reflection
Imports System.Xml.Serialization

Namespace KeyMonitoring

    ''' <summary>
    ''' Provides standard IO operations for key commands.
    ''' </summary>
    Public NotInheritable Class KeyCommandOperator

#Region "Declarations"

        Private Const COMMANDS_REPOSITORY As String = "Commands" ' The default command directory.
        Private Const COMMANDS_DEFAULT_NAME As String = "KeyCommand_" ' The default command prefix name.
        Private Const COMMAND_EXTENSION As String = ".xml" ' The default command extension.

        ''' <summary>
        ''' Provides status messages that indicate the currently running/last completed IO operations.
        ''' </summary>
        Private NotInheritable Class StatusMessage

            Private Sub New()
            End Sub

            Public Const CommandExists As String = "Command already exists! ID:"
            Public Const CommandNameEmpty As String = "Command ID shouldn't be empty!"
            Public Const CommandNameFail As String = "Command ID should not contain intervals!"
            Public Const CommandCreateSuccess As String = "command created successfully!"
            Public Const CommandNotFoundFail As String = "command not found!"
            Public Const CommandDeleteSuccess As String = "command deleted successfully!"
            Public Const CommandRestoreSuccess As String = "Command repository restored successfully!"
            Public Const CommandObjectEmpty As String = "Command object is empty!"

        End Class

        Private Shared _otfList As List(Of ObjectToFile(Of KeyCommand, Integer)) = LoadCommands(False) ' Contains all default and user KeyCommand XML files.

        Shared Event OnStatusChanged(statusMessage As String, err As Boolean) ' Provides status messages to the user of the KeyCommandOperator class.

        Shared ReadOnly Property ActiveCommandsList As List(Of KeyCommand)
            Get
                Return _otfList.Select(Function(otf As ObjectToFile(Of KeyCommand, Integer)) otf.InternalObject).ToList ' Returns only the KeyCommand objects.
            End Get
        End Property

#End Region

#Region "Main"

        ''' <summary>
        ''' Creates a new user KeyCommand and saves it to the command repository.
        ''' </summary>
        ''' <param name="keyCmd">The KeyCommand to be added.</param>
        <MethodAlias(ConsoleController.KeyCommandOperatorCreate)>
        Shared Sub CreateCommand(keyCmd As KeyCommand)
            ' Command integrity checks.
            Select Case True
                Case keyCmd Is Nothing
                    RaiseEvent OnStatusChanged(StatusMessage.CommandObjectEmpty, True) ' Command mustn't be empty.
                    Exit Sub
                Case String.IsNullOrWhiteSpace(keyCmd.ID)
                    RaiseEvent OnStatusChanged(StatusMessage.CommandNameEmpty, True) ' Command ID mustn't be empty.
                    Exit Sub
                Case keyCmd.ID.Contains(" ")
                    RaiseEvent OnStatusChanged(StatusMessage.CommandNameFail, True) ' Command mustn't have intervals.
                    Exit Sub
                Case _otfList.Any(Function(otf As ObjectToFile(Of KeyCommand, Integer)) otf.InternalObject.Equals(keyCmd))
                    RaiseEvent OnStatusChanged($"{StatusMessage.CommandExists} {keyCmd.ID}", True) ' Command shouldn't already exist.
                    Exit Sub
            End Select

            keyCmd.ID = keyCmd.ID.ToUpper
            Dim xmlSrl As XmlSerializer = New XmlSerializer(GetType(KeyCommand))
            Dim newMaxIdx As Integer = _otfList.Select(Function(kcXML As ObjectToFile(Of KeyCommand, Integer)) kcXML.FileIdentifier).ToArray.Max
            Dim newKeyCommandFile As String = ""

            Do
                newMaxIdx += 1
                newKeyCommandFile = $"{My.Application.Info.DirectoryPath}\{COMMANDS_REPOSITORY}\{COMMANDS_DEFAULT_NAME}{newMaxIdx}{COMMAND_EXTENSION}"
            Loop While File.Exists(newKeyCommandFile)

            Using fs As New FileStream(newKeyCommandFile, FileMode.Create)
                xmlSrl.Serialize(fs, keyCmd) ' Serializing the KeyCommand object.
                _otfList.Add(New ObjectToFile(Of KeyCommand, Integer)(keyCmd, newMaxIdx)) ' Actualizing the current ActiveCommandsList.
                fs.Close()
                RaiseEvent OnStatusChanged($"{keyCmd.ID} {StatusMessage.CommandCreateSuccess}", False)
            End Using
        End Sub

        ''' <summary>
        ''' Deletes a command from the repository. Default commands can always be restored.
        ''' </summary>
        ''' <param name="commandID">The KeyCommand to be removed.</param>
        <MethodAlias(ConsoleController.KeyCommandOperatorDelete)>
        Shared Sub DeleteCommand(commandID As String)
            Dim kcRem As ObjectToFile(Of KeyCommand, Integer) = _otfList.FirstOrDefault(
                Function(otf As ObjectToFile(Of KeyCommand, Integer)) otf.InternalObject.ID.Equals(commandID.ToUpper))
            If Not kcRem Is Nothing Then
                Dim fileRem As String = $"{My.Application.Info.DirectoryPath}\{COMMANDS_REPOSITORY}\{COMMANDS_DEFAULT_NAME}{kcRem.FileIdentifier}{COMMAND_EXTENSION}"
                If File.Exists(fileRem) Then
                    File.Delete(fileRem)
                End If
                _otfList.Remove(kcRem) ' Actualizing the current ActiveCommandsList.
                RaiseEvent OnStatusChanged($"{kcRem.InternalObject.ID} {StatusMessage.CommandDeleteSuccess}", False)
            Else
                RaiseEvent OnStatusChanged($"{kcRem.InternalObject.ID} {StatusMessage.CommandNotFoundFail}", True)
            End If
        End Sub

        ''' <summary>
        ''' Removes user commands, restoring the command repository to its default state.
        ''' </summary>
        <MethodAlias(ConsoleController.KeyCommandOperatorRestore)>
        Shared Sub RestoreDefaults()
            _otfList = LoadCommands(True) ' Load only the default key commands and clear all user ones.
            RaiseEvent OnStatusChanged(StatusMessage.CommandRestoreSuccess, False)
        End Sub

        ''' <summary>
        ''' Gets all active KeyCommand IDs.
        ''' </summary>
        ''' <returns>Returns a list of all commands.</returns>
        <MethodAlias(ConsoleController.KeyCommandOperatorListing)>
        Shared Function GetAllCommands()
            Return $"Full key command listing: {String.Join(", ", _otfList.Select(Function(otf) otf.InternalObject.ID))}"
        End Function

        ''' <summary>
        ''' Gets information about the specified command ID.
        ''' </summary>
        ''' <param name="commandID">The command ID.</param>
        ''' <returns>Returns the command's type and its action</returns>
        <MethodAlias(ConsoleController.KeyCommandOperatorHelp)>
        Shared Function GetCommandHelp(commandID As String)
            Dim resOtf = _otfList.FirstOrDefault(Function(otf As ObjectToFile(Of KeyCommand, Integer)) otf.InternalObject.ID = commandID.ToUpper)
            If Not resOtf Is Nothing Then
                Return $"{commandID} - Type: {resOtf.InternalObject.Type.ToString}, Action: '{resOtf.InternalObject.Action}'"
            Else
                Return $"{commandID} - {StatusMessage.CommandNotFoundFail}"
            End If
        End Function

        ''' <summary>
        ''' Releases all KeyCommandOperator resources.
        ''' </summary>
        Shared Sub Dispose()
            _otfList.Clear()
        End Sub

#End Region

#Region "Internal"

        ''' <summary>
        ''' Cannot be initialized directly.
        ''' </summary>
        Private Sub New()
        End Sub

        ''' <summary>
        ''' Loads all predefined and user key commands.
        ''' </summary>
        Private Shared Function LoadCommands(clearRepo As Boolean) As List(Of ObjectToFile(Of KeyCommand, Integer))
            Dim xmlSrl As XmlSerializer = New XmlSerializer(GetType(KeyCommand))
            Dim tempCmdList = New List(Of ObjectToFile(Of KeyCommand, Integer))

            ' Preparing the key command repository. Clear all contents, if required.
            If Not Directory.Exists($"{My.Application.Info.DirectoryPath}\{COMMANDS_REPOSITORY}") Then
                Directory.CreateDirectory($"{My.Application.Info.DirectoryPath}\{COMMANDS_REPOSITORY}")
            ElseIf clearRepo Then
                Directory.Delete($"{My.Application.Info.DirectoryPath}\{COMMANDS_REPOSITORY}", True)
                Directory.CreateDirectory($"{My.Application.Info.DirectoryPath}\{COMMANDS_REPOSITORY}")
            End If

            ' Generate any missing default key commands into the command repository.
            For Each cmdResName In Assembly.GetExecutingAssembly.GetManifestResourceNames().Where(
                Function(resObj As String)
                    Return resObj.EndsWith(COMMAND_EXTENSION) AndAlso resObj.Substring(resObj.IndexOf(".") + 1).StartsWith(COMMANDS_DEFAULT_NAME)
                End Function).ToArray
                Using rs = Assembly.GetExecutingAssembly.GetManifestResourceStream(cmdResName)
                    Dim fn As String = $"{My.Application.Info.DirectoryPath}\{COMMANDS_REPOSITORY}\{cmdResName.Substring(cmdResName.IndexOf(".") + 1)}"
                    If Not File.Exists(fn) Then
                        Using fs = New FileStream(fn, FileMode.Create, FileAccess.Write)
                            rs.CopyTo(fs) ' Saves all non-existing predefined key commands to the command repository as XML files.
                            fs.Close()
                        End Using
                    End If
                    rs.Close()
                End Using
            Next

            ' Deserializes all predefined and user KeyCommand XML files from the command repository.
            For Each cmdXmlFile In Directory.GetFiles($"{My.Application.Info.DirectoryPath}\{COMMANDS_REPOSITORY}").Where(
                    Function(fName As String)
                        If Path.GetExtension(fName).ToLower.Equals(COMMAND_EXTENSION) Then
                            Dim fn As String = Path.GetFileNameWithoutExtension(fName)
                            Return fn.StartsWith(COMMANDS_DEFAULT_NAME, StringComparison.Ordinal) AndAlso IsNumeric(fn.Substring(fn.LastIndexOf("_") + 1))
                        End If
                        Return False
                    End Function).ToArray
                Using cmdReader As New StreamReader(cmdXmlFile)
                    Dim fn As String = Path.GetFileNameWithoutExtension(cmdXmlFile)
                    Dim fIdx As Integer = fn.Substring(fn.LastIndexOf("_") + 1) ' Getting the command identifier from the XML file.
                    Dim kCmd As KeyCommand = CType(xmlSrl.Deserialize(cmdReader), KeyCommand) ' Deserializing the KeyCommand object.

                    If Not tempCmdList.Any(Function(otf As ObjectToFile(Of KeyCommand, Integer)) otf.InternalObject.Equals(kCmd)) Then
                        tempCmdList.Add(New ObjectToFile(Of KeyCommand, Integer)(kCmd, fIdx))
                    End If
                    cmdReader.Close()
                End Using
            Next
            Return tempCmdList
        End Function

#End Region

    End Class

End Namespace