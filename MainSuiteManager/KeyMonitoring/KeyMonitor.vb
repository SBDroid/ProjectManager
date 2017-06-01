Imports System.Runtime.InteropServices
Imports System.Text.RegularExpressions
Imports System.Threading
Imports System.Windows.Forms
Imports MasterLib.FilesystemManagement

Namespace KeyMonitoring

    ''' <summary>
    ''' Captures keyboard input by directly using WinAPI functions. Usage:
    ''' In order to enable capturing, the Capture method needs to be called with the appropriate parameter.
    ''' When predefined commands (starting with the ampersand symbol) or key combinations are used, different actions are performed. 
    ''' Currently, there are 3 types of commands:
    ''' - TEXT - Print a predefined set of symbols, when the command is executed;
    ''' - OS - Restart/Shutdown the system or perform standard Windows diagnostics;
    ''' - FILE - Perform operations on media files (MP3, FLAC, M4A). Uses the taglib-sharp library (http://www.nuget.org/packages/taglib/);
    ''' Additionally, specific key combinations affect the Clipboard. 
    ''' After Control + C/X is used and it's followed by Control + Alt + Numpad 0-9, the contents of the Clipboard are saved internally.
    ''' Using Control + Alt + Numpad 0-9 subsequently/independently gets the internal content of the specified position and sets it as the current Clipboard.
    ''' </summary>
    Public NotInheritable Class KeyMonitor

#Region "Declarations"

        <DllImport("user32.dll", CharSet:=CharSet.Auto, SetLastError:=True)>
        Private Shared Function SetWindowsHookEx(idHook As Integer, lpfn As LowLevelKeyboardProc, hMod As IntPtr, dwThreadId As UInteger) As IntPtr
        End Function

        <DllImport("user32.dll", CharSet:=CharSet.Auto, SetLastError:=True)>
        Private Shared Function UnhookWindowsHookEx(hhk As IntPtr) As <MarshalAs(UnmanagedType.Bool)> Boolean
        End Function

        <DllImport("user32.dll", CharSet:=CharSet.Auto, SetLastError:=True)>
        Private Shared Function CallNextHookEx(hhk As IntPtr, nCode As Integer, wParam As IntPtr, lParam As IntPtr) As IntPtr
        End Function

        <DllImport("kernel32.dll", CharSet:=CharSet.Auto, SetLastError:=True)>
        Private Shared Function GetModuleHandle(lpModuleName As String) As IntPtr
        End Function

        Private Const TOTAL_CLIPBOARDS As Integer = 9
        Private Const WH_KEYBOARD_LL As Integer = 13
        Private Const WM_KEYDOWN As Integer = &H100
        Private Const ASCII_7 As Integer = 55
        Private Const ASCII_A As Integer = 65
        Private Const ASCII_C As Integer = 67
        Private Const ASCII_X As Integer = 88
        Private Const ASCII_Z As Integer = 90
        Private Const ASCII_NUM0 As Integer = 96
        Private Const ASCII_NUM9 As Integer = 105

        ''' <summary>
        ''' Provides status messages that indicate the currently running/last completed actions.
        ''' </summary>
        Private NotInheritable Class StatusMessage

            Private Sub New()
            End Sub

            Public Const InfoCaptureEnabled As String = "Key capturing is now enabled."
            Public Const InfoCaptureDisabled As String = "Key capturing is now disabled."
            Public Const InfoDrawTextSuccess As String = "Successfully executed text command."
            Public Const InfoOSCmdStart As String = "OS command executing..."
            Public Const InfoIntClipboardSetSuccess As String = "Internal Clipboard set successfully at index:"
            Public Const InfoIntClipboardGetSuccess As String = "Internal Clipboard retrieved successfully at index:"
            Public Const InfoIntClipboardReset As String = "Internal Clipboards have been reset."
            Public Const ErrorExtClipboardGet As String = "Current Clipboard is empty."
            Public Const ErrorFileOpUnknown As String = "Unknown file operation..."
            Public Const ErrorIntClipboardGet As String = "Internal Clipboard is empty. Index:"

        End Class

        ''' <summary>
        ''' Holds a Clipboard fragment of data with a specified format.
        ''' </summary>
        Private Class ClipboardPart

            ''' <summary>
            ''' Requires the fragment's data and its format.
            ''' </summary>
            ''' <param name="fmt">Object format.</param>
            ''' <param name="dt">Object data.</param>
            Public Sub New(fmt As String, dt As Object)
                Format = fmt
                Data = dt
            End Sub

            ReadOnly Property Format As String
            ReadOnly Property Data As Object
        End Class

        Private Delegate Function LowLevelKeyboardProc(nCode As Integer, wParam As IntPtr, lParam As IntPtr) As IntPtr

        Private Shared _lFuncDlg As LowLevelKeyboardProc = AddressOf HookCallback ' Delegate used for the listening function (HookCallback).
        Private Shared _hookID As IntPtr = SetHook(_lFuncDlg) ' Hook associated with the listening function.
        Private Shared _cmdList As List(Of KeyCommand) = KeyCommandOperator.ActiveCommandsList ' A list of all defined commands.
        Private Shared _cbDictionary As Dictionary(Of Integer, List(Of ClipboardPart)) = LoadCBDictionary() ' A dictionary containing data from multiple Clipboards.
        Private Shared _beginCommand As Boolean ' Indicates whether the detection of key commands is enabled/disabled.
        Private Shared _strCommand As String ' Used to build a command ID.
        Private Shared _beginMulCB As Boolean ' Indicates whether the detection of multiple Clipboards is enabled/disabled.
        Private Shared _capture As Boolean ' Indicates whether key monitoring is enabled.
        Private Shared _waitHandle As AutoResetEvent

        Shared Event OnEnabledChanged(type As Type, status As Boolean) ' Provides information whether the class' main functionalities are enabled.
        Shared Event OnStatusChanged(statusMessage As String, err As Boolean) ' Provides status messages to the user of the KeyMonitor class.
        Shared Event OnControlledShutDown(e As AutoResetEvent) ' Provides a starting point to the user of the KeyMonitor class for disposing resources.

#End Region

#Region "Main"

        ''' <summary>
        ''' Enables/Disables key detection.
        ''' </summary>
        ''' <param name="enabled">Key detection flag.</param>
        <MethodAlias(ConsoleController.KeyMonitorCapture)>
        Shared Sub Capture(enabled As Boolean)
            _capture = enabled
            If _capture Then
                _cmdList = KeyCommandOperator.ActiveCommandsList ' Reload commands, in case they were modified.
                RaiseEvent OnStatusChanged(StatusMessage.InfoCaptureEnabled, False)
            Else
                RaiseEvent OnStatusChanged(StatusMessage.InfoCaptureDisabled, False)
            End If
            RaiseEvent OnEnabledChanged(GetType(KeyMonitor), _capture) ' UI actualization.
        End Sub

        ''' <summary>
        ''' Initializes/Resets the internal clipboard.
        ''' </summary>
        <MethodAlias(ConsoleController.KeyMonitorClipboardReset)>
        Shared Sub ClipboardReset()
            _cbDictionary = LoadCBDictionary()
            RaiseEvent OnStatusChanged(StatusMessage.InfoIntClipboardReset, False)
        End Sub

        ''' <summary>
        ''' Releases all KeyMonitor resources.
        ''' </summary>
        Shared Sub Dispose()
            UnhookWindowsHookEx(_hookID) ' Release the allocated hook.
            _cmdList.Clear()
            _cbDictionary.Clear()
        End Sub

#End Region

#Region "Internal"

        ''' <summary>
        ''' Cannot be initialized directly.
        ''' </summary>
        Private Sub New()
        End Sub

        ''' <summary>
        ''' Gets a new hook for the listening function. Requires a matching delegate.
        ''' </summary>
        ''' <param name="lFuncDlg">Delegate of the listening function.</param>
        ''' <returns>Returns a hook associated with the delegate.</returns>
        Private Shared Function SetHook(lFuncDlg As LowLevelKeyboardProc) As IntPtr
            Using curProcess As Process = Process.GetCurrentProcess()
                Using curModule As ProcessModule = curProcess.MainModule
                    Return SetWindowsHookEx(WH_KEYBOARD_LL, lFuncDlg, GetModuleHandle(curModule.ModuleName), 0)
                End Using
            End Using
        End Function

        ''' <summary>
        ''' Main listening function.
        ''' </summary>
        ''' <param name="nCode">The current key code.</param>
        ''' <param name="wParam">wParam.</param>
        ''' <param name="lParam">lParam.</param>
        ''' <returns>Returns 1 when the default hook procedure is skipped or the result from CallNextHookEx in user32.dll.</returns>
        Private Shared Function HookCallback(nCode As Integer, wParam As IntPtr, lParam As IntPtr) As IntPtr
            If _capture AndAlso nCode >= 0 AndAlso wParam = CType(WM_KEYDOWN, IntPtr) Then
                Dim vkCode As Integer = Marshal.ReadInt32(lParam) ' Get the character code.

                If Control.ModifierKeys = Keys.Shift AndAlso vkCode = ASCII_7 Then
                    ' Shift + 7 (&).
                    _beginCommand = True  ' Begin command detection.
                    _strCommand = "" ' Reset command string.
                    _beginMulCB = False ' End Clipboard detection.
                    Return CallNextHookEx(_hookID, nCode, wParam, lParam) ' Call the hook normally.
                ElseIf Control.ModifierKeys = Keys.Control AndAlso (vkCode = ASCII_C OrElse vkCode = ASCII_X) Then
                    ' Control + C, Control + X.
                    _beginCommand = False  ' End command detection.
                    _strCommand = "" ' Reset command string.
                    _beginMulCB = True ' Begin Clipboard detection.
                    Return CallNextHookEx(_hookID, nCode, wParam, lParam) ' Call the hook normally.
                End If

                If _beginCommand Then
                    If vkCode >= ASCII_A AndAlso vkCode <= ASCII_Z Then
                        ' After Shift + 7 (&) and a valid symbol, add it to the _strCommand field.
                        _strCommand += ChrW(vkCode)
                        Dim _tmpHolder As KeyCommand = _cmdList.Find(Function(ch As KeyCommand)
                                                                         Return ch.ID = _strCommand ' If there is a matching command ID.
                                                                     End Function)
                        Select Case _tmpHolder?.Type
                            Case KeyCommandType.TEXT
                                Return ExecTextCmd(_tmpHolder.ID.Length, _tmpHolder.Action)  ' The hook is skipped.
                            Case KeyCommandType.OS
                                Return ExecOSCmd(_tmpHolder.ID.Length, _tmpHolder.Action) ' The hook is skipped.
                            Case KeyCommandType.FILE
                                Return ExecFileCmd(_tmpHolder.Action) ' The hook is skipped.
                            Case Else
                                ' Call the hook normally.
                        End Select
                    Else
                        ' After Shift + 7 (&) and an invalid symbol follows, reset and call the hook normally.
                        _beginCommand = False  ' End command detection.
                        _strCommand = "" ' Reset command string.
                    End If
                ElseIf _beginMulCB Then
                    If Not Control.ModifierKeys = Nothing Then
                        ' If there were modifiers pressed after Control + C or Control + X (as indicated by _beginMulCB = True)
                        If Control.ModifierKeys = (Keys.Control Or Keys.Alt) AndAlso vkCode >= ASCII_NUM0 AndAlso vkCode <= ASCII_NUM9 Then
                            ' If the modifiers are specifically Control + Alt + 0-9, the corresponding Clipboard is set and the hook is skipped.
                            Return ExecSetClipBoard(vkCode - ASCII_NUM0)
                        End If
                    Else
                        ' If no modifiers are used next, reset and call the hook normally.
                        _beginMulCB = False ' End clipboard detection.
                    End If
                ElseIf Not _beginCommand AndAlso Not _beginMulCB Then
                    If Control.ModifierKeys = (Keys.Control Or Keys.Alt) AndAlso vkCode >= ASCII_NUM0 AndAlso vkCode <= ASCII_NUM9 Then
                        ' If an Alt + Num is pressed independently, get the corresponding Clipboard and call the hook normally.
                        ExecGetClipBoard(vkCode - ASCII_NUM0)
                    End If
                End If
            End If
            Return CallNextHookEx(_hookID, nCode, wParam, lParam) ' Call the hook normally.
        End Function

        ''' <summary>
        ''' Replaces the currently written text with an alternative one.
        ''' </summary>
        ''' <param name="lenCmd">Length of the command.</param>
        ''' <param name="txt">Text to be displayed.</param>
        ''' <returns>Returns 1 (the default hook procedure is skipped).</returns>
        Private Shared Function ExecTextCmd(lenCmd As Integer, txt As String) As IntPtr
            _beginCommand = False  ' End command detection.
            _strCommand = "" ' Reset command string.
            Try
                SendKeys.SendWait($"{{BS {lenCmd}}}")
                Thread.Sleep(50)
                Clipboard.SetText(txt)
                SendKeys.SendWait("^(v)")
                RaiseEvent OnStatusChanged(StatusMessage.InfoDrawTextSuccess, False)
            Catch ex As Exception
                RaiseEvent OnStatusChanged(ex.Message, True)
            End Try
            Return 1
        End Function

        ''' <summary>
        ''' Windows operations.
        ''' </summary>
        ''' <param name="lenCmd">Length of the command.</param>
        ''' <param name="txtArg">Additional arguments of the command.</param>
        ''' <returns>Returns 1 (the default hook procedure is skipped).</returns>
        Private Shared Function ExecOSCmd(lenCmd As Integer, txtArg As String) As IntPtr
            _beginCommand = False  ' End command detection.
            _strCommand = "" ' Reset command string.
            Try
                SendKeys.SendWait($"{{BS {lenCmd}}}")
                RaiseEvent OnStatusChanged(StatusMessage.InfoOSCmdStart, False)
                Dim proc As New Process
                proc.StartInfo.FileName = "cmd.exe"
                proc.StartInfo.Arguments = Regex.Replace(txtArg, "\w+[.](?i)exe", $"{Environment.GetFolderPath(Environment.SpecialFolder.Windows)}\sysnative\$&")
                proc.StartInfo.Verb = "runas"
                proc.StartInfo.UseShellExecute = True
                _waitHandle = New AutoResetEvent(False)
                RaiseEvent OnControlledShutDown(_waitHandle)
                _waitHandle.WaitOne()
                proc.Start()
            Catch ex As Exception
                RaiseEvent OnStatusChanged(ex.Message, True)
            End Try
            Return 1
        End Function

        ''' <summary>
        ''' File operations on media files.
        ''' </summary>
        ''' <param name="opTxt">Operation ID.</param>
        ''' <returns>Returns 1 (the default hook procedure is skipped).</returns>
        Private Shared Function ExecFileCmd(opTxt As String) As IntPtr
            _beginCommand = False  ' End command detection.
            _strCommand = "" ' Reset command string.
            ' Sequence to get the working directory's string.
            SendKeys.SendWait("%(d)")
            SendKeys.SendWait("^(c)")
            SendKeys.SendWait("{TAB 4}")
            Dim dirStr As String = Clipboard.GetText() ' Working directory name.

            Task.Run(
                Sub()
                    AddHandler MediaFileEditor.OnStatusChanged, AddressOf RaiseRemoteEvents
                    AddHandler GenericFileProcessor.OnStatusChanged, AddressOf RaiseRemoteEvents
                    Try
                        Select Case opTxt
                            Case NameOf(MediaFileEditor.SetTracksFilename)
                                MediaFileEditor.SetTracksFilename(dirStr) ' Step 0
                            Case NameOf(MediaFileEditor.ClearTracksExtraTags)
                                MediaFileEditor.ClearTracksExtraTags(dirStr) ' Step 1
                            Case NameOf(MediaFileEditor.SetTracksArtistAndTitleTagsFromFilename)
                                MediaFileEditor.SetTracksArtistAndTitleTagsFromFilename(dirStr) ' Step 2
                            Case NameOf(MediaFileEditor.FormatTracksArtistAndTitleTags)
                                MediaFileEditor.FormatTracksArtistAndTitleTags(dirStr) ' Step 3
                            Case NameOf(MediaFileEditor.SetTracksCommentTags)
                                MediaFileEditor.SetTracksCommentTags(dirStr) ' Step 4
                            Case NameOf(GenericFileProcessor.DuplicateFileFinder)
                                GenericFileProcessor.DuplicateFileFinder(dirStr)
                            Case Else
                                RaiseEvent OnStatusChanged(StatusMessage.ErrorFileOpUnknown, True)
                        End Select
                    Catch ex As Exception
                        RaiseEvent OnStatusChanged(ex.Message, True)
                    End Try
                    RemoveHandler MediaFileEditor.OnStatusChanged, AddressOf RaiseRemoteEvents
                    RemoveHandler GenericFileProcessor.OnStatusChanged, AddressOf RaiseRemoteEvents
                End Sub)
            Return 1
        End Function

        ''' <summary>
        ''' Initializes the Clipboard dictionary for multiple Clipboard instances.
        ''' </summary>
        ''' <returns>Returns the Clipboard dictionary.</returns>
        Private Shared Function LoadCBDictionary() As Dictionary(Of Integer, List(Of ClipboardPart))
            Dim tmpDictionary = New Dictionary(Of Integer, List(Of ClipboardPart))
            For totalCB As Integer = 0 To TOTAL_CLIPBOARDS
                tmpDictionary.Add(totalCB, New List(Of ClipboardPart))
            Next
            Return tmpDictionary
        End Function

        ''' <summary>
        ''' Sets the contents of the current Clipboard into an internal Clipboard object.
        ''' </summary>
        ''' <param name="cbIdx">Save index.</param>
        ''' <returns>Returns 1 (the default hook procedure is skipped).</returns>
        Private Shared Function ExecSetClipBoard(cbIdx As Integer) As Boolean
            _beginMulCB = False
            Try
                Dim idObj As IDataObject = Clipboard.GetDataObject()
                If idObj?.GetFormats?.Count > 0 Then
                    _cbDictionary.Item(cbIdx) = New List(Of ClipboardPart)
                    For Each fmt As String In idObj.GetFormats
                        _cbDictionary.Item(cbIdx).Add(New ClipboardPart(fmt, idObj.GetData(fmt)))
                    Next
                Else
                    RaiseEvent OnStatusChanged(StatusMessage.ErrorExtClipboardGet, True)
                End If
                RaiseEvent OnStatusChanged($"{StatusMessage.InfoIntClipboardSetSuccess} {cbIdx}", False)
            Catch ex As Exception
                RaiseEvent OnStatusChanged(ex.Message, True)
            End Try
            Return 1
        End Function

        ''' <summary>
        ''' Gets the contents of an internal Clipboard object.
        ''' </summary>
        ''' <param name="cbIdx">Returns 1 (the default hook procedure is skipped).</param>
        Private Shared Sub ExecGetClipBoard(cbIdx As Integer)
            Try
                If _cbDictionary.Item(cbIdx)?.Count > 0 Then
                    Dim dObj As New DataObject
                    For Each cbPt As ClipboardPart In _cbDictionary.Item(cbIdx)
                        dObj.SetData(cbPt.Format, cbPt.Data)
                    Next
                    Clipboard.SetDataObject(dObj)
                    RaiseEvent OnStatusChanged($"{StatusMessage.InfoIntClipboardGetSuccess} {cbIdx}", False)
                Else
                    RaiseEvent OnStatusChanged($"{StatusMessage.ErrorIntClipboardGet} {cbIdx}", True)
                End If
            Catch ex As Exception
                RaiseEvent OnStatusChanged(ex.Message, True)
            End Try
        End Sub

        ''' <summary>
        ''' Raise other classes' OnStatusChanged events.
        ''' </summary>
        ''' <param name="message">Status message.</param>
        ''' <param name="isError">Status type.</param>
        Private Shared Sub RaiseRemoteEvents(message As String, isError As Boolean)
            RaiseEvent OnStatusChanged(message, isError)
        End Sub

#End Region

    End Class

End Namespace