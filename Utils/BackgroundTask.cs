using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Background;

namespace Utils
{
    sealed class BackGroundTask
    {
        public const string TimezoneTriggerTaskName = "timezoneTrigger";

        public static void RegisterSystemBackgroundTask(string name, SystemTriggerType trigger)
        {
            UnregisterBackgroundTask(name);
            var requestStatus = BackgroundExecutionManager.RequestAccessAsync();
            var builder = new BackgroundTaskBuilder();
            builder.Name = name;
            builder.SetTrigger(new SystemTrigger(trigger, false));
            var task = builder.Register();
        }

        public static async Task TriggerApplicationBackgroundTask(string name)
        {
            try
            {
                var appTrigger = new ApplicationTrigger();
                if (!IsBackgroundTaskRegistered(name))
                {
                    var requestStatus = BackgroundExecutionManager.RequestAccessAsync();
                    var builder = new BackgroundTaskBuilder();
                    builder.Name = name;
                    builder.SetTrigger(appTrigger);
                    var task = builder.Register();
                }

                var result = await appTrigger.RequestAsync();
            }
            catch (Exception ex)
            {
               Debug.WriteLine("TriggerApplicationBackgroundTask exception: " + ex.Message);
            }
        }

        public static void UnregisterBackgroundTask(string name)
        {
            var count = BackgroundTaskRegistration.AllTasks.Count;
            foreach (var task in BackgroundTaskRegistration.AllTasks)
            {
                if (task.Value.Name == name)
                {
                    task.Value.Unregister(true);
                }
            }
        }

        public static bool IsBackgroundTaskRegistered(string name)
        {
            var count = BackgroundTaskRegistration.AllTasks.Count;
            foreach (var task in BackgroundTaskRegistration.AllTasks)
            {
                if (task.Value.Name == name)
                {
                    return true;
                }
            }

            return false;
        }
    }
}

