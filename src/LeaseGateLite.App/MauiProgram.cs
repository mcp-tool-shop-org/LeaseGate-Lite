using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace LeaseGateLite.App;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		HookCrashLogging();
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

	private static void HookCrashLogging()
	{
		Directory.CreateDirectory(@"C:\Temp");

		// Managed first-chance exceptions (very noisy but extremely informative)
		AppDomain.CurrentDomain.FirstChanceException += (s, e) =>
		{
			try
			{
				File.AppendAllText(@"C:\Temp\leasegate-firstchance.log",
					$"{DateTime.Now:o} FIRST_CHANCE: {e.Exception.GetType().FullName}: {e.Exception.Message}\n");
			}
			catch { }
		};

		// Unhandled exceptions
		AppDomain.CurrentDomain.UnhandledException += (s, e) =>
		{
			try
			{
				File.AppendAllText(@"C:\Temp\leasegate-crash.log",
					$"{DateTime.Now:o} UNHANDLED: {e.ExceptionObject}\n");
			}
			catch { }
		};

		// Unobserved task exceptions
		TaskScheduler.UnobservedTaskException += (s, e) =>
		{
			try
			{
				File.AppendAllText(@"C:\Temp\leasegate-crash.log",
					$"{DateTime.Now:o} UNOBSERVED: {e.Exception}\n");
			}
			catch { }
			e.SetObserved();
		};

		// WinUI unhandled exceptions (important for UI thread crashes)
#if WINDOWS
		try
		{
			Microsoft.UI.Xaml.Application.Current.UnhandledException += (s, e) =>
			{
				try
				{
					File.AppendAllText(@"C:\Temp\leasegate-crash.log",
						$"{DateTime.Now:o} WINUI: {e.Exception}\n");
				}
				catch { }
				e.Handled = false; // keep default crash behavior
			};
		}
		catch { } // Application.Current might not be set yet
#endif
	}
}
