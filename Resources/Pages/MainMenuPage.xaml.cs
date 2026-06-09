using Maui3DApp.Pages;

namespace Maui3DApp.Pages;

public partial class MainMenuPage : ContentPage
{
    public MainMenuPage()
    {
        InitializeComponent();
    }

    private async void OnRoomClicked(object sender, EventArgs e)
        => await Shell.Current.GoToAsync(nameof(RoomPage));

    private async void OnStairsClicked(object sender, EventArgs e)
        => await Shell.Current.GoToAsync(nameof(StairsPage));
}