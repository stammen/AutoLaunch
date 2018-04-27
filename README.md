# Auto Launch UWP App to the Foreground Example

This sample demonstrates how to auto launch and bring a UWP application to the foreground using a Win32 Desktop Extension launcher app.
The sample also shows how to bring a UWP app to the foreground from a background tast using the Win32 Desktop Extension launcher app.

Note: This example will only work in Desktop scenarios

## Requirements

* Visual Studio 2017 with Windows Universal App Development package installed
* Windows SDK version 17025 (installed with Visual Studio 2017) or minimum SDK version 15063

## Running the Sample

* Open AutoLaunch.sln with Visual Studio 2017

* Select the Debug/x86 or Debug/x64 configuration. (Release/x86 and Release x/64 also work)

* Set the AutoLaunch project as the StartUp project

* Press F5 to build and run the solution. 

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


