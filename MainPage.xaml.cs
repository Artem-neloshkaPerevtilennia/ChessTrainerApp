namespace ChessTrainerApp;

public partial class MainPage : ContentPage
{
	public MainPage()
	{
		InitializeComponent();
	}

	private async void OnGoToPlayPage(object sender, EventArgs e)
	{
		await Navigation.PushAsync(new PlayPage());
	}

	private async void OnGoToArchievePage(object sender, EventArgs e)
	{
		await Navigation.PushAsync(new ArchievePage());
	}

	private async void OnGoToSettingsPage(object sender, EventArgs e)
	{
		await Navigation.PushAsync(new SettingsPage());
	}

	// private void OnQuit(object sender, EventArgs e)
    // {
    // }
}
