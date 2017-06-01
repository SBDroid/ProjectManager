Namespace StorageMonitoring

    Public NotInheritable Class StorageMonitor

    End Class

    'Imports System.IO
    'Imports System.Management
    ''Imports System.Threading

    'Namespace StorageSynchronization

    '    ''' <summary>
    '    ''' Automatically synchronizes sets of directories between drives, when an external one is connected. Uses a snapshot algorithm. Usage:
    '    ''' Initialize the StorageMonitor class to begin detection of potential external drives. Drives are identified by their name.
    '    ''' </summary>
    '    Public Class StorageMonitor
    '        Implements IDisposable

    '#Region "Declarations"

    '        Private Const WMI_EVENT As String = "SELECT * FROM __InstanceCreationEvent WITHIN 1 WHERE TargetInstance ISA 'Win32_LogicalDisk'" ' WQL, used to detect newly added external storages by the system.

    '        Private _volumeWatcher As ManagementEventWatcher ' Detects newly added drives.
    '        Private _linkedDirectories As List(Of LinkedDirectoryPair) ' Contains all currently set linked directory pairs.
    '        Private _disposedValue As Boolean ' To detect redundant calls.

    '#End Region

    '#Region "Main"

    '        ''' <summary>
    '        ''' Initializes the StorageSynchronizer class. Requires an external storage name to be associated with.
    '        ''' </summary>
    '        Public Sub New()
    '            _linkedDirectories = New List(Of LinkedDirectoryPair)
    '            _volumeWatcher = New ManagementEventWatcher()
    '            AddHandler _volumeWatcher.EventArrived, AddressOf VolumeArrived
    '            _volumeWatcher.Query = New EventQuery(WMI_EVENT)
    '            _volumeWatcher.Start()
    '        End Sub

    '        ''' <summary>
    '        ''' Called when a new drive is added to the system.
    '        ''' </summary>
    '        ''' <param name="sender"></param>
    '        ''' <param name="e"></param>
    '        Private Sub VolumeArrived(sender As Object, e As EventArrivedEventArgs)
    '            Dim storageDrive As DriveInfo = DriveInfo.GetDrives().FirstOrDefault(
    '            Function(drive As DriveInfo)
    '                Return drive.IsReady AndAlso (drive.DriveType = DriveType.Removable OrElse drive.DriveType = DriveType.Fixed)
    '            End Function) ' Gets the storage's info, only if it matches the provided name.

    '            Dim storageName As String = storageDrive.VolumeLabel
    '            Dim storageRoot As DirectoryInfo = storageDrive?.RootDirectory
    '            If storageRoot?.Exists Then

    '                'If _linkedDirectories.GetCollisions(storageRoot.FullName).Count > 0 Then
    '                '    MsgBox("Collisions detected! Please remove them before continuing.")
    '                '    Exit Sub
    '                'End If

    '                Parallel.For(
    '                0,
    '                _linkedDirectories.Count,
    '                Sub(idx)
    '                    AddHandler _linkedDirectories.Item(idx).OnTotalFilesGotten,
    '                    Sub(filesTotal As Integer)
    '                        'RaiseEvent OnFilesToDoChanged(
    '                        'Interlocked.Add(_allFilesToDo, filesTotal),
    '                        '_allFilesDone)
    '                    End Sub

    '                    AddHandler _linkedDirectories.Item(idx).OnFileProcessed,
    '                    Sub()
    '                        'RaiseEvent OnFilesToDoChanged(
    '                        'Interlocked.Decrement(_allFilesToDo),
    '                        'Interlocked.Increment(_allFilesDone))
    '                    End Sub

    '                    AddHandler _linkedDirectories.Item(idx).OnCleanUpSkipped,
    '                    Sub(filesSkipped As Integer)
    '                        'RaiseEvent OnFilesToDoChanged(
    '                        'Interlocked.Add(_allFilesToDo, -filesSkipped),
    '                        'Interlocked.Add(_allFilesDone, filesSkipped))
    '                    End Sub

    '                    _linkedDirectories.Item(idx).Synchronize(storageRoot.FullName) ' Synchronizes all the linked directory pairs associated with the storage.
    '                End Sub)
    '            End If
    '        End Sub

    '#End Region

    '#Region "Functions"

    '        Public Sub ReportProgress()

    '        End Sub

    '        ''' <summary>
    '        ''' Adds a linked directory pair associated with the current external storage. 
    '        ''' Requires a source and destination directories. One of the paths is a map (directory path without a root).
    '        ''' </summary>
    '        ''' <param name="sourcePath">Source path.</param>
    '        ''' <param name="destinationPath">Destination path.</param>
    '        ''' <param name="destinationCleanUp">Indicates whether the destination directory should match the source one completely.</param>
    '        ''' <param name="outerMostOnly">Indicates whether only the contents of the root directory will be synchronized.</param>
    '        Public Sub AddLinkedDirectories(storageName As String, sourcePath As String, destinationPath As String, Optional destinationCleanUp As Boolean = True, Optional outerMostOnly As Boolean = False)
    '            Dim ldpTemp As New LinkedDirectoryPair(storageName, sourcePath, destinationPath, destinationCleanUp, outerMostOnly)
    '            'If Not _linkedDirectories.ContainsKey(ldpTemp.Key) Then
    '            _linkedDirectories.Add(ldpTemp)
    '            'Else
    '            '    MsgBox("The linked directories already exist.")
    '            'End If
    '        End Sub

    '        Public Sub RemoveLinkedDirectories()

    '        End Sub

    '#End Region

    '#Region "IDisposable Support"

    '        Protected Overridable Sub Dispose(disposing As Boolean)
    '            If Not _disposedValue Then
    '                If disposing Then
    '                    _linkedDirectories.Clear()
    '                End If
    '                _volumeWatcher.Stop()
    '                _volumeWatcher.Dispose()
    '            End If
    '            _disposedValue = True
    '        End Sub

    '        Protected Overrides Sub Finalize()
    '            Dispose(False)
    '            MyBase.Finalize()
    '        End Sub

    '        ''' <summary>
    '        ''' Releases all the resources used by the StorageSynchronizer. 
    '        ''' </summary>
    '        Public Sub Dispose() Implements IDisposable.Dispose
    '            Dispose(True)
    '            GC.SuppressFinalize(Me)
    '        End Sub

    '#End Region

    '    End Class

    'End Namespace

    ''Private ss As New StorageSynchronizer("My Book")
    ''AddHandler ss.OnFilesToDoChanged, AddressOf FilesToDo
    ''ss.AddLinkedDirectories("E:\YT Projects", "YTProj")
    ''ss.Dispose() ' Always explicitly dispose the StorageSynchronizer object

    ''Using rs = Assembly.GetExecutingAssembly.GetManifestResourceStream(Assembly.GetExecutingAssembly.GetName.Name & ".ccp.ico")
    ''    Dim srcImg As New BitmapImage()
    ''    srcImg.BeginInit()
    ''    srcImg.StreamSource = rs
    ''    srcImg.CreateOptions = BitmapCreateOptions.PreservePixelFormat
    ''    srcImg.CacheOption = BitmapCacheOption.OnLoad
    ''    srcImg.EndInit()
    ''    Icon = srcImg
    ''End Using

    ''Private Sub FilesToDo(filesToDo As Integer, filesDone As Integer)
    ''sc.Send(New SendOrPostCallback(
    ''        Sub()
    ''            rtbConsole.AppendText("Files to do: " & filesToDo.ToString & "; Files done: " & filesDone.ToString)
    ''            'Dim lBlock As Block = rtbConsole.Document.Blocks.LastBlock
    ''            'rtbConsole.Document.Blocks.Remove(lBlock)

    ''            'Dim currentText As String = New TextRange(rtbConsole.Document.ContentStart, rtbConsole.Document.ContentEnd).Text
    ''            'richTextBox1.Document.Blocks.Clear();
    ''            'richTextBox1.Document.Blocks.Add(New Paragraph(New Run("Text")));
    ''            'string richText = new TextRange(richTextBox1.Document.ContentStart, richTextBox1.Document.ContentEnd).Text;
    ''        End Sub), Nothing)
    ''End Sub

