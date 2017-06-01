Namespace MemberOrganization

    ''' <summary>
    ''' Provides a structure for the main Organizer objects.
    ''' </summary>
    Public Class CompositeMember

        ''' <summary>
        ''' Needed for serialization/deserialization of CompositeMember objects.
        ''' </summary>
        Private Sub New()
        End Sub

        ''' <summary>
        ''' Creates a new CompositeMember. All newly created members are unchecked.
        ''' </summary>
        ''' <param name="memberName">The member's name.</param>
        ''' <param name="memberType">The member's content type.</param>
        ''' <param name="memberRelease">The member's release date is only required for manual members.</param>
        ''' <param name="memberFrequency">The member's repeatability frequency is only required for manual members of content type Series.</param>
        <ConstructorAlias(NameOf(CompositeMember))>
        Public Sub New(memberName As String, memberType As ContentType, Optional memberRelease As Date = Nothing, Optional memberFrequency As ManualFrequency = ManualFrequency.None)
            InternalID = "0" ' The internal ID is set to 0 by default.
            Name = memberName
            Type = memberType
            Release = IIf(memberRelease = Date.MinValue, New CompositeDate(0, 0, 0), New CompositeDate(memberRelease.Year, memberRelease.Month, memberRelease.Day))
            Deliveries = New List(Of CompositeDelivery)
            Frequency = memberFrequency
            ImageByteArr = Nothing
            Checked = False
        End Sub

        Public Property InternalID As String ' The ID identifies an automatic member uniquely by the corresponding API used. ID is 0 for manual members.
        Public Property Name As String ' All members require a name to be associated with.
        Public Property Type As ContentType ' All members require contet type to be associated with.
        Public Property Release As CompositeDate ' Release date. Used in both automatic and manual members.
        Public Property Deliveries As List(Of CompositeDelivery) ' Used in automatic/manual members of content type Series.
        Public Property Frequency As ManualFrequency ' The frequency determines the next manual delivery. Frequency is 0 (None) for automatic members.
        Public Property ImageByteArr As Byte() ' Image associated with the member (if any).
        Public Property Checked As Boolean
        ReadOnly Property CombinedType As CombinedType
            Get
                If InternalID = "0" Then
                    If Release.Complete Then
                        If Frequency = ManualFrequency.None Then
                            If Not Type = ContentType.Series Then Return CombinedType.ManualSingle
                        Else
                            If Type = ContentType.Series Then Return CombinedType.ManualRepeatable
                        End If
                    End If
                Else
                    If Frequency = ManualFrequency.None Then
                        If Type = ContentType.Series Then
                            Return CombinedType.AutomaticRepeatable
                        Else
                            Return CombinedType.AutomaticSingle
                        End If
                    End If
                End If
                Return CombinedType.Unknown
            End Get
        End Property

        Overrides Function Equals(obj As Object) As Boolean
            If obj Is Nothing OrElse Not obj.GetType = GetType(CompositeMember) Then
                Return False
            Else
                Dim cmObj As CompositeMember = CType(obj, CompositeMember)
                Return Name = cmObj.Name AndAlso Type = cmObj.Type
            End If
        End Function

    End Class

    ''' <summary>
    ''' Defines the content type of automatic/manual composite members.
    ''' </summary>
    Public Enum ContentType
        Game = 0
        Movie = 1
        Series = 2
    End Enum

    ''' <summary>
    ''' Defines the repeatability of manual series.
    ''' </summary>
    Public Enum ManualFrequency
        None = 0
        Weekly = 1
        Monthly = 2
        Yearly = 3
    End Enum

    ''' <summary>
    ''' Defines the type of the a composite member.
    ''' </summary>
    Public Enum CombinedType
        Unknown = 0 ' Invalid composite members.
        ManualSingle = 1 ' Identifies Games and Movies that were entered manually. (Automatic search has failed)
        ManualRepeatable = 2 ' Identifies Series that were entered manually. (Birthdays, conventions etc.)
        AutomaticSingle = 3 ' Identifies Games and Movies that were found by the automatic search.
        AutomaticRepeatable = 4 ' Identifies Series that were found by the automatic search. (TV series)
    End Enum

End Namespace