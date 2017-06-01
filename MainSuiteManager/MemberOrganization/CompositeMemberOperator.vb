Imports System.IO
Imports System.Xml
Imports System.Xml.Serialization
Imports MasterLib.APIManagement
Imports MasterLib.FilesystemManagement

Namespace MemberOrganization

    ''' <summary>
    ''' Provides standard IO operations for composite members.
    ''' </summary>
    Public NotInheritable Class CompositeMemberOperator

#Region "Declarations"

        Private Const CMO_REPOSITORY As String = "Members" ' The default member directory.
        Private Const CMO_FILENAME As String = "CompositeMember_" ' The default command prefix name.
        Private Const API_KEYS_FILENAME As String = "apiKeys" ' The API keys XML file.
        Private Const DEFAULT_EXTENSION As String = ".xml" ' The default command extension.

        ''' <summary>
        ''' Provides status messages that indicate the currently running/last completed IO operations.
        ''' </summary>
        Private NotInheritable Class StatusMessage

            Private Sub New()
            End Sub

            Public Const APIKeySaveFail As String = "Key could not be saved - unknown API type!"
            Public Const APIKeySaveSuccess As String = "API Key successfully saved!"
            Public Const MemberDeleteSuccess As String = "deleted successfully!"
            Public Const MemberDeleteFail As String = "deletion failed!"
            Public Const MemberCreateSuccess As String = "created successfully!"
            Public Const MemberCreateFail As String = "creation failed!"
            Public Const MemberUpdateSuccess As String = "updated successfully!"
            Public Const MemberUpdateFail As String = "update failed!"
            Public Const MemberExists As String = "already exists!"
            Public Const MemberObjectEmpty As String = "Member object is empty!"
            Public Const MemberNameEmpty As String = "Member has no name!"
            Public Const MemberUnknownCombinedType As String = "Member is an unknown type!"
            Public Const MemberUnknownTypeForReleaseSync As String = "Unknown type found for date clarification! Member:"
            Public Const MemberSynchronizationError As String = "Member synchronization is still running..."
            Public Const MemberSynchronizationStart As String = "Member synchronization started..."
            Public Const MemberSynchronizationFinish As String = "Members synchronized successfully!"
            Public Const MemberDateClarification As String = "Date clarification started for: "
            Public Const MemberDeliveryActualization As String = "Delivery actualization started for: "

        End Class

        Private Shared _syncRunning As Boolean ' Flag determining whether a sync process has been initiated.
        Private Shared _otfList As List(Of ObjectToFile(Of CompositeMember, String)) = LoadMembers() ' Contains all composite members.
        Private Shared _keyArr() As String = LoadKeys() ' Contains all API keys.

        Shared Event OnStatusChanged(statusMessage As String, err As Boolean) ' Provides status messages to the user of the CompositeMemberOperator class.

        Shared ReadOnly Property ActiveMemberList As List(Of CompositeMember)
            Get
                Return _otfList.Select(Function(otf As ObjectToFile(Of CompositeMember, String)) otf.InternalObject).ToList ' Returns only the CompositeMember objects.
            End Get
        End Property

        Shared ReadOnly Property SynchronizationRunning As Boolean
            Get
                Return _syncRunning
            End Get
        End Property

#End Region