End Namespace

'Imports System.Configuration
'Imports System.Management
'Imports System.Threading
'Imports System.IO

'''' <summary>
'''' Automatically synchronizes sets of directories between storages, when an external storage is connected. Uses a snapshot algorithm. Usage:
'''' Initialize the StorageSynchronizer class with the name of the external storage's drive. 
'''' Using the AddLinkedDirectories method, a synchronized directory pair is added:
'''' - The source directory. The contents of the directory are always considered the most up-to-date.
'''' - The destination directory. The contents of the directory must always match the ones of the source one, unless the user chooses otherwise. 
'''' Directory pairs associated with a particular external storage (by name), will be persisted between sessions.
'''' They will be automatically loaded when the StorageSynchronizer is initialized with the same name parameter.
'''' </summary>
'Public Class StorageSynchronizer
'    Implements IDisposable

'#Region "Declarations"

'    Private Const DEFAULT_SECTION As String = "LinkedDirectories" ' The section in the App.config where the linked directories are kept.
'    Private Const WMI_EVENT As String = "SELECT * FROM __InstanceCreationEvent WITHIN 1 WHERE TargetInstance ISA 'Win32_LogicalDisk'" ' WQL, used to detect newly added external storages by the system.

'    ''' <summary>
'    ''' Used to load the LinkedDirectories section from App.config. It contains the collection holding all linked directory pairs (configuration elements).
'    ''' </summary>
'    Private Class LinkedDirectoriesSection
'        Inherits ConfigurationSection

