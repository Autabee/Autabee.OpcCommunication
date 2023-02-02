﻿using Autabee.Communication.ManagedOpcClient;
using Autabee.OpcScoutApp.Data;
using Autabee.Utility.Logger;
using Opc.Ua;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Components;


#if ANDROID
using Android.App;
using Android.Content.PM;
#endif
#if WINDOWS
using Microsoft.Win32;
using System.Management;
#endif

namespace Autabee.OpcScoutApp
{
    public static class AutabeeDictionaryExtension
    {
        public static void Add<T, D>(this Dictionary<T, D> dict, KeyValuePair<T, D> keyValuePair)
        {
            dict.Add(keyValuePair.Key, keyValuePair.Value);
        }
    }

    public static class MauiProgram
    {
#if WINDOWS
        static ManagementEventWatcher watcher;
#endif
        private static ApplicationConfiguration uaApplicationConfiguration;
#if Android
        [Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize)]
#endif
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            builder.Services.AddMauiBlazorWebView();
#if WINDOWS
            int res = 0;
            try
            {
                //WqlEventQuery query = new WqlEventQuery(
                //     "SELECT * FROM RegistryValueChangeEvent WHERE " +
                //     "Hive = 'HKEY_CURRENT_USER' " +
                //     @"AND KeyPath ='HKEY_CURRENT_USER\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize' AND ValueName='AppsUseLightTheme'");

                //watcher = new ManagementEventWatcher(query);
                //Console.WriteLine("Waiting for an event...");

                //// Set up the delegate that will handle the change event.
                //watcher.EventArrived += new EventArrivedEventHandler(HandleEvent);

                //// Start listening for events.
                //watcher.Start();

                res = (int)Registry.GetValue("HKEY_CURRENT_USER\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize", "AppsUseLightTheme", -1);
            }
            catch (Exception e)
            {
                //Exception Handling     
            }
#endif

#if DEBUG
            builder.Services.AddSingleton(o =>
            {
                return OpcUaClientHelperApi.CreateDefaultClientConfiguration(Assembly.GetExecutingAssembly()
                    .GetManifestResourceStream("Autabee.OpcScoutApp.autabeeopcscout.Config.xml"));
            }
            );
#else
            builder.Services.AddSingleton(o => OpcUaClientHelperApi.CreateDefaultClientConfiguration("autabeeopcscout",FileSystem.Current.AppDataDirectory));
#endif

#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
#endif
            builder.Services.AddScoped<IPresistantProgramData<List<EndpointRecord>>>(o => new AppProgramData<List<EndpointRecord>>("EndpointsRecord"));


            builder.Services.AddScoped<UserTheme>(o => new UserTheme() { Theme = "app"
#if WINDOWS
                , NavDark = res==0
                , Dark = res==0
#endif
            });
            builder.Services.AddScoped<InMemoryLog>();
            builder.Services.AddScoped<IAutabeeLogger, InMemoryLog>(o => (InMemoryLog)o.GetService(typeof(InMemoryLog)));
            builder.Services.AddSingleton(o => new OpcScoutPersistentData());
            return builder.Build();
        }
#if WINDOWS
        static private void HandleEvent(object sender, EventArrivedEventArgs e)
        {
            Console.WriteLine("Received an event.");
            // RegistryKeyChangeEvent occurs here; do something.
        }
#endif
    }
}