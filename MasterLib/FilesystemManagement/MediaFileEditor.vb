Imports System.Drawing
Imports System.Drawing.Imaging
Imports System.Globalization
Imports System.IO
Imports System.Text
Imports System.Text.RegularExpressions

Namespace FilesystemManagement

    ''' <summary>
    ''' Analyzes or modifies media files' ID3 tags or their file names.
    ''' </summary>
    Public NotInheritable Class MediaFileEditor

#Region "Declarations"

        Private Const EXT_MP3 As String = ".MP3"
        Private Const EXT_FLAC As String = ".FLAC"
        Private Const EXT_M4A As String = ".M4A"
        Private Const EXT_COVER As String = ".JPG"

        ''' <summary>
        ''' Provides status messages that indicate the currently running/last completed actions.
        ''' </summary>
        Private NotInheritable Class StatusMessage

            Private Sub New()
            End Sub

            Public Const InfoTracksMissingTagsStart As String = "Getting tracks with missing ID3 tags..."
            Public Const InfoTracksUnicodeTagsStart As String = "Getting tracks containing unicode ID3 tags..."
            Public Const InfoTracksDifferentNamesToTagsStart As String = "Getting tracks with differentiating names to ID3 tags..."
            Public Const InfoTracksAnalysisComplete As String = "Tracks analyzed successfully."
            Public Const InfoClearExtraTagsStart As String = "Clearing unnecessary ID3 tags..."
            Public Const InfoClearExtraTagsComplete As String = "Extra ID3 tags cleared successfully."
            Public Const InfoGenerateTracksTagsStart As String = "Generating tracks ID3 tags from their filenames..."
            Public Const InfoGenerateTracksTagsComplete As String = "ID3 tag generation completed successfully."
            Public Const InfoFormatTracksTagsStart As String = "Formatting tracks artists and title ID3 tags..."
            Public Const InfoFormatTracksTagsComplete As String = "Formatting ID3 tags completed successfully."
            Public Const InfoID3CommentStart As String = "ID3 comment change started..."
            Public Const InfoID3CommentComplete As String = "ID3 comments changed successfully."
            Public Const InfoChangeCoverStart As String = "Changing ID3 cover tags..."
            Public Const InfoChangeCoverComplete As String = "Tracks ID3 cover changed successfully."
            Public Const InfoFileRenameStart As String = "Renaming track filenames started..."
            Public Const InfoFileRenameComplete As String = "Tracks renamed successfully."
            Public Const ErrorInputDirectory As String = "Input directory doesn't exist..."
            Public Const ErrorInputFile As String = "Input file doesn't exist..."
            Public Const ErrorInputFileFormat As String = "Input file is not a supported format..."
            Public Const ErrorEmptyInputKey As String = "No input key has been provided..."
            Public Const ErrorInvalidInputKey As String = "The provided key is unknown..."
            Public Const ErrorInvalidBPM As String = "Invalid BPM value provided..."
            Public Const ErrorCoverNotFound As String = "Input cover file doesn't exist..."
            Public Const ErrorCoverFormat As String = "Input cover is not a JPG file..."

        End Class

        Shared CamelotTranslatedOuter() As String = {"01B", "02B", "03B", "04B", "05B", "06B", "07B", "08B", "09B", "10B", "11B", "12B"}
        Shared CamelotTranslatedInner() As String = {"01A", "02A", "03A", "04A", "05A", "06A", "07A", "08A", "09A", "10A", "11A", "12A"}

        Shared Event OnStatusChanged(message As String, isError As Boolean)

#End Region

