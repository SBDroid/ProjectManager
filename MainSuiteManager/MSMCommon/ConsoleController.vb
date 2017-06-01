Imports System.Globalization
Imports System.Reflection
Imports System.Threading
Imports MainSuiteManager.KeyMonitoring
Imports MainSuiteManager.MemberOrganization
Imports MainSuiteManager.StorageMonitoring
Imports MasterLib.RegistryManagement
Imports MasterLib.ProcessManagement
Imports MasterLib.FilesystemManagement

''' <summary>
''' Provides all methods that can be called through the MSM console with unique aliases.
''' </summary>
NotInheritable Class ConsoleController

#Region "Declarations"

    ' Add all console-monitored classes here:
    Private Shared _classWatchList() As Type = {
        GetType(ConsoleController),
        GetType(KeyMonitor), GetType(StorageMonitor),
        GetType(KeyCommandOperator), GetType(StorageDeviceOperator), GetType(CompositeMemberOperator)}
    ' ---------------------------------------

    ' Add all console-called methods from monitored classes here:
    Public Const KeyMonitorCapture As String = "kmcpt;Enables/Disables key detection."
    Public Const KeyMonitorClipboardReset As String = "kmcbrst;Initializes/Resets the internal clipboard."

    Public Const KeyCommandOperatorCreate As String = "kcocrt;Creates a new user KeyCommand and saves it to the command repository."
    Public Const KeyCommandOperatorDelete As String = "kcodel;Deletes a command from the repository. Default commands can always be restored."
    Public Const KeyCommandOperatorRestore As String = "kcorst;Removes user commands, restoring the command repository to its default state."
    Public Const KeyCommandOperatorListing As String = "kcolist;Gets all active KeyCommand IDs."
    Public Const KeyCommandOperatorHelp As String = "kcohelp;Gets information about the specified command ID."

    Public Const CompositeMemberOperatorAPIKeyCreate As String = "cmokeycrt;Creates a new API key. Requires a corresponding type to be associated with."
    Public Const CompositeMemberOperatorCount As String = "cmocnt;Gets the total number of results after an automatic member search."
    Public Const CompositeMemberOperatorCreate As String = "cmocrt;Creates a new CompsoiteMember and saves it locally."
    Public Const CompositeMemberOperatorDelete As String = "cmodel;Deletes an existing CompositeMember."
    Public Const CompositeMemberOperatorUpdate As String = "cmoupd;Updates an existing CompositeMember."
    Public Const CompositeMemberOperatorSync As String = "cmosync;Synchronizes all CompositeMembers. This includes: incomplete automatic members, outdated deliveries for both automatic and manual members."
    ' ------------------------------------------------------

    ' Add ConsoleController-specific methods here:
    Public Const MSMHelp As String = "help;Gets the call parameters and the description of the specified MSM method."
    Public Const MSMList As String = "list;Lists all methods that can be executed through the MSM console."
    Public Const MSMStartup As String = "startup;Determines whether MSM should start on Windows startup."
    Public Const MSMExit As String = "exit;Terminates MSM, after waiting for it to dispose all its resources."
    Public Const MSMEmote As String = "talktome;Prints some random text on the MSM console."
    Public Const MSMAbout As String = "about;About."
    Public Const MSMClear_0 As String = "clear;Clears the MSM console."
    Public Const MSMClear_1 As String = "clr;Clears the MSM console."
    Public Const MSMClear_2 As String = "cls;Clears the MSM console."
    Public Const MSMCAP_ChromeShutdownScheduler As String = "capcss;Schedules a system shutdown when all Chrome downloads are completed."
    Public Const MSMCAP_MediaFileFormatter As String = "capmff;Formats media files and analyzes the results. Resulting file names as well as ID3 tags will be irreversibly modified!"
    Public Const MSMSServiceToggle As String = "svc;Starts/Stops an installed Windows service by name."
    Public Const MSMFileExtensionChange As String = "ext;Changes the extensions of all specified files in a directory."
    ' --------------------------------------------

    Private Shared _msmResponses() As String = {
        "MSM: Guy who made me has no idea what he's doing...",
        "MSM: You know you're chatting with a hard-coded bot right? You must be really bored.",
        "MSM: The 0110001001100101011000010111001001100100 is a lie.",
        "MSM: This was written on 6.05.2016, a Sunday. What a waste.",
        "MSM: Oh hi Mark.",
        "Dev: This feature is the most autistic thing I've done all day.",
        $"MSM: I care about you, your feelings, your goals and interests.{Environment.NewLine}...{Environment.NewLine}j/k lol",
        "MSM: Kappa BORT FrankerZ PJSalt RuleFive",
        "Response #42: Chutney. That is all.",
        "MSM: Suffering from a severe case of cenosillicaphobia?",
        "MSM: At least someone finds me useful.",
        "MSM: ...",
        "MSM: HTID!!!",
        "MSM: XAML is love. XAML is life.",
        "Dev: I should really get back to work.",
        "MSM: Ultrafunkular.",
        "MSM: Supercalifragilisticexpialidocious.",
        "Dev: Have to create method descriptions. Not happy."}

    Private Shared _waitHandle As AutoResetEvent

    Shared Event OnStatusChanged(statusMessage As String, err As Boolean) ' Provides status messages to the user of the ConsoleController class.
    Shared Event OnClearRequest() ' Raised when the user requests a console clear.
    Shared Event OnMSMExit(e As AutoResetEvent) ' Raised when the application is terminated.

#End Region

#Region "Main"

    ''' <summary>
    ''' Calls a method by alias, provided its parameters have been correctly specified by the user.
    ''' </summary>
    ''' <param name="callLst">The user's provided list of parameters.</param>
    ''' <returns>Returns the result of the method, if any.</returns>
    Shared Function MethodInvocation(callLst As List(Of String)) As Object
        For Each classType As Type In _classWatchList
            Dim mInfo As MethodInfo = GetMethodToCall(callLst.Item(0), classType)
            If Not mInfo Is Nothing Then
                Dim paramArr() As Object = GenerateParameterCallArray(callLst, 1, mInfo.GetParameters)
                Dim pInfo As PropertyInfo = classType.GetProperties.FirstOrDefault(
                    Function(pi)
                        Return pi.Name.ToUpper.Equals("INSTANCE") AndAlso
                        pi.CanWrite = False AndAlso pi.CanRead AndAlso
                        pi.GetMethod.IsStatic AndAlso pi.GetMethod.IsPublic AndAlso
                        pi.GetMethod.ReturnType Is classType
                    End Function) ' Singleton handling.
                Return mInfo.Invoke(pInfo?.GetValue(Nothing), paramArr)
            End If
        Next
        Throw New ConsoleSyntaxException
    End Function

    ''' <summary>
    ''' Gets the call parameters and/or description of the specified MSM method.
    ''' </summary>
    <MethodAlias(MSMHelp)>
    Shared Sub GetMethodHelp(methodID As String)
        If GetType(ConsoleController).GetFields.Select(Function(fi) fi.GetRawConstantValue.ToString.Split(";")(0)).ToArray.Contains(methodID) Then
            For Each classType As Type In _classWatchList
                Dim mInfo As MethodInfo = GetMethodToCall(methodID, classType)
                If Not mInfo Is Nothing Then
                    Dim mAlias As MethodAlias = GetMethodAlias(methodID, mInfo)
                    Dim message As String = $"{methodID} - {mAlias.Description} Parameters: {GenerateParameterDescription(mInfo.GetParameters)}"
                    RaiseEvent OnStatusChanged(message, False)
                    Exit Sub
                End If
            Next
        End If
        RaiseEvent OnStatusChanged($"{MSMHelp.Split(";")(0)} - Syntax error! '{methodID}' is an unknown command.", True)
    End Sub

    ''' <summary>
    ''' Lists all methods that can be executed through the MSM console.
    ''' </summary>
    <MethodAlias(MSMList)>
    Shared Sub GetMethodsList()
        RaiseEvent OnStatusChanged($"Full MSM command listing: {String.Join(", ", GetType(ConsoleController).GetFields.Select(
                                                                            Function(fi) fi.GetRawConstantValue.ToString.Split(";")(0)).ToArray)}", False)
    End Sub

    ''' <summary>
    ''' Determines whether MSM should start on Windows startup.
    ''' </summary>
    ''' <param name="startOnStartUp">Start up flag.</param>
    <MethodAlias(MSMStartup)>
    Shared Sub SetStartUp(startOnStartUp As Boolean)
        Dim res As String = StartupManager.SetApplicationStartUp(startOnStartUp)
        RaiseEvent OnStatusChanged(res, res.Contains("Error"))
    End Sub

    ''' <summary>
    ''' Terminates MSM, after waiting for it to dispose all its resources.
    ''' </summary>
    <MethodAlias(MSMExit)>
    Shared Sub Terminate()
        _waitHandle = New AutoResetEvent(False)
        RaiseEvent OnMSMExit(_waitHandle)
        _waitHandle.WaitOne()
        Environment.Exit(0)
    End Sub

    ''' <summary>
    ''' Prints some random text on the MSM console.
    ''' </summary>
    <MethodAlias(MSMEmote)>
    Shared Sub Emote()
        RaiseEvent OnStatusChanged(_msmResponses(New Random().Next(0, _msmResponses.Count)), False)
    End Sub

    ''' <summary>
    ''' About.
    ''' </summary>
    <MethodAlias(MSMAbout)>
    Shared Sub About()
        RaiseEvent OnStatusChanged(Environment.NewLine &
            "-----------------------------------------" & Environment.NewLine &
            "Author: Ivan Stoev, thesbdroid@gmail.com" & Environment.NewLine &
            "Copyright: ©2015-2016, Ivan Stoev" & Environment.NewLine &
            "Summary: This application is free software; you can redistribute it and/or modify it under the terms of the GNU Lesser General Public License version 3.0 as published by the Free Software Foundation." & Environment.NewLine &
            "----------------------------------------", False)
    End Sub

    ''' <summary>
    ''' Raises the MSM console clear request event.
    ''' </summary>
    <MethodAlias(MSMClear_0), MethodAlias(MSMClear_1), MethodAlias(MSMClear_2)>
    Shared Sub ClearRequested()
        RaiseEvent OnClearRequest()
    End Sub

    ''' <summary>
    ''' Schedules a system shutdown when all Chrome downloads are completed.
    ''' </summary>
    <MethodAlias(MSMCAP_ChromeShutdownScheduler)>
    Shared Sub ShutdownScheduler()
        Dim prc As New Process()
        prc.StartInfo = New ProcessStartInfo() With {.FileName = IO.Path.Combine(My.Application.Info.DirectoryPath, GetType(ChromeShutdownScheduler.ModMaster).Module.Name), .UseShellExecute = False}
        If Not ProcessAnalyzer.AreAnyInstancesRunning(prc) Then
            prc.Start()
        Else
            RaiseEvent OnStatusChanged("Another instance of the process is already running...", True)
        End If
    End Sub

    ''' <summary>
    ''' Formats media files and analyzes the results. Resulting file names as well as ID3 tags will be irreversibly modified!
    ''' </summary>
    <MethodAlias(MSMCAP_MediaFileFormatter)>
    Shared Sub FileFormatter()
        Dim prc As New Process()
        prc.StartInfo = New ProcessStartInfo() With {.FileName = IO.Path.Combine(My.Application.Info.DirectoryPath, GetType(MediaFileFormatter.ModMaster).Module.Name), .UseShellExecute = False}
        If Not ProcessAnalyzer.AreAnyInstancesRunning(prc) Then
            prc.Start()
        Else
            RaiseEvent OnStatusChanged("Another instance of the process is already running...", True)
        End If
    End Sub

    ''' <summary>
    ''' Starts/Stops an installed Windows service by name.
    ''' </summary>
    ''' <param name="serviceName">Service name.</param>
    <MethodAlias(MSMSServiceToggle)>
    Shared Sub WinServiceToggle(serviceName As String)
        Dim res As String = ServiceManager.WinServiceToggle(serviceName)
        RaiseEvent OnStatusChanged(res, res.Contains("Error"))
    End Sub

    ''' <summary>
    ''' Changes the extensions of all specified files in a directory.
    ''' </summary>
    ''' <param name="workDirectory">Input directory.</param>
    ''' <param name="newExtension">New file extension.</param>
    ''' <param name="fileFilter">Optional file filter. If not specified, all files in the input directory will be changed.</param>
    <MethodAlias(MSMFileExtensionChange)>
    Shared Sub FileExtensionChange(workDirectory As String, newExtension As String, Optional fileFilter As String = Nothing)
        AddHandler GenericFileProcessor.OnStatusChanged, AddressOf RaiseRemoteEvents
        GenericFileProcessor.ChangeFileExtensionsInDirectory(workDirectory, newExtension, fileFilter)
        RemoveHandler GenericFileProcessor.OnStatusChanged, AddressOf RaiseRemoteEvents
    End Sub

