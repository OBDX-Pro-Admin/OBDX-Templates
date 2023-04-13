namespace MAUIExample1;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();

        //apply routing to navigate between pages
        Routing.RegisterRoute(nameof(MainPage), typeof(MainPage));
        Routing.RegisterRoute(nameof(SettingsPage), typeof(SettingsPage));
        Routing.RegisterRoute(nameof(DebugPage), typeof(DebugPage));
        Routing.RegisterRoute(nameof(Login), typeof(Login));

    }
}