'        Private Shared ReadOnly _linkedDirectoryPairs As New ConfigurationProperty(Nothing, GetType(LinkedDirectoryPairCollection), Nothing, ConfigurationPropertyOptions.IsDefaultCollection)
'        Private Shared _properties As New ConfigurationPropertyCollection

'        ''' <summary>
'        ''' Adds all linked directories from the LinkedDirectoryPairCollection collection kept in the section, to the ConfigurationPropertyCollection. 
'        ''' </summary>
'        Shared Sub New()
'            _properties.Add(_linkedDirectoryPairs)
'        End Sub

'        <ConfigurationProperty("", Options:=ConfigurationPropertyOptions.IsDefaultCollection)>
'        Public ReadOnly Property LinkedDirectories As LinkedDirectoryPairCollection
'            Get
'                Return CType(Item(_linkedDirectoryPairs), LinkedDirectoryPairCollection)
'            End Get
'        End Property
'    End Class

'    ''' <summary>
'    ''' A collection used to hold all linked directory pairs (configuration elements) in the LinkedDirectories section.
'    ''' </summary>
'    <ConfigurationCollection(GetType(LinkedDirectoryPair), AddItemName:="Pair", CollectionType:=ConfigurationElementCollectionType.BasicMap)>
'    Private Class LinkedDirectoryPairCollection
'        Inherits ConfigurationElementCollection

'        ''' <summary>
'        ''' The default element is added as "Undefined".
'        ''' </summary>
'        ''' <returns></returns>
'        Protected Overrides Function CreateNewElement() As ConfigurationElement
'            Return New LinkedDirectoryPair("Undefined", "Undefined", "Undefined", True, False)
'        End Function

'        ''' <summary>
'        ''' Gets the corresponding element's key.
'        ''' </summary>
'        ''' <param name="element"></param>
'        ''' <returns></returns>
'        Protected Overrides Function GetElementKey(element As ConfigurationElement) As Object
'            Return CType(element, LinkedDirectoryPair).Key
'        End Function

'        ''' <summary>
'        ''' Adds an element to the collection.
'        ''' </summary>
'        ''' <param name="ldp"></param>
'        Public Sub Add(ldp As LinkedDirectoryPair)
'            If Not ContainsKey(ldp.Key) Then
'                BaseAdd(ldp)
'            Else
'                Throw New ArgumentException("Key already exists.")
'            End If
'        End Sub

