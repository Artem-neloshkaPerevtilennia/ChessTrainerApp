using ChessTrainerApp.ViewModels;

namespace ChessTrainerApp;

public partial class ArchivePage : ContentPage
{
	public ArchivePage()
	{
		InitializeComponent();
	}

    // Цей метод спрацьовує автоматично, коли сторінка з'являється на екрані
    protected override async void OnAppearing()
    {
        base.OnAppearing();

        // Отримуємо доступ до ViewModel і просимо завантажити дані
        if (BindingContext is ArchiveViewModel vm)
        {
            await vm.LoadGames();
        }
    }
}