#End Region

#Region "Internal"

    ''' <summary>
    ''' Cannot be initialized directly.
    ''' </summary>
    Private Sub New()
    End Sub

    ''' <summary>
    ''' Generates a description of the method's parameters.
    ''' </summary>
    ''' <param name="pInfoArr">The method's list of parameters by signature.</param>
    ''' <returns></returns>
    Private Shared Function GenerateParameterDescription(pInfoArr() As ParameterInfo) As String
        Dim resDescription As String = ""
        For piIdx As Integer = 0 To pInfoArr.Length - 1
            Dim paramType As Type = pInfoArr(piIdx).ParameterType
            If paramType.IsValueType OrElse paramType Is GetType(String) Then
                resDescription &= $"{IIf(pInfoArr(piIdx).IsOptional, "Optional ", "")}{pInfoArr(piIdx).Name}"
                If paramType.IsEnum Then
                    resDescription &= $" [Enum ({String.Join("|", [Enum].GetNames(paramType))})]"
                Else
                    Dim typeNameArr() As String = paramType.ToString.Split(".")
                    resDescription &= $" [{typeNameArr(typeNameArr.Length - 1)}]"
                End If
            Else
                resDescription &= GenerateParameterDescription(GetConstructorToCall(paramType).GetParameters)
            End If
            If Not piIdx = pInfoArr.Length - 1 Then resDescription &= ", "
        Next
        Return IIf(String.IsNullOrWhiteSpace(resDescription), "none", resDescription)
    End Function

    ''' <summary>
    ''' Generates a parameter array for a method to be called with.
    ''' </summary>
    ''' <param name="callLst">The user's provided list of parameters.</param>
    ''' <param name="callArrIdx">The current parameter index. Used when recursion is required.</param>
    ''' <param name="pInfoArr">The method's list of parameters by signature.</param>
    ''' <returns></returns>
    Private Shared Function GenerateParameterCallArray(callLst As List(Of String), ByRef callArrIdx As Integer, pInfoArr() As ParameterInfo) As Object()
        Dim callParam As New List(Of Object)
        For Each pInfo As ParameterInfo In pInfoArr
            If pInfo.IsOptional AndAlso callLst.Count - 1 < callArrIdx Then
                ' Optional parameter that was not supplied by the user.
                callParam.Add(Nothing)
                callArrIdx += 1
            Else
                ' Required or supplied optional parameter.
                If callLst.Count - 1 < callArrIdx Then
                    ' If the required parameter was not supplied by the user.
                    Throw New ConsoleSyntaxException
                End If

                Dim paramType As Type = pInfo.ParameterType
                If paramType.IsValueType OrElse paramType Is GetType(String) Then
                    If paramType Is GetType(Date) Then
                        callParam.Add(CTypeDynamic(
                                      Date.ParseExact(callLst(callArrIdx), "dd/MM/yyyy", CultureInfo.InvariantCulture), paramType))
                    Else
                        callParam.Add(CTypeDynamic(
                                      callLst(callArrIdx), paramType))
                    End If
                    callArrIdx += 1
                Else
                    Dim cInfo As ConstructorInfo = GetConstructorToCall(paramType)
                    If Not cInfo Is Nothing Then
                        callParam.Add(CTypeDynamic(
                                      Activator.CreateInstance(paramType, GenerateParameterCallArray(callLst, callArrIdx, cInfo.GetParameters)), paramType))
                    Else
                        ' No accessible constructor was found.
                        Throw New ConsoleSyntaxException
                    End If
                End If
            End If
        Next
        Return callParam.ToArray
    End Function

    ''' <summary>
    ''' Determines the constructor of a class to call.
    ''' </summary>
    ''' <param name="classType">The class type.</param>
    ''' <returns></returns>
    Private Shared Function GetConstructorToCall(classType As Type) As ConstructorInfo
        Return classType.GetConstructors.FirstOrDefault(
            Function(ci)
                Dim cCtor As ConstructorAlias = CType(ci.GetCustomAttribute(GetType(ConstructorAlias)), ConstructorAlias)
                Return classType.Name = cCtor?.ID
            End Function)
    End Function

    ''' <summary>
    ''' Determines the method to call given its corresponding class type and its ID.
    ''' </summary>
    ''' <param name="ID">The method ID.</param>
    ''' <param name="classType">The class type.</param>
    ''' <returns></returns>
    Private Shared Function GetMethodToCall(ID As String, classType As Type) As MethodInfo
        Return classType.GetMethods.FirstOrDefault(
            Function(mi)
                Return Not GetMethodAlias(ID, mi) Is Nothing
            End Function)
    End Function

    ''' <summary>
    ''' Determines whether the method implements the MethodAlias attribute and if it matches the MethodAlias' ID.
    ''' </summary>
    ''' <param name="ID">The MethodAlias' ID.</param>
    ''' <param name="mi">The method's MethodInfo object.</param>
    ''' <returns>Returns the MethodAlias object or null if no matches are found.</returns>
    Private Shared Function GetMethodAlias(ID As String, mi As MethodInfo)
        Return mi.GetCustomAttributes(GetType(MethodAlias), False)?.FirstOrDefault(
            Function(ma As MethodAlias)
                Return ma.ID = ID
            End Function)
    End Function

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