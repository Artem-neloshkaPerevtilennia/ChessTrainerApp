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

	private async void OnPlayBlindfold(object sender, EventArgs e)
	{
		// Створюємо сторінку
		var gamePage = new ChessBoardPage();

		if (gamePage.BindingContext is ChessTrainerApp.ViewModels.ChessBoardViewModel vm)
		{
			// Запускаємо: Гравець Білі, Глибина 2, Режим Навчальний, Наосліп = TRUE
			vm.SetupGame(
				ChessTrainerApp.Models.PieceColor.White, 
				2, 
				ChessTrainerApp.Models.GameMode.Training, 
				true); // <--- Вмикаємо сліпий режим
		}

		await Navigation.PushAsync(gamePage);
	}

	private void OnQuit(object sender, EventArgs e)
	{
		Application.Current.Quit();
	}
}
