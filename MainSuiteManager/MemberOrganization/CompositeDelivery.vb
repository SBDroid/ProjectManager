Namespace MemberOrganization

    ''' <summary>
    ''' Provides a structure for deliveries of automatic/manual members of content type Series.
    ''' </summary>
    Public Class CompositeDelivery

        ''' <summary>
        ''' Needed for serialization/deserialization of CompositeDelivery objects.
        ''' </summary>
        Private Sub New()
        End Sub

        ''' <summary>
        ''' Creates a new CompositeDelivery. All newly created deliveries are unchecked.
        ''' </summary>
        ''' <param name="dlvName">The delivery's name.</param>
        ''' <param name="dlvSeason">The delivery's season.</param>
        ''' <param name="dlvEpisode">The delivery's episode.</param>
        ''' <param name="dlvRelease">The delivery's release date.</param>
        Public Sub New(dlvName As String, dlvSeason As Integer, dlvEpisode As Integer, dlvRelease As Date)
            Name = dlvName
            Season = dlvSeason
            Episode = dlvEpisode
            Release = dlvRelease
            Checked = False
        End Sub

        ''' <summary>
        ''' Creates a new CompositeDelivery. All newly created deliveries are unchecked.
        ''' </summary>
        ''' <param name="dlvName">The delivery's name.</param>
        ''' <param name="dlvRelease">The delivery's release.</param>
        Public Sub New(dlvName As String, dlvRelease As Date)
            Name = dlvName
            Season = 0
            Episode = 0
            Release = dlvRelease
            Checked = False
        End Sub

        Public Property Name As String
        Public Property Season As Integer
        Public Property Episode As Integer
        Public Property Release As Date
        Public Property Checked As Boolean

        Overrides Function Equals(obj As Object) As Boolean
            If obj Is Nothing OrElse Not obj.GetType = GetType(CompositeDelivery) Then
                Return False
            Else
                Dim cdObj As CompositeDelivery = CType(obj, CompositeDelivery)
                Return Name = cdObj.Name AndAlso Season = cdObj.Season AndAlso Episode = cdObj.Episode AndAlso Release = cdObj.Release
            End If
        End Function

    End Class

End Namespace