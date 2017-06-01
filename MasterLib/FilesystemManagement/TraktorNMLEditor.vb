Imports System.IO
Imports System.Xml
Imports MasterLib.APIManagement

Namespace FilesystemManagement

    ''' <summary>
    ''' Modifies Traktor NML playlist files. When loading the file in Traktor it can modify the corresponding files' ID3 tags.
    ''' </summary>
    Public NotInheritable Class TraktorNMLEditor

#Region "Declarations"

        ''' <summary>
        ''' Provides status messages that indicate the currently running/last completed actions.
        ''' </summary>
        Private NotInheritable Class StatusMessage

            Private Sub New()
            End Sub

            Public Const InfoGenerateStart As String = "NML generation started..."
            Public Const InfoNodeModified As String = "Node modified for"
            Public Const InfoSuccess As String = "NML successfully generated."
            Public Const ErrorGeneric As String = "An error has occurred:"
            Public Const ErrorInputFile As String = "No input NML file found..."

        End Class

        Private Shared CamelotOuter() As String = {"C", "G", "D", "A", "E", "B", "F#", "C#", "G#", "D#", "A#", "F"}
        Private Shared CamelotInner() As String = {"Am", "Em", "Bm", "F#m", "C#m", "G#m", "D#m", "A#m", "Fm", "Cm", "Gm", "Dm"}

        Shared Event OnStatusChanged(message As String, isError As Boolean)

#End Region

#Region "Main"

        ''' <summary>
        ''' Generates a new Traktor NML file, which has all tracks' extra node values cleared. Those are: track number, release date, remixer, lyrics and publisher.
        ''' </summary>
        ''' <param name="inNML">Input NML playlist file.</param>
        ''' <param name="outNML">Modified NML playlist file.</param>
        Shared Sub ClearTracksExtraTags(inNML As String, outNML As String)

            Select Case True
                Case Not File.Exists(inNML)
                    RaiseEvent OnStatusChanged(StatusMessage.ErrorInputFile, True)
                    Exit Sub
            End Select

            Dim nmlDoc As New XmlDocument
            Try
                nmlDoc.Load(inNML)
                RaiseEvent OnStatusChanged(StatusMessage.InfoGenerateStart, False)
                For Each trackNode As XmlNode In nmlDoc.DocumentElement.SelectSingleNode(APITraktorNML.NodeCollection).ChildNodes()
                    Dim fileName As String = trackNode.SelectSingleNode(APITraktorNML.NodeLocation).Attributes.GetNamedItem(APITraktorNML.NodeFile).Value

                    If Not trackNode.SelectSingleNode(APITraktorNML.NodeAlbum) Is Nothing Then
                        If CType(trackNode.SelectSingleNode(APITraktorNML.NodeAlbum), XmlElement).HasAttribute(APITraktorNML.NodeTrackNum) Then
                            CType(trackNode.SelectSingleNode(APITraktorNML.NodeAlbum), XmlElement).RemoveAttribute(APITraktorNML.NodeTrackNum)
                        End If
                    End If

                    For Each nodeName In {APITraktorNML.NodeReleaseDate, APITraktorNML.NodeRemixer, APITraktorNML.NodeLyrics, APITraktorNML.NodePublisher}
                        If CType(trackNode.SelectSingleNode(APITraktorNML.NodeInfo), XmlElement).HasAttribute(nodeName) Then
                            CType(trackNode.SelectSingleNode(APITraktorNML.NodeInfo), XmlElement).RemoveAttribute(nodeName)
                        End If
                    Next
                    RaiseEvent OnStatusChanged($"{StatusMessage.InfoNodeModified} '{fileName}'", False)
                Next
                nmlDoc.Save(outNML)

                RaiseEvent OnStatusChanged(StatusMessage.InfoSuccess, False)
            Catch ex As Exception
                RaiseEvent OnStatusChanged($"{StatusMessage.ErrorGeneric} {ex.Message}", True)
            End Try
        End Sub

        ''' <summary>
        ''' Generates a new Traktor NML file, which has all tracks' album values replaced with their Key and Gain nodes' values.
        ''' </summary>
        ''' <param name="inNML">Input NML playlist file.</param>
        ''' <param name="outNML">Modified NML playlist file.</param>
        Shared Sub SetAlbumToKeyGainPair(inNML As String, outNML As String)

            Select Case True
                Case Not File.Exists(inNML)
                    RaiseEvent OnStatusChanged(StatusMessage.ErrorInputFile, True)
                    Exit Sub
            End Select

            Dim nmlDoc As New XmlDocument
            Try
                nmlDoc.Load(inNML)
                RaiseEvent OnStatusChanged(StatusMessage.InfoGenerateStart, False)
                For Each trackNode As XmlNode In nmlDoc.DocumentElement.SelectSingleNode(APITraktorNML.NodeCollection).ChildNodes()
                    Dim fileName As String = trackNode.SelectSingleNode(APITraktorNML.NodeLocation).Attributes.GetNamedItem(APITraktorNML.NodeFile).Value
                    Dim keyValue As String = trackNode.SelectSingleNode(APITraktorNML.NodeInfo).Attributes.GetNamedItem(APITraktorNML.NodeKey).Value
                    Dim gainValue As Double = Math.Round(CDbl(trackNode.SelectSingleNode(APITraktorNML.NodeLoudness).Attributes.GetNamedItem(APITraktorNML.NodeAnalyzedDB).Value), 1)
                    Dim gainCorrection As Double
                    Dim gainMask As String = ""
                    Dim tagResultValue As String = ""

                    If CamelotOuter.Contains(keyValue) Then
                        tagResultValue = $"{(Array.IndexOf(CamelotOuter, keyValue) + 1).ToString("00")}B | "
                    Else
                        tagResultValue = $"{(Array.IndexOf(CamelotInner, keyValue) + 1).ToString("00")}A | "
                    End If

                    If gainValue > 0 Then
                        gainMask = "+0.0"
                        gainCorrection = 0.1
                    ElseIf gainValue < 0 Then
                        gainMask = "0.0"
                        gainCorrection = -0.1
                    Else
                        gainMask = "±0.0"
                    End If

                    If gainValue * 10 Mod 2 = 0 Then
                        tagResultValue &= gainValue.ToString(gainMask)
                    Else
                        tagResultValue &= (gainValue + gainCorrection).ToString(gainMask)
                    End If

                    Dim albumNode As XmlNode = trackNode.SelectSingleNode(APITraktorNML.NodeAlbum)
                    If albumNode Is Nothing Then
                        Dim elem As XmlElement = nmlDoc.CreateElement(APITraktorNML.NodeAlbum)
                        elem.SetAttribute(APITraktorNML.NodeTitle, tagResultValue)
                        trackNode.AppendChild(elem)
                    Else
                        CType(albumNode, XmlElement).SetAttribute(APITraktorNML.NodeTitle, tagResultValue)
                    End If
                    RaiseEvent OnStatusChanged($"{StatusMessage.InfoNodeModified} '{fileName}'", False)
                Next
                nmlDoc.Save(outNML)
                RaiseEvent OnStatusChanged(StatusMessage.InfoSuccess, False)
            Catch ex As Exception
                RaiseEvent OnStatusChanged($"{StatusMessage.ErrorGeneric} {ex.Message}", True)
            End Try
        End Sub

#End Region

#Region "Internal"

        ''' <summary>
        ''' Cannot be initialized directly.
        ''' </summary>
        Private Sub New()
        End Sub

#End Region

    End Class

End Namespace
