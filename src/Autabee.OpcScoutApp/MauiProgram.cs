using Autabee.Communication.ManagedOpcClient;
using Autabee.OpcScoutApp.Data;
using Autabee.Utility.Logger;
using Opc.Ua;
using System.Net;
using System.Reflection;

#if ANDROID
using Android.App;
using Android.Content.PM;
#endif
namespace Autabee.OpcScoutApp
{
  public static class MauiProgram
  {
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
      builder.Services.AddScoped<IPresistantProgramData<List<EndpointRecord>> >(o=> new AppProgramData<List<EndpointRecord>>("EndpointsRecord"));


      builder.Services.AddScoped<UserTheme>(o => new UserTheme() { Theme = "app" });
      builder.Services.AddScoped<InMemoryLog>();
      builder.Services.AddScoped<IAutabeeLogger, InMemoryLog>(o => (InMemoryLog)o.GetService(typeof(InMemoryLog)));
      builder.Services.AddSingleton<WeatherForecastService>();
      builder.Services.AddSingleton(o => new Controller.OpcScoutBackend());

      return builder.Build();
    }
  }
}