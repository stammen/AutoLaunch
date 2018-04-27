//*********************************************************
//
// Copyright (c) Microsoft. All rights reserved.
// THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF
// ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY
// IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR
// PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.
//
//*********************************************************


using Windows.Storage;

namespace Utils
{
    public sealed class UserSettings
    {
        public const string RUN_APP_AT_STARTUP = "RUN_APP_AT_STARTUP";

        public static void SetValueForKey<T>(string key, T value)
        {
            if (key == "" || key == null)
            {
                return;
            }

            ApplicationDataContainer localSettings = ApplicationData.Current.LocalSettings;
            localSettings.Values[key] = value;
        }

        public static T GetValueForKey<T>(string key, T defaultValue)
        {
            T result = defaultValue;

            if (key != "" && key != null)
            {
                ApplicationDataContainer settings = ApplicationData.Current.LocalSettings;
                if (null != settings.Values[key] && settings.Values[key].GetType() == typeof(T))
                {
                    result = (T)settings.Values[key];
                }
            }

            return result;
        }

    }
}

