using System.Globalization;
using Maui3DApp.Models;

namespace Maui3DApp.Pages;

public partial class RoomPage : ContentPage
{
    public RoomPage()
    {
        InitializeComponent();
    }

    private async void OnCalculateClicked(object sender, EventArgs e)
    {
        ErrorLabel.IsVisible = false;

        if (!TryParseAll(out double A, out double B, out double C,
                         out double h1, out double h2, out double N))
        {
            ErrorLabel.Text      = "Заполните все поля корректными числами.";
            ErrorLabel.IsVisible = true;
            return;
        }

        if (N == 0)
        {
            ErrorLabel.Text      = "N не может быть равно 0.";
            ErrorLabel.IsVisible = true;
            return;
        }

        var result = StairsCalculator.Magic(A, B, C, h1, h2, N);
        await Shell.Current.GoToAsync(nameof(RoomResultPage),
            new Dictionary<string, object>
            {
                ["Result"] = result,
                ["A"]  = A,  ["B"] = B,  ["C"] = C,
                ["h1"] = h1, ["h2"] = h2, ["N"] = N,
            });
    }

    private bool TryParseAll(
        out double A,  out double B,  out double C,
        out double h1, out double h2, out double N)
    {
        var ci = CultureInfo.InvariantCulture;
        return double.TryParse(AEntry.Text,  NumberStyles.Float, ci, out A)
             & double.TryParse(BEntry.Text,  NumberStyles.Float, ci, out B)
             & double.TryParse(CEntry.Text,  NumberStyles.Float, ci, out C)
             & double.TryParse(H1Entry.Text, NumberStyles.Float, ci, out h1)
             & double.TryParse(H2Entry.Text, NumberStyles.Float, ci, out h2)
             & double.TryParse(NEntry.Text,  NumberStyles.Float, ci, out N);
    }
}