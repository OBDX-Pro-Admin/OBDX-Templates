namespace MAUIExample1;

public partial class SettingsPage : ContentPage
{
	public SettingsPage()
	{
		InitializeComponent();
	}

    private async void LoadLoginButton_Clicked(object sender, EventArgs e)
    {
        //Opens the LOGIN tab and screen after clicking
        await Shell.Current.GoToAsync("//Login");
    }
}