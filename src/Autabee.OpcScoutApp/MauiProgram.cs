using Autabee.Communication.ManagedOpcClient;
using Autabee.OpcScout;
using Autabee.OpcScout.Data;
using Autabee.OpcScout.RazorControl;
using Autabee.Utility.Logger;
using Microsoft.AspNetCore.Components.WebView.Maui;
using Microsoft.Win32;
using MudBlazor.Services;
using System.Reflection;

namespace Autabee.OpcScoutApp
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
#if WINDOWS
			int res = 0;
			try
			{

				res = (int)Registry.GetValue("HKEY_CURRENT_USER\\SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Themes\\Personalize", "AppsUseLightTheme", -1);
			}
			catch (Exception e)
			{
				//Exception Handling     
			}
#endif
			builder.Services.AddScoped(o => new UserTheme()
			{
				Theme = "app"
#if WINDOWS
				,NavDark = res == 0
				,Dark = res == 0
#endif
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


			builder.Services.AddScoped<InMemoryLog>();
			builder.Services.AddScoped<IAutabeeLogger, InMemoryLog>(o => (InMemoryLog)o.GetService(typeof(InMemoryLog)));
			builder.Services.AddSingleton<OpcScoutPersistentData>();
			builder.Services.AddScoped<IPersistentProgramData<List<EndpointRecord>>>(o => new AppProgramData<List<EndpointRecord>>("EndpointsRecord"));
#if DEBUG
			builder.Services.AddBlazorWebViewDeveloperTools();
#endif
			

			return builder.Build();
		}
	}
}