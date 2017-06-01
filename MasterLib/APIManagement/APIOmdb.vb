Namespace APIManagement

    ''' <summary>
    ''' IMDB API structure. No key requirement. Credit: http://www.omdbapi.com
    ''' </summary>
    Public NotInheritable Class APIOmdb

        Private Sub New()
        End Sub

        Public Const QueryByName As String = "https://www.omdbapi.com/?r=xml&s="
        Public Const QueryByID As String = "https://www.omdbapi.com/?plot=short&r=xml&i="

        Public Const NodeTotalResults As String = "totalResults"
        Public Const NodeResults As String = "/root/result"
        Public Const NodeTitle As String = "title"
        Public Const NodeID As String = "imdbID"
        Public Const NodeType As String = "type"
        Public Const NodePoster As String = "poster"
        Public Const NodeMovie As String = "/root/movie"
        Public Const NodeReleased As String = "released"
        Public Const NodeReleaseNA As String = "N/A"

    End Class

End Namespace