'        ''' <summary>
'        ''' Clears all elements from the collection.
'        ''' </summary>
'        Public Sub Clear()
'            BaseClear()
'        End Sub

'        ''' <summary>
'        ''' Checks if the collection contains the corresponding key.
'        ''' </summary>
'        ''' <param name="key"></param>
'        ''' <returns></returns>
'        Public Function ContainsKey(key As String) As Boolean
'            Return Not IsNothing(BaseGet(key))
'        End Function

'        ''' <summary>
'        ''' Gets the element's index in the collection.
'        ''' </summary>
'        ''' <param name="ldp"></param>
'        ''' <returns></returns>
'        Public Function IndexOf(ldp As LinkedDirectoryPair) As Integer
'            Return BaseIndexOf(ldp)
'        End Function

'        ''' <summary>
'        ''' Removes the element from the collection, if it has been already added.
'        ''' </summary>
'        ''' <param name="ldp"></param>
'        Public Sub Remove(ldp As LinkedDirectoryPair)
'            If BaseIndexOf(ldp) >= 0 Then
'                BaseRemove(ldp.Key)
'            End If
'        End Sub

'        ''' <summary>
'        ''' Removes the element from the collection by index.
'        ''' </summary>
'        ''' <param name="index"></param>
'        Public Sub RemoveAt(index As Integer)
'            BaseRemoveAt(index)
'        End Sub

'        Default Public Overloads Property Item(index As Integer) As LinkedDirectoryPair
'            Get
'                Return CType(BaseGet(index), LinkedDirectoryPair)
'            End Get
'            Set
'                If BaseGet(index) IsNot Nothing Then
'                    BaseRemoveAt(index)
'                End If
'                BaseAdd(index, Value)
'            End Set
'        End Property

'        Public Function GetCollisions(root As String) As LinkedDirectoryPairCollection
'            Dim ldpSourceDirectories As New List(Of String)
'            Dim ldpDestinationDirectories As New List(Of String)
'            Dim ldpCollisions As New LinkedDirectoryPairCollection

'            For Each ldpKey In BaseGetAllKeys()
'                Dim ldpTemp As LinkedDirectoryPair = CType(BaseGet(ldpKey), LinkedDirectoryPair)
'                Dim dirPairArr() As String = IIf(ldpTemp.SourcePathIsOnStorage,
'                                             {root & ldpTemp.SourcePath, ldpTemp.DestinationPath},
'                                             {ldpTemp.SourcePath, root & ldpTemp.DestinationPath})

'                If Not ldpDestinationDirectories.Contains(dirPairArr(1)) AndAlso
'                        Not ldpDestinationDirectories.Contains(dirPairArr(0)) AndAlso
'                        Not ldpSourceDirectories.Contains(dirPairArr(1)) Then
'                    ' If the destination is not used as another destination - ok.
'                    ' If the source is not used as another destination - ok.
'                    ' If the destination is not used as another source - ok.
'                    ldpSourceDirectories.Add(dirPairArr(0))
'                    ldpDestinationDirectories.Add(dirPairArr(1))
'                Else
'                    ' Collisions
'                    ldpCollisions.Add(ldpTemp)
'                End If
'            Next
'            Return ldpCollisions
'        End Function

'    End Class

'    ''' <summary>
'    ''' A class used to hold a single linked directory pair (configuration element).
'    ''' </summary>
'    Private Class LinkedDirectoryPair
'        Inherits ConfigurationElement

'        ''' <summary>
'        ''' Used to compare separate files by their size (in bytes) and their last write time.
'        ''' </summary>
'        Private Class FileValues

'            Public ReadOnly Length As Long
'            Public ReadOnly LastWriteTime As Date

'            ''' <summary>
'            ''' Requires the file's byte length as well as its last write time.
'            ''' </summary>
'            ''' <param name="len">File's byte length.</param>
'            ''' <param name="lwt">File's last modified time.</param>
'            Public Sub New(len As Long, lwt As Date)
'                Length = len
'                LastWriteTime = lwt
'            End Sub

