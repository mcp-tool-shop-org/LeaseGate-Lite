using Microsoft.Extensions.DependencyInjection;

namespace LeaseGateLite.App;

public partial class App : Application
{
	private readonly MainPage _mainPage;

	public App()
	{
		InitializeComponent();
		_mainPage = IPlatformApplication.Current!.Services.GetRequiredService<MainPage>();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(_mainPage);
	}
}