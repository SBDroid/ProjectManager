''' <summary>
''' Structure, used by operator classes that provides a conenction between an internal object and its corresponding XML file. 
''' </summary>
Class ObjectToFile(Of T, F)

    ReadOnly Property InternalObject As T
    ReadOnly Property FileIdentifier As F

    ''' <summary>
    ''' Requires the internal object and its corresponding XML file's identifier.
    ''' </summary>
    ''' <param name="obj">Internal object.</param>
    ''' <param name="fId">XML file indicator.</param>
    Public Sub New(obj As T, fId As F)
        InternalObject = obj
        FileIdentifier = fId
    End Sub

End Class
