namespace MAUIExample;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();

        //apply routing to navigate between pages
        Routing.RegisterRoute(nameof(MainPage), typeof(MainPage));
        Routing.RegisterRoute(nameof(DebugPage), typeof(DebugPage));

    }
}
