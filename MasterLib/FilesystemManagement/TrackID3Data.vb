Namespace FilesystemManagement

    ''' <summary>
    ''' Provides a structure for a media file's metadata.
    ''' </summary>
    Public Class TrackID3Data

        ''' <summary>
        ''' Requires specific media file metadata.
        ''' </summary>
        ''' <param name="trackName">Track artist.</param>
        ''' <param name="trackBPM">Track beats per minute.</param>
        ''' <param name="trackKey">Track key.</param>
        ''' <param name="trackGain">Track gain.</param>
        ''' <param name="trackGenre">Track genre.</param>
        ''' <param name="trackCreated">Track creation date.</param>
        Public Sub New(trackName As String, trackBPM As Integer, trackKey As String, trackGain As Double, trackGenre As String, trackCreated As String)
            Name = trackName
            BeatsPerMinute = trackBPM
            Key = trackKey
            Gain = trackGain
            Genre = trackGenre
            Created = trackCreated
        End Sub

        ReadOnly Property Name As String
        ReadOnly Property BeatsPerMinute As Integer
        ReadOnly Property Key As String
        ReadOnly Property Gain As Double
        ReadOnly Property Genre As String
        ReadOnly Property Created As String

    End Class

End Namespace
