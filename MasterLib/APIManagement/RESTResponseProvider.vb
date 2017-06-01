Imports System.IO
Imports System.Net
Imports System.Threading
Imports System.Web.Script.Serialization
Imports System.Xml
Imports MasterLib.FilesystemManagement

Namespace APIManagement

    ''' <summary>
    ''' Calls REST APIs with a provided request string. Returns resources in different formats.
    ''' </summary>
    Public NotInheritable Class RESTResponseProvider

#Region "Declarations"

        Private Const WEB_RESPONSE_TIMEOUT As Integer = 10 * 1000 ' In milliseconds.
        Private Const WEB_REQUEST_DELAY As Integer = 600 ' In milliseconds.

#End Region

#Region "Main"

        ''' <summary>
        ''' Sends a web request and parses the response accordingly.
        ''' </summary>
        ''' <param name="reqStr">Web request string.</param>
        ''' <param name="respType">Expected response type.</param>
        ''' <returns>Object determined by the response type parameter.</returns>
        Shared Function GetAPIResponse(reqStr As String, respType As ResponseType) As Object
            Dim respObj As Object = Nothing
            Dim wReq As HttpWebRequest = Nothing
            If respType = ResponseType.IMAGE Then
                Dim imgURI As Uri = Nothing
                If Uri.TryCreate(reqStr, UriKind.Absolute, imgURI) Then
                    wReq = CType(WebRequest.Create(imgURI), HttpWebRequest)
                Else
                    Return ImageProcessor.GetDefaultImageAsByteArr(DefaultImage.NotFound)
                End If
            Else
                wReq = CType(WebRequest.Create(reqStr), HttpWebRequest)
            End If
            wReq.Timeout = WEB_RESPONSE_TIMEOUT
            wReq.Proxy = WebRequest.GetSystemWebProxy()
            wReq.Proxy.Credentials = CredentialCache.DefaultCredentials
            wReq.UserAgent = NameOf(RESTResponseProvider)
            If respType = ResponseType.JSON Then Thread.Sleep(WEB_REQUEST_DELAY)
            Try
                Using wResp As WebResponse = wReq.GetResponse()
                    Using ioReader As Stream = wResp.GetResponseStream()
                        Select Case respType
                            Case ResponseType.XML
                                Using xmlReader As New XmlTextReader(ioReader)
                                    Dim doc As New XmlDocument
                                    doc.Load(xmlReader)
                                    respObj = doc
                                    xmlReader.Close()
                                End Using
                            Case ResponseType.JSON
                                Using tr As TextReader = New StreamReader(ioReader)
                                    Dim jsonSrl As New JavaScriptSerializer
                                    respObj = jsonSrl.Deserialize(Of Dictionary(Of String, Object))(tr.ReadToEnd())
                                    tr.Close()
                                End Using
                            Case ResponseType.IMAGE
                                Using ms As New MemoryStream
                                    ioReader.CopyTo(ms)
                                    respObj = ms.ToArray
                                    ms.Close()
                                End Using
                        End Select
                        ioReader.Close()
                    End Using
                    wResp.Close()
                End Using
            Catch ex As WebException
                If respType = ResponseType.IMAGE Then respObj = ImageProcessor.GetDefaultImageAsByteArr(DefaultImage.NotFound)
            End Try
            Return respObj
        End Function

#End Region

#Region "Internal"

        ''' <summary>
        ''' Cannot be initialized directly.
        ''' </summary>
        Private Sub New()
        End Sub

#End Region

    End Class

    ''' <summary>
    ''' Determines the expected response from a web request.
    ''' </summary>
    Public Enum ResponseType
        ''' <summary>
        ''' XML will be returned as an XmlDocument type.
        ''' </summary>
        XML = 0
        ''' <summary>
        ''' JSON will be returned as a Dictionary(Of String, Object) type.
        ''' </summary>
        JSON = 1
        ''' <summary>
        ''' Image will be returned as a byte array type.
        ''' </summary>
        IMAGE = 2
    End Enum

End Namespace

