Imports System.ComponentModel
Imports System.Text.RegularExpressions
Imports System.Threading
Imports MainSuiteManager.KeyMonitoring
Imports MainSuiteManager.MemberOrganization
Imports MainSuiteManager.HarmonicMixing
Imports MainSuiteManager.StorageMonitoring
Imports MasterLib.ProcessManagement

''' <summary>
''' Manager's main UI class.
''' </summary>
Class ManagerMain

#Region "Declarations"

    ''' <summary>
    ''' Provides status messages that indicate the currently running/last completed actions.
    ''' </summary>
    Private NotInheritable Class StatusMessage

        Private Sub New()
        End Sub

        Public Const InfoInitializationStart As String = "Initialization started..."
        Public Const InfoInitializationEnd As String = "Initialization finished!"
        Public Const InfoEmptyLine As String = "[EmptyLine]"
        Public Const InfoTrue As String = "True"
        Public Const InfoFalse As String = "False"
        Public Const ErrorSyncRunning As String = "Member synchronization is still running..."

    End Class

    Private _sc As SynchronizationContext = SynchronizationContext.Current
    Private _callCacheList As New List(Of String)
    Private _callCacheIndex As Integer

#End Region

#Region "User Interface"

    Private Sub ManagerMain_Initialized(sender As Object, e As EventArgs)
        SetStatus(StatusMessage.InfoInitializationStart, False)

        ' Preparing working area.
        Dim secondaryScreen As Forms.Screen = Forms.Screen.AllScreens.FirstOrDefault(
            Function(scr)
                Return Not scr.Primary
            End Function)
        Left = secondaryScreen.WorkingArea.Right - Width + 7
        Top = secondaryScreen.WorkingArea.Bottom - Height + 7

        ' Adding event handlers.
        AddHandler ConsoleController.OnStatusChanged, AddressOf SetStatus
        AddHandler ConsoleController.OnClearRequest, AddressOf ClearConsole
        AddHandler ConsoleController.OnMSMExit, AddressOf FreeAllocatedResources
        AddHandler KeyMonitor.OnEnabledChanged, AddressOf ActualizeUI
        AddHandler KeyMonitor.OnStatusChanged, AddressOf SetStatus
        AddHandler KeyMonitor.OnControlledShutDown, AddressOf FreeAllocatedResources
        AddHandler KeyCommandOperator.OnStatusChanged, AddressOf SetStatus
        ' StorageMonitor event handlers.
        ' StorageDeviceOperator event handlers.
        AddHandler CompositeMemberOperator.OnStatusChanged, AddressOf SetStatus

        Task.Run(
            Sub()
                KeyMonitor.Capture(True)
                'CompositeMemberOperator.SynchronizeMembers()
                SetStatus(StatusMessage.InfoInitializationEnd, False)
                SetStatus($"Hello, {Environment.UserName}! MSM is running in {IIf(ProcessAnalyzer.IsUserRoleAdmin, "admin", "user")} mode.", False)
            End Sub)
    End Sub

    Private Sub btn_Click(sender As Object, e As RoutedEventArgs) Handles btnKM.Click, btnSM.Click, btnORG.Click, btnAG.Click, btnHM.Click
        Dim btn As Primitives.ToggleButton = CType(sender, Primitives.ToggleButton)
        Select Case btn.Name
            Case NameOf(btnKM)
                KeyMonitor.Capture(btn.IsChecked)
            Case NameOf(btnORG)
                If Not CompositeMemberOperator.SynchronizationRunning Then
                    btn.IsChecked = New OrganizerMain() With {
                        .Left = Left,
                        .Top = Top}.ShowDialog()
                Else
                    btn.IsChecked = Not btn.IsChecked
                    SetStatus(StatusMessage.ErrorSyncRunning, True)
                End If
            Case NameOf(btnHM)
                btn.IsChecked = New HarmonicMixerMain() With {
                       .Left = Left,
                       .Top = Top}.ShowDialog()
            Case NameOf(btnSM)
                'sm.Capture(btn.IsChecked)
            Case NameOf(btnAG)
                btn.IsChecked = New ASCIIGeneratorMain() With {
                    .Left = Left,
                    .Top = Top}.ShowDialog()
        End Select
    End Sub

    Private Sub btnEnterCommand_Click(sender As Object, e As RoutedEventArgs) Handles btnEnterCommand.Click
        If Not String.IsNullOrWhiteSpace(tbMsmConsoleCommand.Text) Then
            Try
                Dim errMsg As String = "'" & tbMsmConsoleCommand.Text & "'" &
                    " - Syntax error! Use '" & ConsoleController.MSMHelp.Split(";")(0) & " <command>' for more information," &
                    " or '" & ConsoleController.MSMList.Split(";")(0) & "' for a complete command listing..."

                If _callCacheList.Contains(tbMsmConsoleCommand.Text) Then
                    _callCacheList.Remove(tbMsmConsoleCommand.Text)
                End If
                _callCacheList.Add(tbMsmConsoleCommand.Text)
                _callCacheIndex = _callCacheList.Count

                If Not tbMsmConsoleCommand.Text.Count(Function(ch) ch = """"c) Mod 2 = 0 Then
                    Throw New Exception(errMsg)
                End If

                Dim paramCallLst As New List(Of String)
                For Each paramObj In tbMsmConsoleCommand.Text.Split(""""c).ToArray.Select(
                    Function(item, index)
                        If index Mod 2 = 0 Then
                            ' Regular one-word parameters.
                            Return Regex.Replace(item, "\s+", " ").Trim.Split(" ").Where(Function(endItem) Not String.IsNullOrWhiteSpace(endItem))
                        Else
                            ' A quoted parameter with multiple intervals.
                            Return item
                        End If
                    End Function)

                    Select Case paramObj.GetType
                        Case GetType(String)
                            paramCallLst.Add(paramObj)
                        Case Else
                            paramCallLst.AddRange(paramObj)
                    End Select
                Next

                Task.Run(
                    Sub()
                        Try
                            SetStatus(ConsoleController.MethodInvocation(paramCallLst), False) ' If method returns a message, display it. Otherwise, just call it.
                        Catch ex As Exception When TypeOf ex Is ConsoleSyntaxException OrElse TypeOf ex Is InvalidCastException
                            SetStatus(errMsg, True)
                        End Try
                    End Sub)
            Catch ex As Exception
                SetStatus(ex.Message, True)
            End Try
            tbMsmConsoleCommand.Text = Nothing
        Else
            SetStatus(StatusMessage.InfoEmptyLine, False)
        End If
    End Sub

    Protected Overrides Sub OnPreviewKeyDown(e As KeyEventArgs)
        MyBase.OnPreviewKeyDown(e)
        If _callCacheList.Count > 0 Then
            Select Case e.Key
                Case Key.Up
                    _callCacheIndex = IIf(_callCacheIndex - 1 < 0, 0, _callCacheIndex - 1)
                    tbMsmConsoleCommand.Text = _callCacheList.Item(_callCacheIndex)
                    tbMsmConsoleCommand.SelectionStart = tbMsmConsoleCommand.Text.Length
                Case Key.Down
                    _callCacheIndex = IIf(_callCacheIndex > _callCacheList.Count - 2, _callCacheList.Count - 1, _callCacheIndex + 1)
                    tbMsmConsoleCommand.Text = _callCacheList.Item(_callCacheIndex)
                    tbMsmConsoleCommand.SelectionStart = tbMsmConsoleCommand.Text.Length
            End Select
        End If
        tbMsmConsoleCommand.Focus()
    End Sub

    Private Sub ManagerMain_Closing(sender As Object, e As CancelEventArgs)
        FreeAllocatedResources(New AutoResetEvent(True))
    End Sub