'            ''' <summary>
'            ''' FileValues are equal only when both their size and their last write time are identical.
'            ''' </summary>
'            ''' <param name="obj">Comparison object.</param>
'            ''' <returns></returns>
'            Public Overrides Function Equals(obj As Object) As Boolean
'                Dim fvObj As FileValues = TryCast(obj, FileValues)
'                If LastWriteTime = fvObj?.LastWriteTime AndAlso Length = fvObj?.Length Then
'                    Return True
'                Else
'                    Return False
'                End If
'            End Function
'        End Class

'        <ConfigurationProperty(NameOf(SourcePathIsOnStorage), IsRequired:=True)>
'        Public ReadOnly Property SourcePathIsOnStorage As Boolean
'            Get
'                Return Item(NameOf(SourcePathIsOnStorage))
'            End Get
'        End Property

'        <ConfigurationProperty(NameOf(OuterMostOnly), IsRequired:=True)>
'        Public ReadOnly Property OuterMostOnly As Boolean
'            Get
'                Return Item(NameOf(OuterMostOnly))
'            End Get
'        End Property

'        <ConfigurationProperty(NameOf(StorageName), IsRequired:=True)>
'        Public ReadOnly Property StorageName As String
'            Get
'                Return Item(NameOf(StorageName))
'            End Get
'        End Property

'        <ConfigurationProperty(NameOf(SourcePath), IsRequired:=True)>
'        Public ReadOnly Property SourcePath As String
'            Get
'                Return Item(NameOf(SourcePath))
'            End Get
'        End Property

'        <ConfigurationProperty(NameOf(DestinationPath), IsRequired:=True)>
'        Public ReadOnly Property DestinationPath As String
'            Get
'                Return Item(NameOf(DestinationPath))
'            End Get
'        End Property

'        <ConfigurationProperty(NameOf(DestinationCleanUp), IsRequired:=True)>
'        Public ReadOnly Property DestinationCleanUp As Boolean
'            Get
'                Return Item(NameOf(DestinationCleanUp))
'            End Get
'        End Property

'        Friend ReadOnly Property Key As String
'            Get
'                Return $"{StorageName}|{SourcePath}|{DestinationPath}"
'            End Get
'        End Property

'        Public Event OnTotalFilesGotten(filesTotal As Integer) ' Used to return the total number of files in both the source and the destination to the StorageSynchronizer class.
'        Public Event OnFileProcessed() ' Used to return the number of files processed to the StorageSynchronizer class.
'        Public Event OnCleanUpSkipped(filesSkipped As Integer) ' Used to return the total number of files skipped during a clean up.

'        ''' <summary>
'        ''' Requires the external storage's name, the source path and the destination's path, one of which has no root (located on the external storage).
'        ''' </summary>
'        ''' <param name="sName">Storage name.</param>
'        ''' <param name="srcPath">Source path.</param>
'        ''' <param name="dstPath">Destination path.</param>
'        ''' <param name="dstClean">Indicates whether the destination directory should match the source one completely.</param>
'        ''' <param name="outOnly">Indicates whether only the contents of the root directory will be synchronized.</param>
'        Public Sub New(sName As String, srcPath As String, dstPath As String, dstClean As Boolean, outOnly As Boolean)
'            If Directory.Exists(srcPath) AndAlso Not Directory.Exists(dstPath) Then
'                Item(NameOf(SourcePathIsOnStorage)) = False
'            ElseIf Not Directory.Exists(srcPath) AndAlso Directory.Exists(dstPath) Then
'                Item(NameOf(SourcePathIsOnStorage)) = True
'            ElseIf sName.Equals("Undefined") AndAlso srcPath.Equals("Undefined") AndAlso dstPath.Equals("Undefined") AndAlso dstClean AndAlso Not outOnly Then
'                ' Default LinkedDirectoryPairCollection initialization values.
'            Else
'                Throw New ArgumentException("Invalid arguments.")
'            End If

'            Item(NameOf(OuterMostOnly)) = outOnly
'            Item(NameOf(StorageName)) = sName
'            Item(NameOf(SourcePath)) = srcPath
'            Item(NameOf(DestinationPath)) = dstPath
'            Item(NameOf(DestinationCleanUp)) = dstClean
'        End Sub

