Namespace MemberOrganization

    ''' <summary>
    ''' Provides a structure for release dates of automatic members. Can store incomplete dates.
    ''' </summary>
    Public Class CompositeDate
        Implements IComparable

        ''' <summary>
        ''' Needed for serialization/deserialization of CompositeDate objects.
        ''' </summary>
        Private Sub New()
        End Sub

        Public Sub New(y As Integer, m As Integer, d As Integer)
            Year = y
            Month = m
            Day = d
        End Sub

        Public Property Year As Integer
        Public Property Month As Integer
        Public Property Day As Integer
        ReadOnly Property Complete As Boolean
            Get
                Return Not Year = 0 AndAlso Not Month = 0 AndAlso Not Day = 0 ' If one of the CompositeDate fields is not set, the date is incomplete.
            End Get
        End Property

        Shared Widening Operator CType(dtStr As String) As CompositeDate
            Dim dtStd As Date = Format("dd/MM/yyyy", dtStr)
            Return New CompositeDate(dtStd.Year, dtStd.Month, dtStd.Day)
        End Operator

        Shared Widening Operator CType(dt As Date) As CompositeDate
            Return New CompositeDate(dt.Year, dt.Month, dt.Day)
        End Operator

        Shared Narrowing Operator CType(cdt As CompositeDate) As Date
            Return New Date(cdt.Year, cdt.Month, cdt.Day)
        End Operator

        Overrides Function ToString() As String
            Return $"{Day}/{Month}/{Year}"
        End Function

        Shared Operator <=(cd1 As CompositeDate, cd2 As CompositeDate) As Boolean
            Return cd1.CompareTo(cd2) <= 0
        End Operator

        Shared Operator >=(cd1 As CompositeDate, cd2 As CompositeDate) As Boolean
            Return cd1.CompareTo(cd2) >= 0
        End Operator

        Function CompareTo(cdObj As Object) As Integer Implements IComparable.CompareTo
            If cdObj Is Nothing OrElse Not cdObj.GetType = GetType(CompositeDate) Then
                Return 1
            End If
            Dim cdComp As CompositeDate = CType(cdObj, CompositeDate)
            If Year < cdComp.Year Then
                Return -1
            ElseIf Year = cdComp.Year Then
                If Month < cdComp.Month Then
                    Return -1
                ElseIf Month = cdComp.Month Then
                    If Day < cdComp.Day Then
                        Return -1
                    ElseIf Day = cdComp.Day Then
                        Return 0
                    Else
                        Return 1
                    End If
                Else
                    Return 1
                End If
            Else
                Return 1
            End If
        End Function

    End Class

End Namespace
