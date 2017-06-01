Namespace APIManagement

    ''' <summary>
    ''' TheMovieDb API structure. Credit: http://www.themoviedb.org/documentation/api
    ''' </summary>
    Public NotInheritable Class APITmdb

        Private Sub New()
        End Sub

        Public Const QueryByName As String = "https://api.themoviedb.org/3/search/tv?query="
        Public Const QueryKey As String = "&api_key="
        Public Const QueryByImage As String = "https://image.tmdb.org/t/p/w300"
        Public Const QueryByID As String = "https://api.themoviedb.org/3/tv/"

        Public Const NodeTotalResults As String = "total_results"
        Public Const NodeResults As String = "results"
        Public Const NodeID As String = "id"
        Public Const NodeName As String = "name"
        Public Const NodeImage As String = "backdrop_path"
        Public Const NodeAirDate As String = "first_air_date"
        Public Const NodeSeason As String = "/season/"
        Public Const NodeSeasons As String = "seasons"
        Public Const NodeSeasonNumber As String = "season_number"
        Public Const NodeEpisodes As String = "episodes"
        Public Const NodeEpAirDate As String = "air_date"
        Public Const NodeEpNumber As String = "episode_number"

    End Class

End Namespace