'        ''' <summary>
'        ''' Synchronizes files between the linked directory pair. Called when the external storage is first connected. 
'        ''' Requires the external storage's root directory to complete the operation.
'        ''' </summary>
'        ''' <param name="root">Root directory of the connected storage.</param>
'        Public Sub Synchronize(root As String)

'            If StorageName.Equals("Undefined") OrElse SourcePath.Equals("Undefined") OrElse DestinationPath.Equals("Undefined") Then
'                Exit Sub
'            End If

'            Dim dirPaths() As String = IIf(SourcePathIsOnStorage, {root & SourcePath, DestinationPath}, {SourcePath, root & DestinationPath})
'            Dim srchOption As SearchOption = IIf(OuterMostOnly, SearchOption.TopDirectoryOnly, SearchOption.AllDirectories)

'            ' Indices: 0 - Source directory, 1 - Destination directory
'            Dim dirInfos(2) As DirectoryInfo ' Directory information of both directories.
'            Dim dirMapLengths(2) As Integer ' Directory path lengths of both directories.
'            Dim dirTrees(2) As HashSet(Of String) ' Inner tree structure of both directories.
'            Dim dirFiles(2) As Dictionary(Of String, FileValues) ' All files in both directories.

'            ' Directory preparation.
'            For dirIdx As Integer = 0 To dirPaths.Length - 1
'                Dim dirLeIdx As Integer = dirIdx

'                If Not Directory.Exists(dirPaths(dirIdx)) Then
'                    Directory.CreateDirectory(dirPaths(dirIdx))
'                End If

'                dirInfos(dirIdx) = New DirectoryInfo(dirPaths(dirIdx))
'                dirMapLengths(dirIdx) = dirInfos(dirIdx).FullName.Length + 1

'                dirTrees(dirIdx) = New HashSet(Of String)(
'                    Array.ConvertAll(
'                    dirInfos(dirIdx).GetDirectories("*", srchOption),
'                    Function(di As DirectoryInfo) di.FullName.Substring(dirMapLengths(dirLeIdx))))

'                dirFiles(dirIdx) = dirInfos(dirIdx).GetFiles("*", srchOption).ToDictionary(
'                   Function(fiKey As FileInfo) fiKey.FullName.Substring(dirMapLengths(dirLeIdx)),
'                   Function(fiValue As FileInfo) New FileValues(fiValue.Length, fiValue.LastWriteTime))
'            Next

'            RaiseEvent OnTotalFilesGotten(dirFiles(0).Keys.Count + dirFiles(1).Keys.Count)

'            ' Directory synchronization.
'            For Each srcNode In dirTrees(0)
'                ' The source tree is ordered - the most outer directories are always first.
'                If Not dirTrees(1).Contains(srcNode) Then
'                    ' If the directory node exists in the source, but not in the destination, create it.
'                    Directory.CreateDirectory(dirInfos(1).FullName & "\" & srcNode)
'                Else
'                    ' If the directory node exists in source and in the destination, remove it from the list.
'                    dirTrees(1).Remove(srcNode)
'                End If
'            Next

'            ' File synchronization.
'            Parallel.ForEach(
'                dirFiles(0).Keys,
'                Sub(fileMapPath As String)
'                    If Not dirFiles(1).ContainsKey(fileMapPath) Then
'                        ' If the file node exists in the source, but not in the destination.
'                        File.Copy(dirInfos(0).FullName & "\" & fileMapPath, dirInfos(1).FullName & "\" & fileMapPath)
'                    Else
'                        ' If the file node exists in the source and in the destination.
'                        If Not dirFiles(0).Item(fileMapPath).Equals(dirFiles(1).Item(fileMapPath)) Then
'                            ' If the files are not the same, the outdated file in the destination needs to be replaced.
'                            File.Delete(dirInfos(1).FullName & "\" & fileMapPath)
'                            File.Copy(dirInfos(0).FullName & "\" & fileMapPath, dirInfos(1).FullName & "\" & fileMapPath)
'                        End If
'                    End If
'                    RaiseEvent OnFileProcessed()
'                End Sub)

