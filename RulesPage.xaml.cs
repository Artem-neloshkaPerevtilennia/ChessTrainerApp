namespace ChessTrainerApp;

public partial class RulesPage : ContentPage
{
	public RulesPage()
	{
		InitializeComponent();
	}

    private async void OnBackClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}