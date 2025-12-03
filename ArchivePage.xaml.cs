using ChessTrainerApp.ViewModels;

namespace ChessTrainerApp;

public partial class ArchivePage : ContentPage
{
	public ArchivePage()
	{
		InitializeComponent();
	}

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is ArchiveViewModel vm)
        {
            await vm.LoadGames();
        }
    }
}