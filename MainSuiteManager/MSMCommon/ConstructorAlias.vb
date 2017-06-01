''' <summary>
''' Provides an ID and description by which the constructor implementing the attribute can be identified.
''' </summary>
<AttributeUsage(AttributeTargets.Constructor)>
Class ConstructorAlias
    Inherits Attribute

    ReadOnly Property ID As String

    ''' <summary>
    ''' Requires the constructor's custom ID.
    ''' </summary>
    ''' <param name="cID">Constructor ID.</param>
    Public Sub New(cID As String)
        ID = cID
    End Sub

End Class