using Autabee.Communication.ManagedOpcClient;
using Autabee.OpcScout;
using Autabee.OpcScout.Data;
using Autabee.OpcScout.RazorControl;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using MudBlazor.Services;
using Serilog;
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

            builder.Services.AddBlazorContextMenu();
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

           
            builder.Services.AddSingleton(o =>
            {
                //var ass = Assembly.GetExecutingAssembly();
                //var manifest = ass.GetManifestResourceStream("Autabee.OpcScoutWeb.autabeeopcscout.Config.xml");
                return AutabeeManagedOpcClientExtension.GetClientConfiguration("autabee", "opc_scout", "data/opc_certs/", null);
            }
            );

            builder.Services.AddScoped(o => new UserTheme()
            {
                Theme = "app",
                NavLinked = true
                
            });

            builder.Services.AddScoped(o =>
            {
                var logger = new Serilog.LoggerConfiguration()
                    .MinimumLevel.Verbose()
                    .WriteTo.Sink(o.GetRequiredService<InMemoryLog>())
                    .WriteTo.File("logs/log.txt", rollingInterval: RollingInterval.Day)
                    .WriteTo.Console()
                    .CreateLogger();
                return logger;
            });


            builder.Services.AddScoped<InMemoryLog>();
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