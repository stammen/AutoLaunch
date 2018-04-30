//*********************************************************
//
// Copyright (c) Microsoft. All rights reserved.
// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//*********************************************************
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Core;

namespace LauncherExtension
{
    class Program
    {
        static private AutoResetEvent resetEvent;
        static void Main(string[] args)
        {
            bool startApp = true;
            string message = "args: " + args.Count() + "\n";
            int count = 0;
            foreach(string s in args)
            {
                message += count++ + ":" + s + "\n";
            }

            resetEvent = new AutoResetEvent(false);
            if(args.Count() == 0) // app was launched by system at startup
            {
                // check if we should launch the UWP app
                startApp = Utils.UserSettings.GetValueForKey<bool>(Utils.UserSettings.RUN_APP_AT_STARTUP, true);
            }

            if (startApp)
            {
                InvokeForegroundApp();
            }

            message += "Start UWP App: " + startApp;
            Utils.Toasts.ShowToast("LauncherExtension", message);

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