'            ' If the excess files/directories are to be kept in the destination, skip the clean up.
'            If Not DestinationCleanUp Then
'                RaiseEvent OnCleanUpSkipped(dirFiles(1).Keys.Count)
'                Exit Sub
'            End If

'            ' Destination directory tree structure cleanup.
'            For Each dstNode In dirTrees(1)
'                ' Since the outer-most directories are always first, it's possible for some nodes to be deleted before they are reached.
'                If Directory.Exists(dirInfos(1).FullName & "\" & dstNode) Then
'                    ' If the directory node doesn't exist in the source, but it does in the destination, delete it in the destination and all its contents.
'                    Directory.Delete(dirInfos(1).FullName & "\" & dstNode, True)
'                End If
'            Next

'            ' Destination directory file cleanup.
'            Parallel.ForEach(
'                 dirFiles(1).Keys,
'                 Sub(fileMapPath As String)
'                     If Not dirFiles(0).ContainsKey(fileMapPath) Then
'                         If File.Exists(dirInfos(1).FullName & "\" & fileMapPath) Then
'                             File.Delete(dirInfos(1).FullName & "\" & fileMapPath)
'                         End If
'                     End If
'                     RaiseEvent OnFileProcessed()
'                 End Sub)
'        End Sub

'    End Class

'    Private _userConfig As Configuration ' Application's configuration.

'    Private _storageName As String ' The storage name associated with the current StorageSynchronizer object.
'    Private _volumeWatcher As ManagementEventWatcher ' Detects newly added drives.

'    Private _linkedDirectories As LinkedDirectoryPairCollection ' Contains all linked directory pairs for the current storage.
'    Private _allFilesToDo As Integer ' The total number of files that have to be synchronized.
'    Private _allFilesDone As Integer ' The total number of files processed during a synchronization.

'    Private _disposedValue As Boolean ' To detect redundant calls.

'    Public Event OnFilesToDoChanged(filesLeft As Integer, filesDone As Integer) ' Reports the total number of files left/processed by the StorageSynchronizer to the initializer of the class.

'#End Region

'#Region "Main"

'    ''' <summary>
'    ''' Initializes the StorageSynchronizer class. Requires an external storage name to be associated with.
'    ''' </summary>
'    ''' <param name="storageName">Storage name.</param>
'    Public Sub New(storageName As String)
'        _storageName = storageName

'        _userConfig = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None) ' Loads the application's configuration.
'        If _userConfig.Sections(DEFAULT_SECTION) Is Nothing Then
'            ' If the default section hasn't been added, add it to the configuration.
'            _userConfig.Sections.Add(DEFAULT_SECTION, New LinkedDirectoriesSection)
'            _userConfig.Save(ConfigurationSaveMode.Full, True)
'        End If

'        Dim userSection As LinkedDirectoriesSection = CType(_userConfig.GetSection(DEFAULT_SECTION), LinkedDirectoriesSection)
'        _linkedDirectories = New LinkedDirectoryPairCollection

'        For Each ldp As LinkedDirectoryPair In userSection.LinkedDirectories
'            If ldp.StorageName = _storageName Then
'                _linkedDirectories.Add(ldp) ' Loads all linked directory pairs associated with the specific storage name.
'            End If
'        Next

'        _volumeWatcher = New ManagementEventWatcher()
'        AddHandler _volumeWatcher.EventArrived, AddressOf VolumeArrived
'        _volumeWatcher.Query = New EventQuery(WMI_EVENT)
'        _volumeWatcher.Start()
'    End Sub

'    ''' <summary>
'    ''' Called when a new drive is added to the system.
'    ''' </summary>
'    ''' <param name="sender"></param>
'    ''' <param name="e"></param>
'    Private Sub VolumeArrived(sender As Object, e As EventArrivedEventArgs)
'        Dim storageDrive As DriveInfo = DriveInfo.GetDrives().FirstOrDefault(
'            Function(drive As DriveInfo)
'                Return drive.VolumeLabel.Equals(_storageName) AndAlso
'                drive.IsReady AndAlso
'                (drive.DriveType = DriveType.Removable OrElse drive.DriveType = DriveType.Fixed)
'            End Function) ' Gets the storage's info, only if it matches the provided name.

