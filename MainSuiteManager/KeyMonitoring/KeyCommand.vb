Namespace KeyMonitoring

    ''' <summary>
    ''' Provides a structure for key commands.
    ''' </summary>
    Public Class KeyCommand

        ''' <summary>
        ''' Needed for serialization/deserialization of KeyCommand objects.
        ''' </summary>
        Private Sub New()
        End Sub

        ''' <summary>
        ''' Requires the command type, its internal id and its action.
        ''' </summary>
        ''' <param name="commandType">Command type.</param>
        ''' <param name="commandID">Command ID.</param>
        ''' <param name="commandAction">Command action.</param>
        <ConstructorAlias(NameOf(KeyCommand))>
        Public Sub New(commandType As KeyCommandType, commandID As String, commandAction As String)
            Type = commandType
            ID = commandID.ToUpper
            Action = commandAction
        End Sub

        Public Property Type As KeyCommandType
        Public Property ID As String
        Public Property Action As String

        Overrides Function Equals(chObj As Object) As Boolean
            If chObj Is Nothing OrElse CType(chObj, KeyCommand) Is Nothing Then
                Return False
            Else
                Return ID.ToUpper = CType(chObj, KeyCommand).ID.ToUpper
            End If
        End Function

    End Class

    ''' <summary>
    ''' Defines the main command types.
    ''' </summary>
    Public Enum KeyCommandType
        TEXT = 0
        OS = 1
        FILE = 2
    End Enum

End Namespace