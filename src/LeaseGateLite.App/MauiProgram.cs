using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace LeaseGateLite.App;

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
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		builder.Services.AddTransient<MainPage>();
		builder.Services.AddHttpClient<DaemonApiClient>(client =>
		{
			client.BaseAddress = new Uri("http://localhost:5177");
			client.Timeout = TimeSpan.FromSeconds(5);
			client.DefaultRequestHeaders.Add("X-Client-AppId", "LeaseGateLite.App");
			client.DefaultRequestHeaders.Add("X-Process-Name", Process.GetCurrentProcess().ProcessName);
		});

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
