Imports System.Drawing
Imports System.Drawing.Drawing2D
Imports System.Drawing.Imaging
Imports System.IO
Imports System.Reflection
Imports System.Windows.Media.Imaging

Namespace FilesystemManagement

    ''' <summary>
    ''' Performs operations on image files and or internal image objects.
    ''' </summary>
    Public NotInheritable Class ImageProcessor

#Region "Declarations"

        Private Const INTERNAL_IMAGE_EXT As String = ".png"

#End Region

#Region "Main"

        ''' <summary>
        ''' Resize the image to the specified width and height.
        ''' </summary>
        ''' <param name="image">The image to resize.</param>
        ''' <param name="width">The width to resize to.</param>
        ''' <param name="height">The height to resize to.</param>
        ''' <returns>The resized image.</returns>
        Shared Function GetResizedImageAsBitmap(image As Image, width As Integer, height As Integer) As Bitmap
            Dim destRect = New Rectangle(0, 0, width, height)
            Dim destImage = New Bitmap(width, height)
            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution)
            Using g = Graphics.FromImage(destImage)
                g.CompositingMode = CompositingMode.SourceCopy
                g.CompositingQuality = CompositingQuality.HighQuality
                g.InterpolationMode = InterpolationMode.HighQualityBicubic
                g.SmoothingMode = SmoothingMode.HighQuality
                g.PixelOffsetMode = PixelOffsetMode.HighQuality
                Using imgAttr = New ImageAttributes()
                    imgAttr.SetWrapMode(WrapMode.TileFlipXY)
                    g.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, imgAttr)
                End Using
            End Using
            Return destImage
        End Function

        ''' <summary>
        ''' Generates a BitmapImage from a raw image byte array.
        ''' </summary>
        ''' <param name="bArr">Image byte array.</param>
        ''' <returns>The resulting BitmapImage object.</returns>
        Shared Function GetBitmapImageFromBytes(bArr() As Byte) As BitmapImage
            Dim bImg As New BitmapImage()
            Using mStr As New MemoryStream(bArr)
                bImg.BeginInit()
                bImg.StreamSource = mStr
                bImg.CacheOption = BitmapCacheOption.OnLoad
                bImg.EndInit()
                bImg.Freeze()
                mStr.Close()
            End Using
            Return bImg
        End Function

        ''' <summary>
        ''' Gets an internal image's byte array.
        ''' </summary>
        ''' <param name="defImage">The default image to return.</param>
        ''' <returns>Returns the image's byte array.</returns>
        Shared Function GetDefaultImageAsByteArr(defImage As DefaultImage) As Byte()
            Dim emptyImageByteArr() As Byte
            Using rs = Assembly.GetExecutingAssembly.GetManifestResourceStream($"{Assembly.GetExecutingAssembly.GetName.Name}.{defImage.ToString}{INTERNAL_IMAGE_EXT}")
                Using ms As New MemoryStream
                    rs.CopyTo(ms)
                    emptyImageByteArr = ms.ToArray
                    ms.Close()
                End Using
                rs.Close()
            End Using
            Return emptyImageByteArr
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
    ''' Determines the requested default image.
    ''' </summary>
    Public Enum DefaultImage
        NotFound = 0
    End Enum

End Namespace
