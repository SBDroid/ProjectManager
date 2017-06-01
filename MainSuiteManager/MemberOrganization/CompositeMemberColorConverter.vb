Imports System.Globalization

Namespace MemberOrganization

    ''' <summary>
    ''' Provides a way to dynamically color Release cells of every CompositeMember/CompositeDelivery.
    ''' </summary>
    Class CompositeMemberColorConverter
        Implements IValueConverter

        Function Convert(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.Convert
            Dim cd As CompositeDate = value.Release
            Dim cdCalc As Date = New Date(IIf(cd.Year = 0, 1, cd.Year), IIf(cd.Month = 0, 1, cd.Month), IIf(cd.Day = 0, 1, cd.Day))
            Dim daysToRelease As Double = (cdCalc - Date.Now).TotalDays
            If daysToRelease < 0 Then
                Return Brushes.LimeGreen
            Else
                Dim calcAlpha As Integer = daysToRelease * (255 / 365)
                Return New SolidColorBrush(Color.FromArgb(255 - IIf(calcAlpha > 255, 255, calcAlpha), 255, 0, 0))
            End If
        End Function

        Function ConvertBack(value As Object, targetType As Type, parameter As Object, culture As CultureInfo) As Object Implements IValueConverter.ConvertBack
            Throw New NotImplementedException()
        End Function

    End Class

End Namespace
