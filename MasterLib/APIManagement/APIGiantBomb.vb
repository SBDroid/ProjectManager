Namespace APIManagement

    ''' <summary>
    ''' GiantBomb API structure. Credit: http://www.giantbomb.com/api/
    ''' </summary>
    Public NotInheritable Class APIGiantBomb

        Private Sub New()
        End Sub

        Public Const QueryByName As String = "https://www.giantbomb.com/api/search/?api_key="
        Public Const QueryParamsAll As String = "&format=xml&resources=game&field_list=id,name,original_release_date,expected_release_day,expected_release_month,expected_release_year,platforms,image&query="
        Public Const QueryByIDPrefix As String = "https://www.giantbomb.com/api/game/"
        Public Const QueryByIDSuffix As String = "/?api_key="
        Public Const QueryParamsPartial As String = "&format=xml&resources=game&field_list=original_release_date"
        Public Const NodeTotalResults As String = "/response/number_of_total_results"
        Public Const NodeGames As String = "/response/results/game"
        Public Const NodeID As String = "id"
        Public Const NodeName As String = "name"
        Public Const NodeReleaseDateByName As String = "original_release_date"
        Public Const NodeExpDay As String = "expected_release_day"
        Public Const NodeExpMonth As String = "expected_release_month"
        Public Const NodeExpYear As String = "expected_release_year"
        Public Const NodePlatforms As String = "platforms"
        Public Const NodeImage As String = "image/super_url"
        Public Const NodeReleaseDateByID As String = "/response/results/original_release_date"

    End Class

End Namespace


