Namespace APIManagement

    ''' <summary>
    ''' TVRage API structure. DEPRECATED!
    ''' </summary>
    Public NotInheritable Class APITVRage

        Private Sub New()
        End Sub

        Private Const tvRageKey As String = "AP64nlcVTB1BpdHSBWR3"

        Private Const QueryByName As String = "https://services.tvrage.com/myfeeds/search.php?key=" & tvRageKey & "&show="
        Private Const QueryInfoByID As String = "https://services.tvrage.com/myfeeds/showinfo.php?key="
        Private Const QueryEpListByID As String = "https://services.tvrage.com/myfeeds/episode_list.php?key="
        Private Const QueryParam As String = "&sid="

        Private Const NodeShowID As String = "/Results/show/showid"
        Private Const NodeName As String = "/Results/show/name"
        Private Const NodeImage As String = "/Showinfo/image"
        Private Const NodeAirDate As String = "/Show/Episodelist/Season/episode/airdate"
        Private Const NodeTitle As String = "/Show/Episodelist/Season/episode/title"

    End Class

End Namespace
