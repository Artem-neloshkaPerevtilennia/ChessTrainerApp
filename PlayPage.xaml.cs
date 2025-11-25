namespace ChessTrainerApp;

public partial class PlayPage : ContentPage
{
    public PlayPage()
    {
        InitializeComponent();
    }

    private async void OnStartGame(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new ChessBoardPage());
    }
}
