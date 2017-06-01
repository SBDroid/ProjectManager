Imports System.IO

Namespace StorageMonitoring

    ''' <summary>
    ''' Provides a structure for the monitored storage devices.
    ''' </summary>
    Public Class StorageDevice

        ''' <summary>
        ''' Used to compare separate files by their size (in bytes) and their last write time.
        ''' </summary>
        Private Class FileIdentifier

            Public ReadOnly Length As Long
            Public ReadOnly LastWriteTime As Date

            ''' <summary>
            ''' Requires the file's byte length as well as its last write time.
            ''' </summary>
            ''' <param name="len">File's byte length.</param>
            ''' <param name="lwt">File's last modified time.</param>
            Public Sub New(len As Long, lwt As Date)
                Length = len
                LastWriteTime = lwt
            End Sub

            Public Overrides Function Equals(obj As Object) As Boolean
                If obj Is Nothing OrElse Not obj.GetType = GetType(FileIdentifier) Then
                    Return False
                Else
                    Dim fidObj As FileIdentifier = CType(obj, FileIdentifier)
                    If LastWriteTime = fidObj.LastWriteTime AndAlso Length = fidObj.Length Then
                        Return True
                    Else
                        Return False
                    End If
                End If
            End Function

        End Class

        Private _syncRunning As Boolean

        Public Property Name As String ' Uniquely identifies a storage device by its name.
        Public Property Directories As List(Of LinkedDirectoryPair) ' Contains all pairs of local and external (found on the storage device) directories.

        Public ReadOnly Property SynchronizationRunning As Boolean
            Get
                Return _syncRunning
            End Get
        End Property

        ''' <summary>
        ''' Needed for serialization/deserialization of StorageDevice objects.
        ''' </summary>
        Private Sub New()
        End Sub

        ''' <summary>
        ''' Creates a new StorageDevice object.
        ''' </summary>
        ''' <param name="storageName">A unique name, by which the storage is identified.</param>
        Public Sub New(storageName As String)
            Name = storageName
        End Sub

        ''' <summary>
        ''' Synchronizes files between all linked directory pairs associated with the StorageDevice object. Requires the external storage's root directory.
        ''' </summary>
        ''' <param name="root">Root directory of the connected storage device.</param>
        Public Sub SynchronizeDirectories(root As String)
            _syncRunning = True
            For Each ldp In Directories

                ' Indices: 0 - Source path, 1 - target path.
                Dim dirPaths() As String = IIf(ldp.StoragePath = DynamicPath.SetOnSource,
                                               {$"{root}{ldp.SourcePath}", ldp.TargetPath},
                                               {ldp.SourcePath, $"{root}{ldp.TargetPath}"})
                Dim dirInfos(2) As DirectoryInfo ' DirectoryInfos of both directories.
                Dim dirMapLengths(2) As Integer ' Map lengths of both directories.
                Dim dirTrees(2) As HashSet(Of String) ' Inner tree structure of both directories.
                Dim dirFiles(2) As Dictionary(Of String, FileIdentifier) ' All files in both directories.

                ' Source and target path preparation.
                For dirIdx As Integer = 0 To dirPaths.Length - 1
                    Dim dirLeIdx As Integer = dirIdx

                    If Not Directory.Exists(dirPaths(dirIdx)) Then
                        Directory.CreateDirectory(dirPaths(dirIdx))
                    End If

                    dirInfos(dirIdx) = New DirectoryInfo(dirPaths(dirIdx))
                    dirMapLengths(dirIdx) = dirInfos(dirIdx).FullName.Length + 1

                    dirTrees(dirIdx) = New HashSet(Of String)(
                    Array.ConvertAll(
                        dirInfos(dirIdx).GetDirectories("*", ldp.SyncLevel),
                        Function(di As DirectoryInfo) di.FullName.Substring(dirMapLengths(dirLeIdx))))

                    dirFiles(dirIdx) = dirInfos(dirIdx).GetFiles("*", ldp.SyncLevel).ToDictionary(
                       Function(fiKey As FileInfo) fiKey.FullName.Substring(dirMapLengths(dirLeIdx)),
                       Function(fiValue As FileInfo) New FileIdentifier(fiValue.Length, fiValue.LastWriteTime))
                Next

                ' Source to target directory tree synchronization.
                For Each srcNode In dirTrees(0)
                    ' The source tree is ordered - the most outer directories are always first.
                    If Not dirTrees(1).Contains(srcNode) Then
                        ' If the directory node exists in the source, but not in the destination, create it.
                        Directory.CreateDirectory($"{dirInfos(1).FullName}\{srcNode}")
                    Else
                        ' If the directory node exists in the source and in the destination, remove it from the list.
                        dirTrees(1).Remove(srcNode)
                    End If
                Next

                ' Source to target file synchronization.
                Parallel.ForEach(
                    dirFiles(0).Keys,
                    Sub(fileMapPath As String)
                        If Not dirFiles(1).ContainsKey(fileMapPath) Then
                            ' If the file node exists in the source, but not in the destination, copy it.
                            File.Copy($"{dirInfos(0).FullName}\{fileMapPath}", $"{dirInfos(1).FullName}\{fileMapPath}")
                        Else
                            ' If the file node exists in the source and in the destination and they are not identical, the target file needs to be replaced.
                            If Not dirFiles(0).Item(fileMapPath).Equals(dirFiles(1).Item(fileMapPath)) Then
                                File.Copy($"{dirInfos(0).FullName}\{fileMapPath}", $"{dirInfos(1).FullName}\{fileMapPath}", True)
                            End If
                        End If
                    End Sub)

                ' If the excess files/directories are to be kept in the target location, skip the clean up.
                If ldp.SyncType = SynchronizationType.Additive Then
                    Continue For
                End If

                ' Source to target directory tree cleanup.
                For Each dstNode In dirTrees(1)
                    ' Since the outer-most directories are always first, it's possible for some directories/files to be deleted before they are reached.
                    If Directory.Exists($"{dirInfos(1).FullName}\{dstNode}") Then
                        ' If the directory node doesn't exist in the source, but it does in the target location, delete it.
                        Directory.Delete($"{dirInfos(1).FullName}\{dstNode}", True)
                    End If
                Next

                ' Source to target file cleanup.
                Parallel.ForEach(
                     dirFiles(1).Keys,
                     Sub(fileMapPath As String)
                         If Not dirFiles(0).ContainsKey(fileMapPath) Then
                             If File.Exists($"{dirInfos(1).FullName}\{fileMapPath}") Then
                                 File.Delete($"{dirInfos(1).FullName}\{fileMapPath}")
                             End If
                         End If
                     End Sub)
            Next
            _syncRunning = False
        End Sub

    End Class

    ''' <summary>
    ''' Provides a structure for a single linked directory pair.
    ''' </summary>
    Public Class LinkedDirectoryPair

        ''' <summary>
        ''' Needed for serialization/deserialization of LinkedDirectoryPair objects.
        ''' </summary>
        Private Sub New()
        End Sub

        Public Property SourcePath As String
        Public Property TargetPath As String
        Public Property StoragePath As DynamicPath
        Public Property SyncLevel As SynchronizationLevel
        Public Property SyncType As SynchronizationType

        ''' <summary>
        ''' Requires the source path and the target path, one of which has no root (located on the external storage).
        ''' </summary>
        ''' <param name="srcPath">Source path.</param>
        ''' <param name="tgtPath">Target path.</param>
        ''' <param name="dynPath">Determines the dynamic path, which is the one located on the external storage.</param>
        ''' <param name="sLevel">Indicates whether only the outermost contents of the source path will be synchronized with the target path.</param>
        ''' <param name="sType">Indicates whether the target path's contents should be identical to the source ones or if they should be always kept.</param>
        Public Sub New(srcPath As String, tgtPath As String, dynPath As DynamicPath, sLevel As SynchronizationLevel, sType As SynchronizationType)
            SourcePath = srcPath
            TargetPath = tgtPath
            StoragePath = dynPath
            SyncLevel = sLevel
            SyncType = sType
        End Sub

    End Class

    ''' <summary>
    ''' Sets the path which is determined on runtime by the root of the storage device.
    ''' </summary>
    Public Enum DynamicPath
        SetOnSource = 0
        SetOnTarget = 1
    End Enum

    ''' <summary>
    ''' Defines the synchronization level in a LinkedDirectoryPair.
    ''' </summary>
    Public Enum SynchronizationLevel
        RootDirectoryOnly = 0
        AllSubDirectories = 1
    End Enum

    ''' <summary>
    ''' Defines the synchronization type in a LinkedDirectoryPair.
    ''' </summary>
    Public Enum SynchronizationType
        Mirrored = 0
        Additive = 1
    End Enum

End Namespace