#Region "Main"

        ''' <summary>
        ''' Creates a new API key. Requires a corresponding type to be associated with.
        ''' </summary>
        ''' <param name="apiType">The API key type.</param>
        ''' <param name="apiKey">The API key.</param>
        <MethodAlias(ConsoleController.CompositeMemberOperatorAPIKeyCreate)>
        Shared Sub APIKeyCreate(apiType As APIKeyType, apiKey As String)

            If apiType > [Enum].GetNames(GetType(APIKeyType)).Length - 1 Then
                RaiseEvent OnStatusChanged(StatusMessage.APIKeySaveFail, True)
                Exit Sub
            End If

            _keyArr(apiType) = apiKey

            If Not Directory.Exists($"{My.Application.Info.DirectoryPath}\{CMO_REPOSITORY}") Then
                Directory.CreateDirectory($"{My.Application.Info.DirectoryPath}\{CMO_REPOSITORY}")
            End If

            Dim xmlSrl As XmlSerializer = New XmlSerializer(GetType(String()))
            Using fs As New FileStream($"{My.Application.Info.DirectoryPath}\{CMO_REPOSITORY}\{API_KEYS_FILENAME}{DEFAULT_EXTENSION}", FileMode.Create, FileAccess.Write)
                xmlSrl.Serialize(fs, _keyArr) ' Serializing the API keys array.
                RaiseEvent OnStatusChanged(StatusMessage.APIKeySaveSuccess, False)
                fs.Close()
            End Using
        End Sub

        ''' <summary>
        ''' Gets the total number of results after an automatic member search.
        ''' </summary>
        ''' <param name="cm">Requires a CompositeMember's name and content type.</param>
        ''' <returns>The total number of CompositeMember results.</returns>
        <MethodAlias(ConsoleController.CompositeMemberOperatorCount)>
        Shared Function GetAutomaticMemberCount(cm As CompositeMember) As String
            Dim res As Integer = GetAutomaticMemberResults(cm, False)
            Return IIf(res > 0, $"Found a total of {res} results.", "No results found.")
        End Function

        ''' <summary>
        ''' Creates a new CompsoiteMember and saves it locally.
        ''' </summary>
        ''' <param name="cm">The CompositeMember to add.</param>
        ''' <param name="EventOnSuccess">Raising an event upon a successful exection. True by default.</param>
        ''' <returns>Returns whether the create has been successful.</returns>
        <MethodAlias(ConsoleController.CompositeMemberOperatorCreate)>
        Shared Function CreateMember(cm As CompositeMember, Optional EventOnSuccess As Boolean = True) As Boolean
            ' Composite Member integrity checks.
            Select Case True
                Case cm Is Nothing
                    RaiseEvent OnStatusChanged(StatusMessage.MemberObjectEmpty, True) ' Member mustn't be empty.
                    Return False
                Case String.IsNullOrWhiteSpace(cm.Name)
                    RaiseEvent OnStatusChanged(StatusMessage.MemberNameEmpty, True) ' Member's name mustn't be empty.
                    Return False
                Case cm.CombinedType = CombinedType.Unknown
                    RaiseEvent OnStatusChanged(StatusMessage.MemberUnknownCombinedType, True) ' Member shouldn't be an unknown type.
                    Return False
                Case _otfList.Any(Function(otf As ObjectToFile(Of CompositeMember, String)) otf.InternalObject.Equals(cm)) OrElse
                                File.Exists($"{My.Application.Info.DirectoryPath}\{CMO_REPOSITORY}\{CMO_FILENAME}{GenericFileProcessor.GetCleanFilenameFromString(cm.Name)}{DEFAULT_EXTENSION}")
                    RaiseEvent OnStatusChanged($"{cm.Name} {StatusMessage.MemberExists}", True) ' Member shouldn't already exist.
                    Return False
            End Select

            Dim xmlSrl As XmlSerializer = New XmlSerializer(GetType(CompositeMember))
            Using fs As New FileStream($"{My.Application.Info.DirectoryPath}\{CMO_REPOSITORY}\{CMO_FILENAME}{GenericFileProcessor.GetCleanFilenameFromString(cm.Name)}{DEFAULT_EXTENSION}", FileMode.Create)
                xmlSrl.Serialize(fs, cm) ' Serializing the CompositeMember object.
                _otfList.Add(New ObjectToFile(Of CompositeMember, String)(cm, GenericFileProcessor.GetCleanFilenameFromString(cm.Name))) ' Actualizing the current ActiveMembersList.
                fs.Close()
                If EventOnSuccess Then RaiseEvent OnStatusChanged($"{cm.Name} {StatusMessage.MemberCreateSuccess}", False)
                Return True
            End Using
            RaiseEvent OnStatusChanged($"{cm.Name} {StatusMessage.MemberCreateFail}", True)
            Return False
        End Function

        ''' <summary>
        ''' Deletes an existing CompositeMember.
        ''' </summary>
        ''' <param name="cm">The CompositeMember to delete.</param>
        ''' <param name="EventOnSuccess">Raising an event upon a successful exection. True by default.</param>
        ''' <returns>Returns whether the delete has been successful.</returns>
        <MethodAlias(ConsoleController.CompositeMemberOperatorDelete)>
        Shared Function DeleteMember(cm As CompositeMember, Optional EventOnSuccess As Boolean = True) As Boolean
            Dim cmRem As ObjectToFile(Of CompositeMember, String) = _otfList.FirstOrDefault(
                Function(otf As ObjectToFile(Of CompositeMember, String)) otf.InternalObject.Equals(cm))
            If Not cmRem Is Nothing Then
                Dim fileRem As String = $"{My.Application.Info.DirectoryPath}\{CMO_REPOSITORY}\{CMO_FILENAME}{cmRem.FileIdentifier}{DEFAULT_EXTENSION}"
                If File.Exists(fileRem) Then
                    File.Delete(fileRem)
                End If
                _otfList.Remove(cmRem) ' Actualizing the current ActiveMembersList.
                If EventOnSuccess Then RaiseEvent OnStatusChanged($"{cm.Name} {StatusMessage.MemberDeleteSuccess}", False)
                Return True
            End If
            RaiseEvent OnStatusChanged($"{cm.Name} {StatusMessage.MemberDeleteFail}", True)
            Return False
        End Function

        ''' <summary>
        ''' Updates an existing CompositeMember.
        ''' </summary>
        ''' <param name="cm">The CompositeMember to update.</param>
        ''' <returns>Returns whether the update has been successful.</returns>
        <MethodAlias(ConsoleController.CompositeMemberOperatorUpdate)>
        Shared Function UpdateMember(cm As CompositeMember) As Boolean
            If DeleteMember(cm, False) AndAlso CreateMember(cm, False) Then
                RaiseEvent OnStatusChanged($"{cm.Name} {StatusMessage.MemberUpdateSuccess}", False)
                Return True
            Else
                RaiseEvent OnStatusChanged($"{cm.Name} {StatusMessage.MemberUpdateFail}", True)
                Return False
            End If
        End Function

        ''' <summary>
        ''' Synchronizes all CompositeMembers. This includes: incomplete automatic members, outdated deliveries for both automatic and manual members.
        ''' </summary>
        <MethodAlias(ConsoleController.CompositeMemberOperatorSync)>
        Shared Sub SynchronizeMembers()

            If _syncRunning Then
                RaiseEvent OnStatusChanged(StatusMessage.MemberSynchronizationError, True)
                Exit Sub
            End If

            _syncRunning = True
            RaiseEvent OnStatusChanged(StatusMessage.MemberSynchronizationStart, False)

            For Each otf In _otfList

                If otf.InternalObject.Checked Then
                    ' Checked members are skipped.
                    Continue For
                End If

                Dim objectChanged As Boolean = False
                Select Case otf.InternalObject.CombinedType
                    Case CombinedType.AutomaticSingle

                        If otf.InternalObject.Release.Complete Then
                            ' Completed release dates don't need clarification.
                            Continue For
                        End If

                        RaiseEvent OnStatusChanged($"{StatusMessage.MemberDateClarification}{otf.InternalObject.Name}", False)
                        Dim reqStr As String = ""
                        Select Case otf.InternalObject.Type
                            Case ContentType.Game
                                reqStr = $"{APIGiantBomb.QueryByIDPrefix}{otf.InternalObject.InternalID}{APIGiantBomb.QueryByIDSuffix}{_keyArr(0)}{APIGiantBomb.QueryParamsPartial}"
                                Dim respXml As XmlDocument = RESTResponseProvider.GetAPIResponse(reqStr, ResponseType.XML)
                                If respXml Is Nothing Then Exit Select
                                Dim root As XmlElement = respXml.DocumentElement
                                Dim node As XmlNode = root.SelectSingleNode(APIGiantBomb.NodeReleaseDateByID)
                                If Not String.IsNullOrWhiteSpace(node.InnerText) Then
                                    otf.InternalObject.Release = node.InnerText ' Release date clarification.
                                    objectChanged = True
                                End If
                            Case Else
                                RaiseEvent OnStatusChanged($"{StatusMessage.MemberUnknownTypeForReleaseSync} {otf.InternalObject.Name}", True)
                        End Select
                    Case CombinedType.AutomaticRepeatable
                        RaiseEvent OnStatusChanged($"{StatusMessage.MemberDeliveryActualization}{otf.InternalObject.Name}", False)
                        Dim maxSeason As Integer = 0
                        Dim maxEpisode As Integer = 0
                        If otf.InternalObject.Deliveries.Count > 0 Then
                            ' Find the maximum season/episode pair.
                            maxSeason = otf.InternalObject.Deliveries.Max(
                                Function(cd As CompositeDelivery) cd.Season)
                            maxEpisode = otf.InternalObject.Deliveries.Where(
                                Function(cd As CompositeDelivery) cd.Season = maxSeason).ToArray.Max(
                                Function(cd As CompositeDelivery) cd.Episode)

                            ' Clean up checked deliveries.
                            If otf.InternalObject.Deliveries.Any(Function(cd As CompositeDelivery) cd.Checked) Then
                                Dim maxReleaseChecked As Date = otf.InternalObject.Deliveries.Where(
                                    Function(cd As CompositeDelivery) cd.Checked).ToArray.Max(
                                    Function(cd As CompositeDelivery) cd.Release)
                                objectChanged = CBool(otf.InternalObject.Deliveries.RemoveAll(
                                    Function(cd As CompositeDelivery) cd.Checked AndAlso Not cd.Release = maxReleaseChecked))
                            End If
                        End If

                        ' Populate with new deliveries.
                        Dim reqSeasons As String = $"{APITmdb.QueryByID}{otf.InternalObject.InternalID}{APITmdb.QueryKey.Replace("&", "?")}{_keyArr(1)}"
                        Dim respJson As Dictionary(Of String, Object) = RESTResponseProvider.GetAPIResponse(reqSeasons, ResponseType.JSON)
                        If respJson Is Nothing Then Exit Select
                        For Each itemSeason As Dictionary(Of String, Object) In respJson(APITmdb.NodeSeasons)
                            Dim sNum As String = itemSeason(APITmdb.NodeSeasonNumber)
                            If sNum >= maxSeason Then
                                Dim reqRetries As Integer = 0 ' Total counter of retry attempts for getting all episodes in a season.
                                Dim reqEps As String = $"{APITmdb.QueryByID}{otf.InternalObject.InternalID}{APITmdb.NodeSeason}{sNum}{APITmdb.QueryKey.Replace("&", "?")}{_keyArr(1)}"
                                Dim respEps As Object ' Contains all episodes in a season.
                                Do
                                    respEps = RESTResponseProvider.GetAPIResponse(reqEps, ResponseType.JSON)?(APITmdb.NodeEpisodes) ' Gets all episodes in a season.
                                    reqRetries += 1
                                Loop While respEps Is Nothing AndAlso reqRetries < 5

                                If respEps Is Nothing Then Continue For

                                For Each itemEpisode As Dictionary(Of String, Object) In respEps
                                    Dim epNum As String = itemEpisode(APITmdb.NodeEpNumber)
                                    If (sNum = maxSeason AndAlso epNum > maxEpisode) OrElse sNum > maxSeason Then
                                        otf.InternalObject.Deliveries.Add(New CompositeDelivery(itemEpisode(APITmdb.NodeName), sNum, epNum, itemEpisode(APITmdb.NodeEpAirDate)))
                                        objectChanged = True
                                    End If
                                Next
                            End If
                        Next
                    Case CombinedType.ManualRepeatable
                        RaiseEvent OnStatusChanged($"{StatusMessage.MemberDeliveryActualization}{otf.InternalObject.Name}", False)
                        Dim maxRelease As Date
                        If otf.InternalObject.Deliveries.Count > 0 Then
                            maxRelease = otf.InternalObject.Deliveries.Max(Function(cd As CompositeDelivery) cd.Release)
                            ' Clean up checked deliveries.
                            If otf.InternalObject.Deliveries.Any(Function(cd As CompositeDelivery) cd.Checked) Then
                                Dim maxReleaseWatched As Date = otf.InternalObject.Deliveries.Where(
                                    Function(cd As CompositeDelivery) cd.Checked).ToArray.Max(
                                    Function(cd As CompositeDelivery) cd.Release)
                                objectChanged = CBool(otf.InternalObject.Deliveries.RemoveAll(
                                    Function(cd As CompositeDelivery) cd.Checked AndAlso Not cd.Release = maxReleaseWatched))
                            End If
                        Else
                            maxRelease = New Date(otf.InternalObject.Release.Year, otf.InternalObject.Release.Month, otf.InternalObject.Release.Day)
                            otf.InternalObject.Deliveries.Add(New CompositeDelivery(otf.InternalObject.Name, maxRelease))
                            objectChanged = True
                        End If

                        While maxRelease <= Date.Now
                            Select Case otf.InternalObject.Frequency
                                Case ManualFrequency.Weekly
                                    maxRelease = maxRelease.AddDays(7)
                                Case ManualFrequency.Monthly
                                    maxRelease = maxRelease.AddMonths(1)
                                Case ManualFrequency.Yearly
                                    maxRelease = maxRelease.AddYears(1)
                            End Select
                            otf.InternalObject.Deliveries.Add(New CompositeDelivery(otf.InternalObject.Name, maxRelease))
                            objectChanged = True
                        End While
                End Select

                If objectChanged Then
                    ' Delete the existing xml file.
                    Dim fileRecr As String = $"{My.Application.Info.DirectoryPath}\{CMO_REPOSITORY}\{CMO_FILENAME}{otf.FileIdentifier}{DEFAULT_EXTENSION}"
                    If File.Exists(fileRecr) Then
                        File.Delete(fileRecr)
                    End If

                    ' Save the changes to a new xml file.
                    Dim xmlSrl As XmlSerializer = New XmlSerializer(GetType(CompositeMember))
                    Using fs As New FileStream(fileRecr, FileMode.Create)
                        xmlSrl.Serialize(fs, otf.InternalObject) ' Serializing the CompositeMember object.
                        fs.Close()
                    End Using
                End If
            Next

            RaiseEvent OnStatusChanged(StatusMessage.MemberSynchronizationFinish, False)
            _syncRunning = False
        End Sub

        ''' <summary>
        ''' Gets the results after an automatic member search.
        ''' </summary>
        ''' <param name="cm">The CompositeMember to search.</param>
        ''' <param name="fullData">Determines whether to return a list of all results as objects or just their count.</param>
        ''' <returns>Returns all resulting composite members or their count.</returns>
        Shared Function GetAutomaticMemberResults(cm As CompositeMember, Optional fullData As Boolean = True) As Object
            ' UI calls only.
            Select Case True
                Case cm Is Nothing
                    RaiseEvent OnStatusChanged(StatusMessage.MemberObjectEmpty, True) ' Member mustn't be empty.
                    Return Nothing
                Case String.IsNullOrWhiteSpace(cm.Name)
                    RaiseEvent OnStatusChanged(StatusMessage.MemberNameEmpty, True) ' Member's name mustn't be empty.
                    Return Nothing
            End Select

            Dim cmResults As New List(Of CompositeMember)
            Dim cmCount As Integer = 0

            Select Case cm.Type
                Case ContentType.Game
                    Dim respXml As XmlDocument = RESTResponseProvider.GetAPIResponse($"{APIGiantBomb.QueryByName}{_keyArr(0)}{APIGiantBomb.QueryParamsAll}""{cm.Name}""", ResponseType.XML)
                    If respXml Is Nothing Then Exit Select
                    Dim root As XmlElement = respXml.DocumentElement
                    If Not root.SelectSingleNode(APIGiantBomb.NodeTotalResults).InnerText = "0" Then
                        If Not fullData Then
                            cmCount = root.SelectNodes(APIGiantBomb.NodeGames).Count
                            Exit Select
                        End If
                        For Each itemNode As XmlNode In root.SelectNodes(APIGiantBomb.NodeGames)
                            If itemNode(APIGiantBomb.NodePlatforms).InnerText.ToUpper.Contains("PC") Then
                                Dim itemCM As New CompositeMember(itemNode(APIGiantBomb.NodeName).InnerText, ContentType.Game)
                                itemCM.InternalID = itemNode(APIGiantBomb.NodeID).InnerText
                                If Not String.IsNullOrWhiteSpace(itemNode(APIGiantBomb.NodeReleaseDateByName).InnerText) Then
                                    itemCM.Release = itemNode(APIGiantBomb.NodeReleaseDateByName).InnerText
                                Else
                                    Dim dateCM As New CompositeDate(0, 0, 0)
                                    Integer.TryParse(itemNode(APIGiantBomb.NodeExpYear).InnerText, dateCM.Year)
                                    Integer.TryParse(itemNode(APIGiantBomb.NodeExpMonth).InnerText, dateCM.Month)
                                    Integer.TryParse(itemNode(APIGiantBomb.NodeExpDay).InnerText, dateCM.Day)
                                    itemCM.Release = dateCM
                                End If
                                itemCM.ImageByteArr = RESTResponseProvider.GetAPIResponse(itemNode.SelectSingleNode(APIGiantBomb.NodeImage)?.InnerText, ResponseType.IMAGE)
                                cmResults.Add(itemCM)
                            End If
                        Next
                    End If
                Case ContentType.Movie
                    Dim respXml As XmlDocument = RESTResponseProvider.GetAPIResponse($"{APIOmdb.QueryByName}{cm.Name}", ResponseType.XML)
                    If respXml Is Nothing Then Exit Select
                    Dim root As XmlElement = respXml.DocumentElement
                    If root.HasAttribute(APIOmdb.NodeTotalResults) Then
                        If Not fullData Then
                            cmCount = root.SelectNodes(APIOmdb.NodeResults).Count
                            Exit Select
                        End If
                        For Each itemNode As XmlNode In root.SelectNodes(APIOmdb.NodeResults)
                            If itemNode.Attributes(APIOmdb.NodeType).InnerText = "movie" Then
                                Dim innerXml As XmlDocument = RESTResponseProvider.GetAPIResponse($"{APIOmdb.QueryByID}{itemNode.Attributes(APIOmdb.NodeID).InnerText}", ResponseType.XML)
                                If innerXml Is Nothing Then Continue For
                                Dim innerRoot As XmlElement = innerXml.DocumentElement
                                Dim itemCM As New CompositeMember(itemNode.Attributes(APIOmdb.NodeTitle).InnerText, ContentType.Movie)
                                Dim itemRelease As String = innerRoot.SelectSingleNode(APIOmdb.NodeMovie).Attributes(APIOmdb.NodeReleased).InnerText
                                itemCM.InternalID = itemNode.Attributes(APIOmdb.NodeID).InnerText
                                If itemRelease = APIOmdb.NodeReleaseNA Then
                                    itemCM.Release = New CompositeDate(0, 0, 0)
                                Else
                                    itemCM.Release = itemRelease
                                End If
                                itemCM.ImageByteArr = RESTResponseProvider.GetAPIResponse(itemNode.Attributes(APIOmdb.NodePoster)?.InnerText, ResponseType.IMAGE)
                                cmResults.Add(itemCM)
                            End If
                        Next
                    End If
                Case ContentType.Series
                    Dim respJson As Dictionary(Of String, Object) = RESTResponseProvider.GetAPIResponse($"{APITmdb.QueryByName}{cm.Name}{APITmdb.QueryKey}{_keyArr(1)}", ResponseType.JSON)
                    If respJson Is Nothing Then Exit Select
                    If CInt(respJson(APITmdb.NodeTotalResults)) > 0 Then
                        If Not fullData Then
                            cmCount = respJson(APITmdb.NodeResults).Count
                            Exit Select
                        End If
                        For Each itemJson As Dictionary(Of String, Object) In respJson(APITmdb.NodeResults)
                            Dim reqImg As String = $"{APITmdb.QueryByImage}{itemJson(APITmdb.NodeImage)}{APITmdb.QueryKey.Replace("&", "?")}{_keyArr(1)}"
                            Dim itemCM As New CompositeMember(itemJson(APITmdb.NodeName), ContentType.Series)
                            itemCM.InternalID = itemJson(APITmdb.NodeID)
                            itemCM.Release = itemJson(APITmdb.NodeAirDate).ToString
                            itemCM.ImageByteArr = RESTResponseProvider.GetAPIResponse(reqImg, ResponseType.IMAGE)
                            cmResults.Add(itemCM) ' Deliveries are added during synchronization.
                        Next
                    End If
            End Select
            Return IIf(fullData, cmResults, cmCount)
        End Function

        ''' <summary>
        ''' Releases all CompositeMemberOperator resources.
        ''' </summary>
        Shared Sub Dispose()
            _otfList.Clear()
            _keyArr = Nothing
        End Sub

#End Region

#Region "Internal"

        ''' <summary>
        ''' Cannot be initialized directly.
        ''' </summary>
        Private Sub New()
        End Sub

        ''' <summary>
        ''' Loads all composite members.
        ''' </summary>
        ''' <returns>Returns a list with all composite members.</returns>
        Private Shared Function LoadMembers() As List(Of ObjectToFile(Of CompositeMember, String))
            Dim xmlSrl As XmlSerializer = New XmlSerializer(GetType(CompositeMember))
            Dim tempMemList = New List(Of ObjectToFile(Of CompositeMember, String))

            If Not Directory.Exists($"{My.Application.Info.DirectoryPath}\{CMO_REPOSITORY}") Then
                Directory.CreateDirectory($"{My.Application.Info.DirectoryPath}\{CMO_REPOSITORY}")
                Return tempMemList
            End If

            ' Deserializes all CompositeMember XML files from the CMO repository.
            For Each cmoXmlFile In Directory.GetFiles($"{My.Application.Info.DirectoryPath}\{CMO_REPOSITORY}").Where(
                    Function(fName As String)
                        If Path.GetExtension(fName).ToLower.Equals(DEFAULT_EXTENSION) Then
                            Dim fn As String = Path.GetFileNameWithoutExtension(fName)
                            Return fn.StartsWith(CMO_FILENAME, StringComparison.Ordinal)
                        End If
                        Return False
                    End Function).ToArray

                Using cmoReader As New StreamReader(cmoXmlFile)
                    Dim fn As String = Path.GetFileNameWithoutExtension(cmoXmlFile)
                    Dim fStr As String = fn.Substring(fn.LastIndexOf("_") + 1) ' Getting the CompositeMember's identifier from the XML file.
                    tempMemList.Add(New ObjectToFile(Of CompositeMember, String)(CType(xmlSrl.Deserialize(cmoReader), CompositeMember), fStr))
                    cmoReader.Close()
                End Using
            Next
            Return tempMemList
        End Function

        ''' <summary>
        ''' Loads all API keys.
        ''' </summary>
        ''' <returns>Returns an array with all API keys.</returns>
        Private Shared Function LoadKeys() As String()
            Dim arrSize As Integer = [Enum].GetNames(GetType(APIKeyType)).Length
            If Not Directory.Exists($"{My.Application.Info.DirectoryPath}\{CMO_REPOSITORY}") Then
                Directory.CreateDirectory($"{My.Application.Info.DirectoryPath}\{CMO_REPOSITORY}")
                Return Enumerable.Repeat("", arrSize).ToArray
            End If

            If File.Exists($"{My.Application.Info.DirectoryPath}\{CMO_REPOSITORY}\{API_KEYS_FILENAME}{DEFAULT_EXTENSION}") Then
                Dim apiKeys() As String = Enumerable.Repeat("", arrSize).ToArray
                Using cmdReader As New StreamReader($"{My.Application.Info.DirectoryPath}\{CMO_REPOSITORY}\{API_KEYS_FILENAME}{DEFAULT_EXTENSION}")
                    Dim xmlSrl As XmlSerializer = New XmlSerializer(GetType(String()))
                    apiKeys = CType(xmlSrl.Deserialize(cmdReader), String()) ' Deserializing the API keys array.
                    cmdReader.Close()
                End Using
                Return apiKeys
            Else
                Return Enumerable.Repeat("", arrSize).ToArray
            End If
        End Function

#End Region

    End Class

    ''' <summary>
    ''' Determines the API key to use. The enumeration's value corresponds to the index of _keyArr.
    ''' </summary>
    Public Enum APIKeyType
        GiantBomb = 0
        TheMovieDB = 1
    End Enum

End Namespace