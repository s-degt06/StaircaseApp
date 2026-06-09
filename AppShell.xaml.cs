using Maui3DApp.Pages;

namespace Maui3DApp;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        Routing.RegisterRoute(nameof(RoomPage),       typeof(RoomPage));
        Routing.RegisterRoute(nameof(RoomResultPage), typeof(RoomResultPage));
        Routing.RegisterRoute(nameof(StairsPage),     typeof(StairsPage));
    }
}