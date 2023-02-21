using Autabee.Communication.ManagedOpcClient;
using Autabee.OpcScout;
using Autabee.OpcScout.Data;
using Autabee.OpcScout.RazorControl;
using Autabee.OpcScoutWeb.Data;
using Autabee.Utility.Logger;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor.Services;
using System.Reflection;

namespace Autabee.OpcScoutWeb
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddRazorPages();
            builder.Services.AddServerSideBlazor();

            builder.Services.AddMudServices(
                config =>
                {
                    config.SnackbarConfiguration.PositionClass = MudBlazor.Defaults.Classes.Position.BottomLeft;

                    config.SnackbarConfiguration.PreventDuplicates = false;
                    config.SnackbarConfiguration.NewestOnTop = false;
                    config.SnackbarConfiguration.ShowCloseIcon = true;
                    config.SnackbarConfiguration.VisibleStateDuration = 10000;
                    config.SnackbarConfiguration.HideTransitionDuration = 500;
                    config.SnackbarConfiguration.ShowTransitionDuration = 500;
                    config.SnackbarConfiguration.SnackbarVariant = MudBlazor.Variant.Filled;
                });

            

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
            builder.Services.AddSingleton(o =>
            {
                return AutabeeManagedOpcClientExtension.CreateDefaultClientConfiguration(Assembly.GetExecutingAssembly()
                    .GetManifestResourceStream("Autabee.OpcScoutApp.autabeeopcscout.Config.xml"));
            }
            );

            builder.Services.AddScoped(o => new UserTheme()
            {
                Theme = "app"
#if WINDOWS
                , NavDark = res==0
                , Dark = res==0
#endif
            });
            builder.Services.AddScoped<InMemoryLog>();
            builder.Services.AddScoped<IAutabeeLogger, InMemoryLog>(o => (InMemoryLog)o.GetService(typeof(InMemoryLog)));
            builder.Services.AddSingleton<OpcScoutPersistentData>();

            builder.Services.AddScoped<IPersistentProgramData<List<EndpointRecord>>>(o => new DataFolder<List<EndpointRecord>>("EndpointsRecord"));


            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            
            app.UseHttpsRedirection();

            app.UseStaticFiles();

            app.UseRouting();

            app.MapBlazorHub();
            app.MapFallbackToPage("/_Host");

            app.Run();
        }
    }
}