#End Region

#Region "Internal"

    ''' <summary>
    ''' Updates the ToggleButtons' check state when their corresponding actions were called without the UI.
    ''' </summary>
    ''' <param name="type">The class type that was called.</param>
    ''' <param name="status">The checked state of the ToggleButton after the action was completed.</param>
    Private Sub ActualizeUI(type As Type, status As Boolean)
        _sc.Send(New SendOrPostCallback(
            Sub()
                Select Case type
                    Case GetType(KeyMonitor)
                        btnKM.IsChecked = status
                    Case GetType(StorageMonitor)
                        btnSM.IsChecked = status
                End Select
            End Sub), Nothing)
    End Sub

    ''' <summary>
    ''' Updates the user when various events occur.
    ''' </summary>
    ''' <param name="message">The displayed message.</param>
    ''' <param name="err">Indicates whether the message was an error.</param>
    Private Sub SetStatus(message As String, err As Boolean)
        If String.IsNullOrWhiteSpace(message) OrElse message.Equals(StatusMessage.InfoTrue) OrElse message.Equals(StatusMessage.InfoFalse) Then
            Exit Sub  ' If empty or the result of an operation, do not display a message.
        End If
        _sc.Send(New SendOrPostCallback(
                 Sub()
                     Dim timeStamp As String = Date.Now.ToString("[HH:mm:ss] ")
                     message = message.Replace(StatusMessage.InfoEmptyLine, "")
                     If err Then
                         Dim rTR As New TextRange(rtbMsmConsole.Document.ContentEnd, rtbMsmConsole.Document.ContentEnd)
                         rTR.Text = $"{timeStamp}{message}{Environment.NewLine}"
                         rTR.ApplyPropertyValue(TextElement.ForegroundProperty, Brushes.Red)
                     Else
                         rtbMsmConsole.AppendText($"{timeStamp}{message}{Environment.NewLine}")
                     End If
                     rtbMsmConsole.ScrollToEnd()
                 End Sub), Nothing)
    End Sub

    ''' <summary>
    ''' Clears the MSM Console.
    ''' </summary>
    Private Sub ClearConsole()
        _sc.Send(New SendOrPostCallback(
                 Sub()
                     tbMsmConsoleCommand.Text = Nothing
                     rtbMsmConsole.Document.Blocks.Clear()
                     rtbMsmConsole.AppendText($"{Environment.NewLine}")
                 End Sub), Nothing)
    End Sub

    ''' <summary>
    ''' Releases all ManagerMain resources.
    ''' </summary>
    Private Sub Dispose()
        _callCacheList.Clear()
        RemoveHandler ConsoleController.OnStatusChanged, AddressOf SetStatus
        RemoveHandler ConsoleController.OnClearRequest, AddressOf ClearConsole
        RemoveHandler ConsoleController.OnMSMExit, AddressOf FreeAllocatedResources
        RemoveHandler KeyMonitor.OnEnabledChanged, AddressOf ActualizeUI
        RemoveHandler KeyMonitor.OnStatusChanged, AddressOf SetStatus
        RemoveHandler KeyMonitor.OnControlledShutDown, AddressOf FreeAllocatedResources
        RemoveHandler KeyCommandOperator.OnStatusChanged, AddressOf SetStatus
        ' StorageMonitor event handlers.
        ' StorageDeviceOperator event handlers.
        RemoveHandler CompositeMemberOperator.OnStatusChanged, AddressOf SetStatus
    End Sub

    ''' <summary>
    ''' Called when closing the application. Ensures all resources have been disposed, before continuing.
    ''' </summary>
    ''' <param name="e">The event will be blocked until set.</param>
    Private Sub FreeAllocatedResources(e As AutoResetEvent)
        CompositeMemberOperator.Dispose()
        KeyCommandOperator.Dispose()
        KeyMonitor.Dispose()
        Dispose()
        e.Set()
    End Sub

#End Region

End Class
