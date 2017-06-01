Imports System.ComponentModel
Imports System.Threading
Imports MasterLib.FilesystemManagement

Namespace MemberOrganization

    ''' <summary>
    ''' Organizer's main UI class.
    ''' </summary>
    Class OrganizerMain

#Region "Declarations"

        ''' <summary>
        ''' Provides an identical structure for a CompositeDelivery to a CompositeMember. 
        ''' </summary>
        Private Class DeliveryDisplayHelper

            Public ReadOnly Property Name As String
            Public ReadOnly Property Type As ContentType
            Public ReadOnly Property DeliveryName As String
            Public ReadOnly Property DeliverySeason As Integer
            Public ReadOnly Property DeliveryEpisode As Integer
            Public ReadOnly Property DeliveryDisplayName As String
            Public ReadOnly Property Release As CompositeDate

            ''' <summary>
            ''' Requires the holding composite member's name and content type as well as the delivery's parameters.
            ''' </summary>
            ''' <param name="cmName">Composite member's name.</param>
            ''' <param name="cmType">Composite member's content type.</param>
            ''' <param name="cdName">Delivery name.</param>
            ''' <param name="cdSeason">Delivery's season.</param>
            ''' <param name="cdEpisode">Delivery's episode.</param>
            ''' <param name="cdRelease">Delivery's release.</param>
            Public Sub New(cmName As String, cmType As ContentType, cdName As String, cdSeason As Integer, cdEpisode As Integer, cdRelease As Date)
                Name = cmName
                Type = cmType
                DeliveryName = cdName
                DeliverySeason = cdSeason
                DeliveryEpisode = cdEpisode
                DeliveryDisplayName = $"({cdSeason}|{cdEpisode}) {cdName}"
                Release = cdRelease
            End Sub

        End Class

        Private _sc As SynchronizationContext = SynchronizationContext.Current
        Private _filterDate As CompositeDate
        Private _dgLastSortColumn As DataGridColumn

#End Region

#Region "User Interface"

        Private Sub Window_Initialized(sender As Object, e As EventArgs)

            AddHandler CompositeMemberOperator.OnStatusChanged, AddressOf SetStatus

            cbMemberContentType.ItemsSource = [Enum].GetValues(GetType(ContentType))
            cbMemberContentType.SelectedIndex = 0
            cbMemberFrequency.ItemsSource = [Enum].GetValues(GetType(ManualFrequency))
            cbMemberFrequency.SelectedIndex = 0

            For Each cmChecked In CompositeMemberOperator.ActiveMemberList.Where(Function(cm) cm.Checked)
                ActualizeCompletedLists(cmChecked) ' All checked composite members are added to the completed lists.
            Next
        End Sub

        Private Sub dgWatchlist_Sorting(sender As Object, e As DataGridSortingEventArgs) Handles dgWatchlist.Sorting
            _dgLastSortColumn = e.Column ' Get the column of the last sort.
        End Sub

        Private Sub rbLookupGroup_Checked(sender As Object, e As RoutedEventArgs) Handles rbAllTime.Checked, rbYear.Checked, rbMonth.Checked, rbWeek.Checked
            Select Case CType(sender, RadioButton).Name
                Case NameOf(rbAllTime)
                    _filterDate = Nothing
                Case NameOf(rbYear)
                    _filterDate = Date.Now.AddYears(1)
                Case NameOf(rbMonth)
                    _filterDate = Date.Now.AddMonths(1)
                Case NameOf(rbWeek)
                    _filterDate = Date.Now.AddDays(7)
            End Select
            AddFilteredMembers(CompositeMemberOperator.ActiveMemberList.Where(
                               Function(cm) Not cm.Checked), True, True)
        End Sub

        Private Sub cbSeriesBase_Checked(sender As Object, e As RoutedEventArgs) Handles cbSeriesBase.Checked
            AddFilteredMembers(CompositeMemberOperator.ActiveMemberList.Where(
                               Function(cm) Not cm.Checked AndAlso cm.Type = ContentType.Series), False, False)
        End Sub

        Private Sub cbSeriesBase_Unchecked(sender As Object, e As RoutedEventArgs) Handles cbSeriesBase.Unchecked
            Dim cmSeriesArr() As CompositeMember = dgWatchlist.Items.OfType(Of CompositeMember).ToArray
            For Each cmSeries In cmSeriesArr.Where(Function(cm) cm.Type = ContentType.Series)
                dgWatchlist.Items.Remove(cmSeries)
            Next
        End Sub

        Private Sub cbIncompleted_Checked(sender As Object, e As RoutedEventArgs) Handles cbIncompleted.Checked
            AddFilteredMembers(CompositeMemberOperator.ActiveMemberList.Where(
                               Function(cm) Not cm.Checked AndAlso Not cm.Type = ContentType.Series AndAlso Not cm.Release.Complete), False, False)
        End Sub

        Private Sub cbIncompleted_Unchecked(sender As Object, e As RoutedEventArgs) Handles cbIncompleted.Unchecked
            Dim cmArr() As CompositeMember = dgWatchlist.Items.OfType(Of CompositeMember).ToArray
            For Each cmInc In cmArr.Where(Function(cm) Not cm.Type = ContentType.Series AndAlso Not cm.Release.Complete)
                dgWatchlist.Items.Remove(cmInc)
            Next
        End Sub

        Private Sub watchlistButtons_Click(sender As Object, e As RoutedEventArgs) Handles btnDelete.Click, btnRelease.Click
            If dgWatchlist.SelectedItems.Count > 0 Then
                ' CompositeMembers.
                RemoveHandler CompositeMemberOperator.OnStatusChanged, AddressOf SetStatus
                For Each cm In dgWatchlist.SelectedItems.OfType(Of CompositeMember).ToArray
                    Select Case CType(sender, Button).Name
                        Case NameOf(btnDelete)
                            If Not CompositeMemberOperator.DeleteMember(cm) Then Exit Sub
                        Case NameOf(btnRelease)
                            cm.Checked = True
                            If Not CompositeMemberOperator.UpdateMember(cm) Then Exit Sub
                            ActualizeCompletedLists(cm)
                    End Select

                    ' Actualize the Watchlist.
                    dgWatchlist.Items.Remove(cm) ' Removing the CompositeMember from the watchlist DataGrid.
                    If cm.Type = ContentType.Series AndAlso dgWatchlist.Items.OfType(Of DeliveryDisplayHelper).Count > 0 Then
                        Dim ddhRemArr() As DeliveryDisplayHelper = dgWatchlist.Items.OfType(Of DeliveryDisplayHelper).ToArray.Where(
                              Function(ddh) ddh.Name = cm.Name).ToArray
                        For Each ddhRem In ddhRemArr
                            ' If the associated CompositeMember was removed, all its deliveries need to be removed from the DataGrid as well.
                            dgWatchlist.Items.Remove(ddhRem)
                        Next
                    End If
                Next

                ' DeliveryDisplayHelpers.
                If dgWatchlist.SelectedItems.Count > 0 Then
                    Dim ddhArr() As DeliveryDisplayHelper = dgWatchlist.SelectedItems.OfType(Of DeliveryDisplayHelper).ToArray
                    For Each cmDistName In ddhArr.Select(Function(ddh) ddh.Name).ToArray.Distinct
                        ' For each affected CompositeMembers.
                        Dim cmUpd = CompositeMemberOperator.ActiveMemberList.FirstOrDefault(
                            Function(cm) cm.Name = cmDistName AndAlso cm.Type = ContentType.Series)
                        For Each ddhAssoc In ddhArr.Where(Function(ddh) ddh.Name = cmDistName)
                            Dim cd As New CompositeDelivery(ddhAssoc.DeliveryName, ddhAssoc.DeliverySeason, ddhAssoc.DeliveryEpisode, ddhAssoc.Release)
                            cmUpd.Deliveries.Remove(cd)
                            If CType(sender, Button).Name = NameOf(btnRelease) Then
                                cd.Checked = True
                                cmUpd.Deliveries.Add(cd)
                            End If
                            ' Actualize the Watchlist.
                            dgWatchlist.Items.Remove(ddhAssoc)
                        Next
                        CompositeMemberOperator.UpdateMember(cmUpd)
                    Next
                End If
                AddHandler CompositeMemberOperator.OnStatusChanged, AddressOf SetStatus
            End If
        End Sub

        Private Sub btnManualMemberAdd_Click(sender As Object, e As RoutedEventArgs) Handles btnManualMemberAdd.Click
            If Not dpMemberRelease.SelectedDate Is Nothing Then
                Dim mMember As New CompositeMember(tbMemberName.Text,
                                                   cbMemberContentType.SelectedIndex,
                                                   dpMemberRelease.SelectedDate,
                                                   cbMemberFrequency.SelectedIndex)
                If CompositeMemberOperator.CreateMember(mMember) Then
                    AddFilteredMembers({mMember}, False, False) ' Adds an unchecked manual member.
                End If
            Else
                SetStatus("No date selected!", True)
            End If
        End Sub

        Private Sub btnAutoMemberAdd_Click(sender As Object, e As RoutedEventArgs) Handles btnAutoMemberAdd.Click
            Dim selItem As CompositeMember = dgMemberResults.SelectedItem
            If Not selItem Is Nothing Then
                If CompositeMemberOperator.CreateMember(selItem) Then
                    AddFilteredMembers({selItem}, False, False) ' Adds an unchecked automatic member.
                End If
            Else
                SetStatus("No member selected!", True)
            End If
        End Sub

        Private Sub btnAutoMemberSearch_Click(sender As Object, e As RoutedEventArgs) Handles btnAutoMemberSearch.Click
            btnAutoMemberSearch.IsEnabled = False
            btnAutoMemberAdd.IsEnabled = False
            Dim sName As String = tbMemberName.Text
            Dim sType As ContentType = cbMemberContentType.SelectedIndex
            Task.Run(
                Sub()
                    Dim srchMember As New CompositeMember(sName, sType)
                    Dim resLst As List(Of CompositeMember) = CompositeMemberOperator.GetAutomaticMemberResults(srchMember)
                    _sc.Send(New SendOrPostCallback(
                             Sub()
                                 dgMemberResults.ItemsSource = resLst
                                 If Not resLst.Count = 0 Then
                                     dgMemberResults.SelectedItem = dgMemberResults.Items(0)
                                 Else
                                     SetStatus("No results found! Missing an API key?", False)
                                 End If
                                 btnAutoMemberSearch.IsEnabled = True
                                 btnAutoMemberAdd.IsEnabled = True
                             End Sub), Nothing)
                End Sub)
        End Sub

        Private Sub dgMemberResults_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles dgMemberResults.SelectionChanged
            Dim selItem As CompositeMember = dgMemberResults.SelectedItem
            If Not selItem Is Nothing Then
                imgMember.Source = ImageProcessor.GetBitmapImageFromBytes(selItem.ImageByteArr)
            End If
        End Sub

        Private Sub OrganizerMain_Closed(sender As Object, e As EventArgs) Handles Me.Closed
            RemoveHandler CompositeMemberOperator.OnStatusChanged, AddressOf SetStatus
        End Sub

#End Region

#Region "Internal"

        ''' <summary>
        ''' Updates the user when various events occur.
        ''' </summary>
        ''' <param name="message">The displayed message.</param>
        ''' <param name="err">Indicates whether the message was an error.</param>
        Private Sub SetStatus(message As String, err As Boolean)
            If Not String.IsNullOrWhiteSpace(message) Then
                MsgBox(message, IIf(err, MsgBoxStyle.Critical, MsgBoxStyle.Information), IIf(err, "Error", "Info"))
            End If
        End Sub

        ''' <summary>
        ''' Adds all newly released or previously completed Games, Movies and Series to their completed lists.
        ''' </summary>
        ''' <param name="cm">The CompositeMember to add.</param>
        Private Sub ActualizeCompletedLists(cm As CompositeMember)
            Select Case cm.Type
                Case ContentType.Game
                    lbCompletedGames.Items.Add(cm.Name)
                Case ContentType.Movie
                    lbCompletedMovies.Items.Add(cm.Name)
                Case ContentType.Series
                    lbCompletedSeries.Items.Add(cm.Name)
            End Select
        End Sub

        ''' <summary>
        ''' Adds CompositeMembers and CompositeDeliveries to the dgWatchlist control fitting a certain criteria. 
        ''' </summary>
        ''' <param name="cmDispLst">CompositeMembers to display.</param>
        ''' <param name="clearContents">Whether the contents of the control need to be cleared.</param>
        ''' <param name="addDeliveries">Adding or skipping the CompositeDeliveries</param>
        Private Sub AddFilteredMembers(cmDispLst As IEnumerable(Of CompositeMember), clearContents As Boolean, addDeliveries As Boolean)
            If clearContents Then dgWatchlist.Items.Clear() ' If required, clear the contents of the watchlist.

            ' Update the contents of the watchlist.
            For Each cmDisp In cmDispLst
                If cmDisp.Type = ContentType.Series Then
                    If addDeliveries AndAlso cmDisp.Deliveries.Count > 0 AndAlso cmDisp.Deliveries.Any(Function(cd As CompositeDelivery) Not cd.Checked) Then
                        For Each ddh As DeliveryDisplayHelper In cmDisp.Deliveries.Where(
                            Function(cd) Not cd.Checked).ToArray.Select(
                            Function(cd) New DeliveryDisplayHelper(cmDisp.Name, cmDisp.Type, cd.Name, cd.Season, cd.Episode, cd.Release))
                            If _filterDate Is Nothing OrElse ddh.Release <= _filterDate Then dgWatchlist.Items.Add(ddh)
                        Next
                    End If
                    If cbSeriesBase Is Nothing OrElse Not cbSeriesBase.IsChecked Then Continue For
                Else
                    If Not cbIncompleted Is Nothing Then
                        If Not cmDisp.Release.Complete AndAlso Not cbIncompleted.IsChecked Then Continue For
                    Else
                        If Not cmDisp.Release.Complete Then Continue For
                    End If
                End If
                If _filterDate Is Nothing OrElse cmDisp.Release <= _filterDate Then dgWatchlist.Items.Add(cmDisp)
            Next

            ' Sort the watchlist.
            Dim sortColumn As DataGridColumn ' Column to be sorted.
            Dim sortDirection As ListSortDirection ' The sort direction of the column.

            If _dgLastSortColumn Is Nothing Then
                ' In case there were no previous sorts, sort by release date in descending order.
                sortColumn = dgWatchlist.Columns(3)
                sortDirection = ListSortDirection.Descending
            Else
                ' Get the last sort column and its sort direction.
                sortColumn = _dgLastSortColumn
                sortDirection = _dgLastSortColumn.SortDirection
            End If

            dgWatchlist.Items.SortDescriptions.Clear()
            dgWatchlist.Items.SortDescriptions.Add(New SortDescription(sortColumn.SortMemberPath, sortDirection))

            For Each dgColumn In dgWatchlist.Columns
                dgColumn.SortDirection = Nothing
            Next
            sortColumn.SortDirection = sortDirection

            dgWatchlist.Items.Refresh()

        End Sub

#End Region

    End Class

End Namespace