#Region "Main"

        ''' <summary>
        ''' Reads the input file's ID3 metadata and stores it in a TrackID3Data object.
        ''' </summary>
        ''' <param name="fName">The input media file.</param>
        ''' <returns>Returns the TrackID3Data object or null in case of error.</returns>
        Shared Function GetTrackID3Data(fName As String) As TrackID3Data
            Select Case True
                Case Not File.Exists(fName)
                    RaiseEvent OnStatusChanged(StatusMessage.ErrorInputFile, True)
                    Return Nothing
                Case Not {EXT_MP3, EXT_FLAC, EXT_M4A}.Contains(Path.GetExtension(fName).ToUpper)
                    RaiseEvent OnStatusChanged(StatusMessage.ErrorInputFileFormat, True)
                    Return Nothing
            End Select

            If fName.Contains(".flac") Then
                Using tagLibFile As TagLib.File = TagLib.File.Create(fName)
                    'tagLibFile.Tag.BeatsPerMinute = 0
                    ' tagLibFile.Save()
                End Using
            End If

            Using tagLibFile As TagLib.File = TagLib.File.Create(fName)
                Dim artistValue As String = IIf(String.IsNullOrWhiteSpace(tagLibFile.Tag?.JoinedPerformers), "Unknown Artist", tagLibFile.Tag?.JoinedPerformers)
                Dim titleValue As String = IIf(String.IsNullOrWhiteSpace(tagLibFile.Tag?.Title), "Unknown Title", tagLibFile.Tag?.Title)
                Dim genreValue As String = IIf(String.IsNullOrWhiteSpace(tagLibFile.Tag?.JoinedGenres), "Unknown", tagLibFile.Tag?.JoinedGenres)
                Dim createdValue As String = IIf(String.IsNullOrWhiteSpace(tagLibFile.Tag?.Comment), "Unknown", tagLibFile.Tag?.Comment)
                Dim kgAlbumValue As String = tagLibFile.Tag?.Album
                Dim keyValue As String = "N/A"
                Dim gainValue As Double = 0.0
                If Not String.IsNullOrWhiteSpace(kgAlbumValue) AndAlso kgAlbumValue.Count(Function(ch) ch = "|") = 1 Then
                    keyValue = kgAlbumValue.Split("|")(0).Trim
                    gainValue = CDbl(kgAlbumValue.Split("|")(1).Replace("±", "").Trim)
                End If
                Return New TrackID3Data(artistValue & " - " & titleValue, tagLibFile.Tag?.BeatsPerMinute, keyValue, gainValue, genreValue, createdValue)
            End Using
            Return Nothing
        End Function

        ''' <summary>
        ''' Gets an array of compatible tone keys to a key/BPM pair. The new keys are calculated based on a specified BPM value.
        ''' </summary>
        ''' <param name="orgKey">The original tone key value.</param>
        ''' <param name="orgBPM">The original BPM value.</param>
        ''' <param name="newBPM">The new BPM value.</param>
        ''' <param name="newKeyOnly">Return only the new key based on the original one.</param>
        ''' <returns>Returns an array of all compatible keys or null in case of error.</returns>
        Shared Function GetTrackAdjacentKeysAtBMP(orgKey As String, orgBPM As Integer, newBPM As Integer, newKeyOnly As Boolean) As String()

            Select Case True
                Case String.IsNullOrWhiteSpace(orgKey)
                    RaiseEvent OnStatusChanged(StatusMessage.ErrorEmptyInputKey, True)
                    Return Nothing
                Case Not CamelotTranslatedOuter.Contains(orgKey) AndAlso Not CamelotTranslatedInner.Contains(orgKey)
                    RaiseEvent OnStatusChanged(StatusMessage.ErrorInvalidInputKey, True)
                    Return Nothing
                Case orgBPM <= 0 OrElse newBPM <= 0
                    RaiseEvent OnStatusChanged(StatusMessage.ErrorInvalidBPM, True)
                    Return Nothing
            End Select

            Dim keyIsOuter As Boolean = CamelotTranslatedOuter.Contains(orgKey)
            Dim keyIndex As Integer

            If keyIsOuter Then
                keyIndex = Array.IndexOf(CamelotTranslatedOuter, orgKey) ' Look for the key in the outer wheel.
            Else
                keyIndex = Array.IndexOf(CamelotTranslatedInner, orgKey) ' Look for the key in the inner wheel.
            End If

            Dim percentTempoChange As Double = Math.Abs(100 - ((100 / orgBPM) * newBPM))
            Dim timesSixPercentChange As Integer = Math.Truncate((3 + percentTempoChange) / 6)
            Dim stepsInWheelChange As Integer = (7 * timesSixPercentChange) Mod 12 ' Effective steps - with no full rotations.

            If orgBPM < newBPM Then
                keyIndex = (keyIndex + stepsInWheelChange) Mod 12 ' Tempo is increasing. Moving key index right-wise.
            ElseIf orgBPM > newBPM Then
                keyIndex = (12 + keyIndex - stepsInWheelChange) Mod 12  ' Tempo is decreasing. Moving key index left-wise.
            Else
                ' Tempo stays the same. The key index stays the same.
            End If

            If keyIsOuter Then
                If newKeyOnly Then Return {CamelotTranslatedOuter(keyIndex)}
                Return {CamelotTranslatedOuter((12 + (keyIndex - 1)) Mod 12), CamelotTranslatedOuter(keyIndex), CamelotTranslatedOuter((keyIndex + 1) Mod 12), CamelotTranslatedInner(keyIndex)}
            Else
                If newKeyOnly Then Return {CamelotTranslatedInner(keyIndex)}
                Return {CamelotTranslatedInner((12 + (keyIndex - 1)) Mod 12), CamelotTranslatedInner(keyIndex), CamelotTranslatedInner((keyIndex + 1) Mod 12), CamelotTranslatedOuter(keyIndex)}
            End If

        End Function

        ''' <summary>
        ''' Analyzes media files' BPM, title, artist, album, genre and comment ID3 tags in the specified input directory for missing data.
        ''' </summary>
        ''' <param name="inputDir">Input directory.</param>
        ''' <returns>Returns all files missing any of their ID3 tags as a string.</returns>
        Shared Function GetTracksWithMissingTags(inputDir As String) As String

            Dim tagInfoSb As New StringBuilder
            Select Case True
                Case Not Directory.Exists(inputDir)
                    RaiseEvent OnStatusChanged(StatusMessage.ErrorInputDirectory, True)
                    Return tagInfoSb.ToString
            End Select

            RaiseEvent OnStatusChanged(StatusMessage.InfoTracksMissingTagsStart, False)
            For Each fName In Directory.GetFiles(inputDir)
                Try
                    If Not {EXT_MP3, EXT_FLAC, EXT_M4A}.Contains(Path.GetExtension(fName).ToUpper) Then
                        RaiseEvent OnStatusChanged($"Skipped: '{fName}'", False)
                        Continue For
                    End If

                    Using tagLibFile As TagLib.File = TagLib.File.Create(fName)
                        If (tagLibFile.Tag?.BeatsPerMinute = 0 AndAlso
                            Not tagLibFile.Tag?.TagTypes = (TagLib.TagTypes.FlacMetadata Or TagLib.TagTypes.Xiph)) OrElse
                            {tagLibFile.Tag?.BeatsPerMinute,
                            tagLibFile.Tag?.Title,
                            tagLibFile.Tag?.JoinedPerformers,
                            tagLibFile.Tag?.Album,
                            tagLibFile.Tag?.JoinedGenres,
                            tagLibFile.Tag?.Comment}.Any(Function(tagField) String.IsNullOrWhiteSpace(tagField)) Then
                            tagInfoSb.Append($"{fName}{vbCrLf}")
                        End If
                    End Using

                    RaiseEvent OnStatusChanged($"Checked: '{fName}'", False)
                Catch ex As Exception
                    RaiseEvent OnStatusChanged($"Error: '{ex.Message}'", True)
                End Try
            Next

            RaiseEvent OnStatusChanged(StatusMessage.InfoTracksAnalysisComplete, False)
            Return tagInfoSb.ToString
        End Function

        ''' <summary>
        ''' Analyzes media files' title, artist, album, genre and comment ID3 tags in the specified input directory for unicode symbols.
        ''' </summary>
        ''' <param name="inputDir">Input directory.</param>
        ''' <returns>Returns all files having unicodes in their ID3 tags as a string.</returns>
        Shared Function GetTracksWithUnicodeTags(inputDir As String) As String

            Dim tagInfoSb As New StringBuilder
            Select Case True
                Case Not Directory.Exists(inputDir)
                    RaiseEvent OnStatusChanged(StatusMessage.ErrorInputDirectory, True)
                    Return tagInfoSb.ToString
            End Select

            RaiseEvent OnStatusChanged(StatusMessage.InfoTracksUnicodeTagsStart, False)
            For Each fName In Directory.GetFiles(inputDir)
                Try
                    If Not {EXT_MP3, EXT_FLAC, EXT_M4A}.Contains(Path.GetExtension(fName).ToUpper) Then
                        RaiseEvent OnStatusChanged($"Skipped: '{fName}'", False)
                        Continue For
                    End If

                    Using tagLibFile As TagLib.File = TagLib.File.Create(fName)
                        If {tagLibFile.Tag?.Title,
                            tagLibFile.Tag?.JoinedPerformers,
                            tagLibFile.Tag?.Album,
                            tagLibFile.Tag?.JoinedGenres,
                            tagLibFile.Tag?.Comment}.Any(Function(tagField) tagField?.Any(Function(c) AscW(c) > 255)) Then
                            tagInfoSb.Append($"{fName}{vbCrLf}")
                        End If
                    End Using

                    RaiseEvent OnStatusChanged($"Checked: '{fName}'", False)
                Catch ex As Exception
                    RaiseEvent OnStatusChanged($"Error: '{ex.Message}'", True)
                End Try
            Next
            RaiseEvent OnStatusChanged(StatusMessage.InfoTracksAnalysisComplete, False)
            Return tagInfoSb.ToString
        End Function

        ''' <summary>
        ''' Compares media files' title and artist ID3 tags to the filename in the specified input directory and returns all files that don't match.
        ''' </summary>
        ''' <returns></returns>
        Shared Function GetTracksWithDifferentFilenameToTags(inputDir As String) As String
            Dim tagInfoSb As New StringBuilder
            Select Case True
                Case Not Directory.Exists(inputDir)
                    RaiseEvent OnStatusChanged(StatusMessage.ErrorInputDirectory, True)
                    Return tagInfoSb.ToString
            End Select

            RaiseEvent OnStatusChanged(StatusMessage.InfoTracksDifferentNamesToTagsStart, False)
            For Each fName In Directory.GetFiles(inputDir)
                Try
                    If Not {EXT_MP3, EXT_FLAC, EXT_M4A}.Contains(Path.GetExtension(fName).ToUpper) Then
                        RaiseEvent OnStatusChanged($"Skipped: '{fName}'", False)
                        Continue For
                    End If

                    Using tagLibFile As TagLib.File = TagLib.File.Create(fName)
                        If Not Path.GetFileNameWithoutExtension(fName).ToUpper = $"{tagLibFile.Tag?.JoinedPerformers}-{tagLibFile.Tag?.Title}".
                            Replace(".", "").
                            Replace("?", "").
                            Replace("/", "").
                            Replace("*", "").
                            Replace("-", " - ").ToUpper Then
                            tagInfoSb.Append($"{fName}{vbCrLf}")
                        End If
                    End Using

                    RaiseEvent OnStatusChanged($"Checked: '{fName}'", False)
                Catch ex As Exception
                    RaiseEvent OnStatusChanged($"Error: '{ex.Message}'", True)
                End Try
            Next
            RaiseEvent OnStatusChanged(StatusMessage.InfoTracksAnalysisComplete, False)
            Return tagInfoSb.ToString
        End Function

        ''' <summary>
        ''' Clears all media files' extra ID3 tags in the specified input directory. Those include: track number, disc number, album artist, year, composer, lyrics, copyright, cover.
        ''' </summary>
        ''' <param name="inputDir">Input directory.</param>
        Shared Sub ClearTracksExtraTags(inputDir As String)

            Select Case True
                Case Not Directory.Exists(inputDir)
                    RaiseEvent OnStatusChanged(StatusMessage.ErrorInputDirectory, True)
                    Exit Sub
            End Select

            RaiseEvent OnStatusChanged(StatusMessage.InfoClearExtraTagsStart, False)
            For Each fName In Directory.GetFiles(inputDir)
                Try
                    If Not {EXT_MP3, EXT_FLAC, EXT_M4A}.Contains(Path.GetExtension(fName).ToUpper) Then
                        RaiseEvent OnStatusChanged($"Skipped: '{fName}'", False)
                        Continue For
                    End If

                    Using tagLibFile As TagLib.File = TagLib.File.Create(fName)
                        tagLibFile.Tag.Track = Nothing
                        tagLibFile.Tag.Disc = Nothing
                        tagLibFile.Tag.AlbumArtists = Nothing
                        tagLibFile.Tag.Year = Nothing
                        tagLibFile.Tag.Composers = Nothing
                        tagLibFile.Tag.Lyrics = Nothing
                        tagLibFile.Tag.Copyright = Nothing
                        tagLibFile.Tag.Pictures = Nothing
                        tagLibFile.Save()  ' Publisher is handled with TrackNMLEditor
                    End Using

                    RaiseEvent OnStatusChanged($"Checked: '{fName}'", False)
                Catch ex As Exception
                    RaiseEvent OnStatusChanged($"Error: '{ex.Message}'", True)
                End Try
            Next
            RaiseEvent OnStatusChanged(StatusMessage.InfoClearExtraTagsComplete, False)
        End Sub

        ''' <summary>
        ''' Generates media files' artist and title ID3 tags in the specified input directory from their filename, if applicable.
        ''' </summary>
        ''' <param name="inputDir"></param>
        Shared Sub SetTracksArtistAndTitleTagsFromFilename(inputDir As String)

            Select Case True
                Case Not Directory.Exists(inputDir)
                    RaiseEvent OnStatusChanged(StatusMessage.ErrorInputDirectory, True)
                    Exit Sub
            End Select

            RaiseEvent OnStatusChanged(StatusMessage.InfoGenerateTracksTagsStart, False)
            For Each fName In Directory.GetFiles(inputDir)
                Try
                    If Not {EXT_MP3, EXT_FLAC, EXT_M4A}.Contains(Path.GetExtension(fName).ToUpper) Then
                        RaiseEvent OnStatusChanged($"Skipped: '{fName}'", False)
                        Continue For
                    End If

                    Dim fNameNoExt As String = Path.GetFileNameWithoutExtension(fName)
                    If fNameNoExt.Trim.StartsWith("-") OrElse fNameNoExt.Trim.EndsWith("-") OrElse Not fNameNoExt.Count(
                        Function(dash As Char)
                            Return dash = "-"c
                        End Function) = 1 Then
                        RaiseEvent OnStatusChanged($"Skipped: '{fName}'", False)
                        Continue For
                    End If

                    Using tagLibFile As TagLib.File = TagLib.File.Create(fName)
                        If String.IsNullOrWhiteSpace(tagLibFile.Tag.JoinedPerformers?.Trim) Then
                            tagLibFile.Tag.Performers = {fNameNoExt.Split("-")(0).Trim} ' Set the artist.
                        Else
                            tagLibFile.Tag.Performers = {tagLibFile.Tag.JoinedPerformers.Trim}
                        End If

                        If String.IsNullOrWhiteSpace(tagLibFile.Tag.Title?.Trim) Then
                            tagLibFile.Tag.Title = fNameNoExt.Split("-")(1).Trim ' Set the title.
                        Else
                            tagLibFile.Tag.Title = tagLibFile.Tag.Title.Trim
                        End If
                        tagLibFile.Save()
                    End Using

                    RaiseEvent OnStatusChanged($"Checked: '{fName}'", False)
                Catch ex As Exception
                    RaiseEvent OnStatusChanged($"Error: '{ex.Message}'", True)
                End Try
            Next
            RaiseEvent OnStatusChanged(StatusMessage.InfoGenerateTracksTagsComplete, False)
        End Sub

        ''' <summary>
        ''' Modifies media files' artist and title ID3 tags in the specified input directory so that they all conform to a specific set of rules.
        ''' </summary>
        ''' <param name="inputDir">Input directory.</param>
        Shared Sub FormatTracksArtistAndTitleTags(inputDir As String)

            Select Case True
                Case Not Directory.Exists(inputDir)
                    RaiseEvent OnStatusChanged(StatusMessage.ErrorInputDirectory, True)
                    Exit Sub
            End Select

            RaiseEvent OnStatusChanged(StatusMessage.InfoFormatTracksTagsStart, False)
            For Each fName In Directory.GetFiles(inputDir)
                Try
                    If Not {EXT_MP3, EXT_FLAC, EXT_M4A}.Contains(Path.GetExtension(fName).ToUpper) Then
                        RaiseEvent OnStatusChanged($"Skipped: '{fName}'", False)
                        Continue For
                    End If

                    Using tagLibFile As TagLib.File = TagLib.File.Create(fName)
                        ' Track title replacements.
                        If Not String.IsNullOrWhiteSpace(tagLibFile.Tag?.Title) Then
                            tagLibFile.Tag.Title = GenerateCleanTitleTag(tagLibFile.Tag.Title)
                        End If

                        ' Track artist replacements.
                        If Not String.IsNullOrWhiteSpace(tagLibFile.Tag?.JoinedPerformers) Then
                            tagLibFile.Tag.Performers = {GenerateCleanArtistTag(tagLibFile.Tag.JoinedPerformers)}
                        End If
                        tagLibFile.Save()
                    End Using

                    RaiseEvent OnStatusChanged($"Checked: '{fName}'", False)
                Catch ex As Exception
                    RaiseEvent OnStatusChanged($"Error: '{ex.Message}'", True)
                End Try
            Next
            RaiseEvent OnStatusChanged(StatusMessage.InfoFormatTracksTagsComplete, False)
        End Sub

        ''' <summary>
        ''' Modifies media files' comment ID3 tag in the specified input directory to each individual file's creation date.
        ''' </summary>
        ''' <param name="inputDir"></param>
        Shared Sub SetTracksCommentTags(inputDir As String)

            Select Case True
                Case Not Directory.Exists(inputDir)
                    RaiseEvent OnStatusChanged(StatusMessage.ErrorInputDirectory, True)
                    Exit Sub
            End Select

            RaiseEvent OnStatusChanged(StatusMessage.InfoID3CommentStart, False)
            For Each fName In Directory.GetFiles(inputDir)
                Try
                    If Not {EXT_MP3, EXT_FLAC, EXT_M4A}.Contains(Path.GetExtension(fName).ToUpper) Then
                        RaiseEvent OnStatusChanged($"Skipped: '{fName}'", False)
                        Continue For
                    End If

                    Using tagLibFile As TagLib.File = TagLib.File.Create(fName)
                        Dim dt As Date = File.GetCreationTime(fName)
                        tagLibFile.Tag.Comment = $"{dt.Year}-{dt.Month.ToString("00")}-{dt.Day.ToString("00")}"
                        tagLibFile.Save()
                    End Using

                    RaiseEvent OnStatusChanged($"Checked: '{fName}'", False)
                Catch ex As Exception
                    RaiseEvent OnStatusChanged($"Error: '{ex.Message}'", True)
                End Try
            Next
            RaiseEvent OnStatusChanged(StatusMessage.InfoID3CommentComplete, False)
        End Sub

        ''' <summary>
        ''' Adds a cover image in the ID3 data of the media files in the specified input directory.
        ''' </summary>
        ''' <param name="inputDir">File input directory.</param>
        ''' <param name="coverFile">Cover image for the files in the specified directory.</param>
        ''' <param name="namePattern">Modify files that only match the filename pattern.</param>
        Shared Sub SetTracksCoverTags(inputDir As String, coverFile As String, Optional namePattern As String = "*")

            Select Case True
                Case Not Directory.Exists(inputDir)
                    RaiseEvent OnStatusChanged(StatusMessage.ErrorInputDirectory, True)
                    Exit Sub
                Case Not File.Exists(coverFile)
                    RaiseEvent OnStatusChanged(StatusMessage.ErrorCoverNotFound, True)
                    Exit Sub
                Case Not Path.GetExtension(coverFile).ToUpper = EXT_COVER
                    RaiseEvent OnStatusChanged(StatusMessage.ErrorCoverFormat, True)
                    Exit Sub
            End Select

            RaiseEvent OnStatusChanged(StatusMessage.InfoChangeCoverStart, False)
            Using ms As New MemoryStream
                Dim coverImg As Image = Image.FromFile(coverFile)
                Dim normFactor As Integer = 0
                If coverImg.Height > coverImg.Width AndAlso coverImg.Height > 300 Then
                    normFactor = coverImg.Height - 300  ' The height is greater than the width and is greater than 300.
                ElseIf coverImg.Width > coverImg.Height AndAlso coverImg.Width > 300 Then
                    normFactor = coverImg.Width - 300 ' The width is greater than the height and is greater than 300.
                ElseIf coverImg.Height > 300 Then
                    normFactor = coverImg.Height - 300 ' The height and width are equal.
                End If

                ImageProcessor.GetResizedImageAsBitmap(coverImg, coverImg.Width - normFactor, coverImg.Height - normFactor).Save(ms, ImageFormat.Jpeg) ' Image normalization.

                For Each fName In Directory.GetFiles(inputDir)
                    Try
                        If Not {EXT_MP3, EXT_FLAC, EXT_M4A}.Contains(Path.GetExtension(fName).ToUpper) OrElse
                            Not Path.GetFileName(fName).ToUpper Like namePattern.ToUpper Then
                            RaiseEvent OnStatusChanged($"Skipped: '{fName}'", False)
                            Continue For
                        End If

                        Using tagLibFile As TagLib.File = TagLib.File.Create(fName)
                            Dim tagLibCover As TagLib.Picture = New TagLib.Picture
                            tagLibCover.Type = TagLib.PictureType.FrontCover
                            tagLibCover.MimeType = Net.Mime.MediaTypeNames.Image.Jpeg
                            tagLibCover.Description = "cover"
                            ms.Position = 0
                            tagLibCover.Data = TagLib.ByteVector.FromStream(ms)
                            tagLibFile.Tag.Pictures = {tagLibCover}
                            tagLibFile.Save()
                        End Using

                        RaiseEvent OnStatusChanged($"Added cover to: '{fName}'", False)
                    Catch ex As Exception
                        RaiseEvent OnStatusChanged($"Error: '{ex.Message}'", True)
                    End Try
                Next
                ms.Close()
            End Using
            RaiseEvent OnStatusChanged(StatusMessage.InfoChangeCoverComplete, False)
        End Sub

        ''' <summary>
        ''' Changes media file names by a predefined set of rules.
        ''' </summary>
        ''' <param name="inputDir">File input directory.</param>
        Shared Sub SetTracksFilename(inputDir As String)

            Select Case True
                Case Not Directory.Exists(inputDir)
                    RaiseEvent OnStatusChanged(StatusMessage.ErrorInputDirectory, True)
                    Exit Sub
            End Select

            RaiseEvent OnStatusChanged(StatusMessage.InfoFileRenameStart, False)
            Parallel.ForEach(
                Directory.GetFiles(inputDir),
                Sub(fileStr As String)
                    Try

                        If Not {EXT_MP3, EXT_FLAC, EXT_M4A}.Contains(Path.GetExtension(fileStr).ToUpper) Then
                            RaiseEvent OnStatusChanged($"Skipped: '{fileStr}'", False)
                            Exit Sub
                        End If

                        Dim fileDir As String = Path.GetDirectoryName(fileStr)
                        Dim newFileExt As String = Path.GetExtension(fileStr).ToLower()
                        Dim newFileStr As String = GenerateCleanFilename(Path.GetFileNameWithoutExtension(fileStr).ToLower())

                        If Not String.IsNullOrWhiteSpace(newFileStr) Then
                            If Not File.Exists($"{fileDir}\{newFileStr}{newFileExt}") Then
                                ' In case the new filename doesn't exist.
                                File.Move(fileStr, $"{fileDir}\{newFileStr}{newFileExt}")
                            ElseIf Not String.Equals(Path.GetFileName(fileStr), $"{newFileStr}{newFileExt}", StringComparison.Ordinal) Then
                                ' In case there are only case changes to the filename.
                                File.Move(fileStr, $"{fileDir}\{newFileStr}{newFileExt}_unique")
                                File.Move($"{fileDir}\{newFileStr}{newFileExt}_unique", $"{fileDir}\{newFileStr}{newFileExt}")
                            End If
                        Else
                            RaiseEvent OnStatusChanged($"Name generates an empty string: '{fileStr}'", False)
                        End If
                    Catch ex As Exception
                        RaiseEvent OnStatusChanged($"Error: '{ex.Message}'", True)
                    End Try
                End Sub)
            RaiseEvent OnStatusChanged(StatusMessage.InfoFileRenameComplete, False)
        End Sub

#End Region

#Region "Internal"

        ''' <summary>
        ''' Cannot be initialized directly.
        ''' </summary>
        Private Sub New()
        End Sub

        ''' <summary>
        ''' Replacing track file names, using a specific set of formatting rules.
        ''' </summary>
        ''' <param name="fName">Filename string to replace.</param>
        ''' <returns>The formatted string.</returns>
        Private Shared Function GenerateCleanFilename(fName As String) As String
            'A
            fName = Regex.Replace(fName, "_", " ") ' Underscore replacement.
            fName = Regex.Replace(fName, "\[", "(") ' Left bracket replacement.
            fName = Regex.Replace(fName, "\]", ")") ' Right bracket replacement.
            fName = Regex.Replace(fName, "\s+(?i)(ft|feat|featuring)\.*\s+", " ft ") ' Different ft. replacements.
            fName = Regex.Replace(fName, "\s+(?i)(and|\&amp\;*)\s+", " & ") ' Different & replacements.
            fName = Regex.Replace(fName, "\s+(?i)(vs\.*|versus)\s+", " vs ") ' Different vs. replacements.
            fName = Regex.Replace(fName, "(?i)(rmx)\.*", "remix") ' Remix replacements.
            'C
            fName = Regex.Replace(fName, "^(?i)[ab][12]\.*\s*", "") ' Disk sides replacements.
            fName = Regex.Replace(fName, "^(?i)(va|a|b)\s+\-*\s*", "") ' Remove starting VA, A or B for leftover disk sides.
            'D
            fName = Regex.Replace(fName, "^[^a-z]*", "") ' Remove all starting characters that aren't in the a-z range.
            fName = Regex.Replace(fName, "\S*[.]\S*", "") ' Removes words containing dots.
            fName = Regex.Replace(fName, "\-", " - ") ' Dash replacement.
            'E
            fName = Regex.Replace(fName, "\s+", " ") ' Multiple intervals replacements.
            fName = Regex.Replace(fName, "\s{1}\,\s{1}", ", ") ' Incorrect comma separation replacement.
            'F
            fName = Regex.Replace(fName, "(?i)\-?\s?\(?(original)\s{1}(mix)?\)?", "") ' Remove '(Original Mix)' with or without brackets.
            fName = Regex.Replace(fName, "(?i)\({1}(master)\){1}", "") ' Remove '(Master)' with brackets only.
            'G
            fName = Regex.Replace(fName, "\s{1}\-\s{1}(?i)(bnp|bc|chc|doc|dwm|free\s{1}\-|free|gti|homely|htid|nrg|osm|pity|q91|sds|sob|sq|uf|ukhx|ukh|web|xds|xtc|zzzz)$", "") ' Name-specific replacements.
            fName = New CultureInfo("en-US", False).TextInfo.ToTitleCase(fName) ' Capitalize only the first letter of each word.

            Return fName.Trim
        End Function

        ''' <summary>
        ''' Replacing track artists, using a specific set of formatting rules.
        ''' </summary>
        ''' <param name="artist">Artist string to replace.</param>
        ''' <returns>The formatted string.</returns>
        Private Shared Function GenerateCleanArtistTag(artist As String) As String
            'A
            artist = Regex.Replace(artist, "_", " ") ' Underscore replacement.
            artist = Regex.Replace(artist, "\[", "(") ' Left bracket replacement.
            artist = Regex.Replace(artist, "\]", ")") ' Right bracket replacement.
            artist = Regex.Replace(artist, "\s+(?i)(ft|feat|featuring)\.*\s+", " ft ") ' Different ft. replacements.
            artist = Regex.Replace(artist, "\s+(?i)(and|\&amp\;*)\s+", " & ") ' Different & replacements.
            artist = Regex.Replace(artist, "\s+(?i)(vs\.*|versus)\s+", " vs ") ' Different vs. replacements.
            artist = Regex.Replace(artist, "(?i)(rmx|remix)\.*", "Remix") ' Remix replacements.
            'B
            artist = Regex.Replace(artist, "(?i)mc\s{1}", "MC ") ' MC replacements.
            artist = Regex.Replace(artist, "(?i)dj\s{1}", "DJ ") ' DJ replacements.
            'C
            artist = Regex.Replace(artist, "^(?i)[ab][12]\.*\s*", "") ' Disk sides replacements.
            artist = Regex.Replace(artist, "^(?i)(va|a|b)\s+\-*\s*", "") ' Remove starting VA, A or B for leftover disk sides.
            'E
            artist = Regex.Replace(artist, "\s+", " ") ' Multiple intervals replacements.
            artist = Regex.Replace(artist, "\s{1}\,\s{1}", ", ") ' Incorrect comma separation replacement.

            Return artist.Trim
        End Function

        ''' <summary>
        ''' Replacing track titles, using a specific set of formatting rules.
        ''' </summary>
        ''' <param name="title">Title string to replace.</param>
        ''' <returns>The formatted string.</returns>
        Private Shared Function GenerateCleanTitleTag(title As String) As String
            'A
            title = Regex.Replace(title, "_", " ") ' Underscore replacement.
            title = Regex.Replace(title, "\[", "(") ' Left bracket replacement.
            title = Regex.Replace(title, "\]", ")") ' Right bracket replacement.
            title = Regex.Replace(title, "\s+(?i)(ft|feat|featuring)\.*\s+", " ft ") ' Different ft. replacements.
            title = Regex.Replace(title, "\s+(?i)(and|\&amp\;*)\s+", " & ") ' Different & replacements.
            title = Regex.Replace(title, "\s+(?i)(vs\.*|versus)\s+", " vs ") ' Different vs. replacements.
            title = Regex.Replace(title, "(?i)(rmx|remix)\.*", "Remix") ' Remix replacements.
            'B
            title = Regex.Replace(title, "(?i)mc\s{1}", "MC ") ' MC replacements.
            title = Regex.Replace(title, "(?i)dj\s{1}", "DJ ") ' DJ replacements.
            'E
            title = Regex.Replace(title, "\s+", " ") ' Multiple intervals replacements.
            title = Regex.Replace(title, "\s{1}\,\s{1}", ", ") ' Incorrect comma separation replacement.
            'F
            title = Regex.Replace(title, "(?i)\-?\s?\(?(original)\s{1}(mix)?\)?", "") ' Remove '(Original Mix)' with or without brackets.
            title = Regex.Replace(title, "(?i)\({1}(master)\){1}", "") ' Remove '(Master)' with brackets only.

            Return title.Trim
        End Function

#End Region

    End Class

End Namespace
