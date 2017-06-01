Imports System.IO
Imports System.Text.RegularExpressions
Imports System.Threading
Imports System.Windows.Forms
Imports MasterLib.FilesystemManagement

Namespace HarmonicMixing

    ''' <summary>
    ''' HarmonicMixer's main UI Class.
    ''' </summary>
    Class HarmonicMixerMain

#Region "Declarations"

        Private Const EXT_MP3 As String = ".MP3"
        Private Const EXT_FLAC As String = ".FLAC"
        Private Const EXT_M4A As String = ".M4A"

        ''' <summary>
        ''' Provides status messages that indicate the currently running/last completed actions.
        ''' </summary>
        Private NotInheritable Class StatusMessage

            Private Sub New()
            End Sub

            Public Const InfoSelectDirectory As String = "Please select the music library's directory."
            Public Const ErrorInputDirectory As String = "Invalid input directory."
            Public Const ErrorNoFiles As String = "No files found in the input directory."

        End Class

        Private _sc As SynchronizationContext = SynchronizationContext.Current
        Private _fullCollection As New HashSet(Of TrackID3Data)

#End Region

#Region "User Interface"

        Private Sub Window_Initialized(sender As Object, e As EventArgs)
            Dim theFolderBrowser As New FolderBrowserDialog
            theFolderBrowser.Description = StatusMessage.InfoSelectDirectory
            theFolderBrowser.ShowNewFolderButton = False
            theFolderBrowser.RootFolder = Environment.SpecialFolder.Desktop
            While True
                If theFolderBrowser.ShowDialog = Forms.DialogResult.OK Then
                    Dim inputDir As String = theFolderBrowser.SelectedPath
                    If String.IsNullOrWhiteSpace(inputDir) OrElse Not Directory.Exists(inputDir) Then
                        MsgBox(StatusMessage.ErrorInputDirectory, MsgBoxStyle.Critical, "Error")
                        Continue While
                    End If

                    Dim mediaFiles() As String = Directory.GetFiles(inputDir).Where(Function(fName) {EXT_MP3, EXT_FLAC, EXT_M4A}.Contains(Path.GetExtension(fName).ToUpper)).ToArray
                    If mediaFiles Is Nothing Then
                        MsgBox(StatusMessage.ErrorNoFiles, MsgBoxStyle.Critical, "Error")
                        Continue While
                    End If

                    Task.Run(
                        Sub()
                            For Each fName In mediaFiles
                                _fullCollection.Add(MediaFileEditor.GetTrackID3Data(fName))
                            Next
                            _sc.Send(
                            New SendOrPostCallback(
                            Sub()
                                dgCollection.ItemsSource = _fullCollection
                            End Sub), Nothing)
                        End Sub)
                    Exit While
                Else
                    Close()
                End If
            End While
        End Sub

        Private Sub dgCollection_MouseDoubleClick(sender As Object, e As MouseButtonEventArgs) Handles dgCollection.MouseDoubleClick
            SelectedTrackChanged()
        End Sub

        Protected Overrides Sub OnPreviewKeyDown(e As Input.KeyEventArgs)
            MyBase.OnPreviewKeyDown(e)
            Select Case e.Key
                Case Key.Up
                    dgCollection.Focus()
                    If dgCollection.SelectedIndex > 0 Then
                        dgCollection.SelectedIndex -= 1
                        dgCollection.ScrollIntoView(dgCollection.SelectedItem)
                    End If
                Case Key.Down
                    dgCollection.Focus()
                    If dgCollection.SelectedIndex < dgCollection.Items.Count - 1 Then
                        dgCollection.SelectedIndex += 1
                        dgCollection.ScrollIntoView(dgCollection.SelectedItem)
                    End If
                Case Key.Enter
                    SelectedTrackChanged()
                    e.Handled = True
            End Select
        End Sub

        Private Sub btnLock_Click(sender As Object, e As RoutedEventArgs) Handles btnLock.Click
            If btnLock.IsChecked Then
                If tbBPM.Text.Length > 1 Then
                    btnLock.Content = "🔒"
                    btnLock.Foreground = Brushes.Green
                Else
                    btnLock.IsChecked = False
                End If
            Else
                btnLock.Content = "🔓"
                btnLock.Foreground = Brushes.Red
            End If
        End Sub

        Private Sub tbBPM_TextChanged(sender As Object, e As TextChangedEventArgs) Handles tbBPM.TextChanged
            If tbBPM.Text.Length > 1 Then
                Dim cTrackID3 As TrackID3Data = CType(tbCurrentTrack.Tag, TrackID3Data)
                Dim adjKeys() As String = MediaFileEditor.GetTrackAdjacentKeysAtBMP(cTrackID3.Key, cTrackID3.BeatsPerMinute, tbBPM.Text, False)
                Dim hsCompatible As New HashSet(Of TrackID3Data)
                For Each tID3 In _fullCollection
                    If adjKeys.Contains(MediaFileEditor.GetTrackAdjacentKeysAtBMP(tID3.Key, tID3.BeatsPerMinute, tbBPM.Text, True)(0)) Then
                        hsCompatible.Add(tID3)
                    End If
                Next
                dgCollection.ItemsSource = hsCompatible
            End If
        End Sub

#End Region

#Region "Internal"

        Private Sub NumericOnly(sender As Object, e As TextCompositionEventArgs)
            e.Handled = New Regex("[^0-9]").IsMatch(e.Text)
        End Sub

        Private Sub SelectedTrackChanged()
            If Not dgCollection.SelectedItem Is Nothing Then
                Dim cTrackID3 As TrackID3Data = CType(dgCollection.SelectedItem, TrackID3Data)
                tbCurrentTrack.Tag = cTrackID3 ' Save the current selected track in the CurrentTrack textbox
                tbCurrentTrack.Text = cTrackID3.Name
                tbGain.Text = cTrackID3.Gain
                If Not btnLock.IsChecked Then
                    tbBPM.Text = cTrackID3.BeatsPerMinute
                End If
            End If
        End Sub

#End Region

    End Class

End Namespace