''' <summary>
''' Provides an ID and description by which the method implementing the attribute can be identified.
''' </summary>
<AttributeUsage(AttributeTargets.Method, AllowMultiple:=True)>
Class MethodAlias
    Inherits Attribute

    ReadOnly Property ID As String
    ReadOnly Property Description As String

    ''' <summary>
    ''' Requires the method's info, which contains its ID and its description. They are separated by a ';' symbol.
    ''' </summary>
    ''' <param name="mInfo">Method's info.</param>
    Public Sub New(mInfo As String)
        Dim infoArr() As String = mInfo.Split(";")
        ID = infoArr(0)
        Description = infoArr(1)
    End Sub

End Class
