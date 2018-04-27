# Auto Launch UWP App to the Foreground Example

This sample demonstrates how to auto launch and bring a UWP application to the foreground using a Win32 Desktop Extension launcher app.
The sample also shows how to bring a UWP app to the foreground from an in-proc background tast using the Win32 Desktop Extension launcher app.

Note: This example will only work in Desktop scenarios

## Requirements

* Visual Studio 2017 with Windows Universal App Development package installed
* Windows SDK version 17025 (installed with Visual Studio 2017) or minimum SDK version 15063

## Running the Sample

* Open AutoLaunch.sln with Visual Studio 2017

* Select the Debug/x86 or Debug/x64 configuration. (Release/x86 and Release x/64 also work)

* Set the AutoLaunch project as the StartUp project

* Press F5 to build and run the solution. 

Note: If the LauncherExtension project won't build because it can't find the reference to Windows, you will need to remove the reference to Windows in the project and then re-add the 
reference with Add Reference | Browse and browsing to C:\Program Files (x86)\Windows Kits\10\UnionMetadata\10.0.15063.0\Windows.winmd.

* Check the "Run App at Startup" checkbox option.

* Restart the computer. After you login to your account, the AutoLaunch app should appear. A toast message will also appear.

* Uncheck the "Run App at Startup" checkbox option.

* Restart the computer. After you login to your account, the AutoLaunch app should not appear. A toast will appear indicating the app was not launched.

## Launching the UWP App from a Background Task

The UWP App installs a Timezone changed SystemTrigger to test launching the UWP app to the foreground. The Timezone trigger is easy to test.

* Right-click on the Time display in the Taskbar and select "Adjust Date/Time"

* Turn off "Set time zone automatically"

* Change the Time zone setting to a different time zone

* The UWP app should launch into the foreground.


##  Setup Instructions

In order to implement this scenario you will need to do the following

* Create a new Visual C# | Windows Universal | Blank App project (or use your existing UWP project). Set the Minimum platform version to 15063 and the max version to 17025 or 16299.

* Right click on the solution and select Add | New Project...

* Select Visual C# | Windows Classic Desktop | Windows Form App. Name the project LauncherExtension. Select at least .NET framework 4.6.1.

###  Modify the LauncherExtension project

* In the LauncherExtension project, delete the Form1.cs file

* Right-click on the References and select Add Reference. We need to add a few references so we can use some UWP functions.

* Click on the Browse tab and then the Browse button. Browse to the folder C:\Program Files (x86)\Windows Kits\10\UnionMetadata\10.0.15063.0. Make sure you select "All Files (*.*) Select the file Windows.winmd and click Add.

* Click on the Browse tab and then the Browse button. Browse to the folder C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETCore\v4.5. Select the file System.Runtime.WindowsRuntime.dll and click Add.

* Replace the contents of Program.cs with the following

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel;

namespace DesktopExtension
{
    class Program
    {
        static private AutoResetEvent resetEvent;
        static void Main(string[] args)
        {
            resetEvent = new AutoResetEvent(false);
            InvokeForegroundApp();
            resetEvent.WaitOne();
        }

        static private async void InvokeForegroundApp()
        {
            var appListEntries = await Package.Current.GetAppListEntriesAsync();
            await appListEntries.First().LaunchAsync();
            resetEvent.Set();
        }
    }
}
```

###  Modify the UWP App project

We need to add the LauncherExtension.exe to the AppX created by the UWP project. One way to automate this is to do the following:

* Right-click on the UWP project and select "Unload project"

* Right-click on the UWP project and select "Edit project"

* Add the following xml near the end of the project xml

```xml
  <ItemGroup Label="DesktopExtensions">
    <Content Include="$(SolutionDir)\LauncherExtension\bin\$(Configuration)\LauncherExtension.exe">
      <CopyToOutputDirectory>Always</CopyToOutputDirectory>
    </Content>
    <Content Include="$(SolutionDir)\LauncherExtension\bin\$(Configuration)\LauncherExtension.pdb">
      <CopyToOutputDirectory>Always</CopyToOutputDirectory>
    </Content>
  </ItemGroup>
```

Note: If you named your Win32 app with a different project name, replace LauncherExtension with the name of your project.

* Right-click on the UWP project and select "Reload project". The files LauncherExtension.exe and LauncherExtension.pdb should appear in your UWP project.

We now need to add LauncherExtension.exe as a start up task to the UWP app.

* Right-click on Package.appxmanifest and select "View Code".

* Replace the Package tag with the following xml:

```xml
<Package 
  xmlns="http://schemas.microsoft.com/appx/manifest/foundation/windows10" 
  xmlns:mp="http://schemas.microsoft.com/appx/2014/phone/manifest" 
  xmlns:uap="http://schemas.microsoft.com/appx/manifest/uap/windows10" 
  xmlns:uap5="http://schemas.microsoft.com/appx/manifest/uap/windows10/5" 
  xmlns:desktop="http://schemas.microsoft.com/appx/manifest/desktop/windows10" 
  xmlns:rescap="http://schemas.microsoft.com/appx/manifest/foundation/windows10/restrictedcapabilities" 
  IgnorableNamespaces="uap uap5 mp rescap desktop">
```

* Add the following xml to the Application tag

 ```xml
 <Extensions>
    <uap5:Extension Category="windows.startupTask" Executable="LauncherExtension.exe" EntryPoint="Windows.FullTrustApplication">
      <uap5:StartupTask TaskId="LauncherExtension" Enabled="true" DisplayName="LauncherExtension" />
    </uap5:Extension>
    <desktop:Extension Category="windows.fullTrustProcess" Executable="LauncherExtension.exe" />
  </Extensions>
```

* Your Applications section should now look something like this:

```xml
  <Applications>
    <Application Id="App"
      Executable="$targetnametoken$.exe"
      EntryPoint="App13.App">
      <uap:VisualElements
        DisplayName="App13"
        Square150x150Logo="Assets\Square150x150Logo.png"
        Square44x44Logo="Assets\Square44x44Logo.png"
        Description="App13"
        BackgroundColor="transparent">
        <uap:DefaultTile Wide310x150Logo="Assets\Wide310x150Logo.png"/>
        <uap:SplashScreen Image="Assets\SplashScreen.png" />
      </uap:VisualElements>
      <Extensions>
        <uap5:Extension Category="windows.startupTask" Executable="LauncherExtension.exe" EntryPoint="Windows.FullTrustApplication">
          <uap5:StartupTask TaskId="LauncherExtension" Enabled="true" DisplayName="LauncherExtension" />
        </uap5:Extension>
        <desktop:Extension Category="windows.fullTrustProcess" Executable="LauncherExtension.exe" />
      </Extensions>
    </Application>
  </Applications>
 ```
 
 * Add the following capability to the end of the Capabilities section
 
```xml
<rescap:Capability Name="runFullTrust" />
```

* Build the solution and run the UWP app. Use the Debug/x86 or Debug/x64 configuration.

* Restart the computer. Your UWP app should run and come to the foreground after you login.