'        Dim storageRoot As DirectoryInfo = storageDrive?.RootDirectory
'        If storageRoot?.Exists Then

'            If _linkedDirectories.GetCollisions(storageRoot.FullName).Count > 0 Then
'                MsgBox("Collisions detected! Please remove them before continuing.")
'                Exit Sub
'            End If

'            Parallel.For(
'                0,
'                _linkedDirectories.Count,
'                Sub(idx)
'                    AddHandler _linkedDirectories.Item(idx).OnTotalFilesGotten,
'                    Sub(filesTotal As Integer)
'                        RaiseEvent OnFilesToDoChanged(
'                        Interlocked.Add(_allFilesToDo, filesTotal),
'                        _allFilesDone)
'                    End Sub

'                    AddHandler _linkedDirectories.Item(idx).OnFileProcessed,
'                    Sub()
'                        RaiseEvent OnFilesToDoChanged(
'                        Interlocked.Decrement(_allFilesToDo),
'                        Interlocked.Increment(_allFilesDone))
'                    End Sub

'                    AddHandler _linkedDirectories.Item(idx).OnCleanUpSkipped,
'                    Sub(filesSkipped As Integer)
'                        RaiseEvent OnFilesToDoChanged(
'                        Interlocked.Add(_allFilesToDo, -filesSkipped),
'                        Interlocked.Add(_allFilesDone, filesSkipped))
'                    End Sub

'                    _linkedDirectories.Item(idx).Synchronize(storageRoot.FullName) ' Synchronizes all the linked directory pairs associated with the storage.
'                End Sub)
'        End If
'    End Sub

'#End Region

'#Region "Functions"

'    ''' <summary>
'    ''' Adds a linked directory pair associated with the current external storage. 
'    ''' Requires a source and destination directories. One of the paths is a map (directory path without a root).
'    ''' </summary>
'    ''' <param name="sourcePath">Source path.</param>
'    ''' <param name="destinationPath">Destination path.</param>
'    ''' <param name="destinationCleanUp">Indicates whether the destination directory should match the source one completely.</param>
'    ''' <param name="outerMostOnly">Indicates whether only the contents of the root directory will be synchronized.</param>
'    Public Sub AddLinkedDirectories(sourcePath As String, destinationPath As String, Optional destinationCleanUp As Boolean = True, Optional outerMostOnly As Boolean = False)
'        Dim ldpTemp As New LinkedDirectoryPair(_storageName, sourcePath, destinationPath, destinationCleanUp, outerMostOnly)
'        If Not _linkedDirectories.ContainsKey(ldpTemp.Key) Then
'            _linkedDirectories.Add(ldpTemp)
'            _userConfig.Save(ConfigurationSaveMode.Full, True)
'        Else
'            MsgBox("The linked directories already exist.")
'        End If
'    End Sub

'    Public Sub RemoveLinkedDirectories()

'    End Sub

'#End Region

'#Region "IDisposable Support"

'    Protected Overridable Sub Dispose(disposing As Boolean)
'        If Not _disposedValue Then
'            If disposing Then
'                _userConfig = Nothing
'                _linkedDirectories.Clear()
'                _storageName = Nothing
'            End If
'            _volumeWatcher.Stop()
'            _volumeWatcher.Dispose()
'        End If
'        _disposedValue = True
'    End Sub

'    Protected Overrides Sub Finalize()
'        Dispose(False)
'        MyBase.Finalize()
'    End Sub

'    ''' <summary>
'    ''' Releases all the resources used by the StorageSynchronizer. 
'    ''' </summary>
'    Public Sub Dispose() Implements IDisposable.Dispose
'        Dispose(True)
'        GC.SuppressFinalize(Me)
'    End Sub

'#End Region

'End Class