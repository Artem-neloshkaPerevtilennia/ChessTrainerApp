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
		await Navigation.PushAsync(new ArchivePage());
	}

	private async void OnGoToRulesPage(object sender, EventArgs e)
	{
		await Navigation.PushAsync(new RulesPage());
	}

	private void OnQuit(object sender, EventArgs e)
	{
		Application.Current.Quit();
	}
}
