using Autabee.Communication.ManagedOpcClient;
using Autabee.OpcScout;
using Microsoft.AspNetCore.Components.WebView.Maui;
using MudBlazor.Services;
using System.Reflection;

namespace MauiApp1
{
    public static class MauiProgram
    {
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

            builder.Services.AddScoped(o => new UserTheme()
            {
                Theme = "app"
            });

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
                return AutabeeManagedOpcClientExtension.CreateDefaultClientConfiguration(Assembly.GetExecutingAssembly()
                    .GetManifestResourceStream("Autabee.OpcScoutApp.autabeeopcscout.Config.xml"));
            }
            );

#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
#endif
            

            return builder.Build();
        }
    }
}