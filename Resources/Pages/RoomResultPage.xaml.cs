using Maui3DApp.Models;
using Maui3DApp.Services;
using CommunityToolkit.Maui.Storage;

namespace Maui3DApp.Pages;

[QueryProperty(nameof(Result), "Result")]
[QueryProperty(nameof(ParamA),  "A")]
[QueryProperty(nameof(ParamB),  "B")]
[QueryProperty(nameof(ParamC),  "C")]
[QueryProperty(nameof(ParamH1), "h1")]
[QueryProperty(nameof(ParamH2), "h2")]
[QueryProperty(nameof(ParamN),  "N")]
public partial class RoomResultPage : ContentPage
{
    public MagicResult Result { get; set; } = null!;
    public double ParamA  { get; set; }
    public double ParamB  { get; set; }
    public double ParamC  { get; set; }
    public double ParamH1 { get; set; }
    public double ParamH2 { get; set; }
    public double ParamN  { get; set; }

    public RoomResultPage()
    {
        InitializeComponent();

        WebView3D.HandlerChanged += (s, e) =>
        {
#if ANDROID
            if (WebView3D.Handler?.PlatformView is Android.Webkit.WebView wv)
            {
                wv.Settings.JavaScriptEnabled = true;
                wv.Settings.DomStorageEnabled = true;
                wv.Settings.AllowFileAccess   = true;
            }
#endif
        };
        WebView3D.Navigated += (s, e) => UpdateScene();
        WebView3D.Source = new HtmlWebViewSource { Html = LoadHtml("cube.html") };

        WebView2D.HandlerChanged += (s, e) =>
        {
#if ANDROID
            if (WebView2D.Handler?.PlatformView is Android.Webkit.WebView wv)
            {
                wv.Settings.JavaScriptEnabled = true;
                wv.Settings.DomStorageEnabled = true;
                wv.Settings.AllowFileAccess   = true;
            }
#endif
        };
        WebView2D.Navigated += (s, e) => UpdateScene();
        WebView2D.Source = new HtmlWebViewSource { Html = LoadHtml("stairs2d.html") };
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        FillResultTab();
    }

    // ── Результаты ────────────────────────────────────────

    private void FillResultTab()
    {
        var r = Result;
        ResultLabel.Text =
            $"{"".PadRight(40, '=')}\n" +
            $"Результаты:\n" +
            $"Высота проступи:                        {r.Nh:F2}\n" +
            $"Глубина проступи:                       {r.G}\n" +
            $"Ширина лестничных маршей:               {r.SL:F2}\n" +
            $"Забежных ступеней / между маршами:      {r.Middle}\n" +
            $"Разница между маршами:                  " +
                $"{(r.Diff.HasValue ? r.Diff.Value.ToString() : "(не задано)")}\n" +
            $"{"".PadRight(40, '=')}";
    }

    // ── 3D + 2D ──────────────────────────────────────────

    private void UpdateScene()
    {
        double w   = Result.SL;
        double h   = Result.Nh;
        double d   = Result.G;
        int count  = (int)Math.Round(ParamN);
        int mode   = 1;
        int diff   = Result.Diff ?? 0;
        int middle = Result.Middle;

        string js = string.Format(System.Globalization.CultureInfo.InvariantCulture,
            "updateParams({0},{1},{2},{3},{4},{5},{6});",
            w, h, d, count, mode, diff, middle);

        CallJS(WebView3D, js);
        CallJS(WebView2D, js);
    }

    private static string LoadHtml(string fileName)
    {
        using var stream = FileSystem.OpenAppPackageFileAsync(fileName).Result;
        using var reader = new System.IO.StreamReader(stream);
        return reader.ReadToEnd();
    }

    private void CallJS(WebView webView, string js)
    {
#if ANDROID
        if (webView.Handler?.PlatformView is Android.Webkit.WebView wv)
            wv.EvaluateJavascript(js, null);
#else
        webView.Eval(js);
#endif
    }

    // ── Переключение вкладок ──────────────────────────────

    private void SetTab(bool result, bool show3D, bool show2D)
    {
        TabResultContent.IsVisible = result;
        WebView3D.IsVisible        = show3D;
        Tab2DContent.IsVisible     = show2D;

        TabResultButton.BackgroundColor = result ? Color.FromArgb("#2196F3") : Color.FromArgb("#E0E0E0");
        TabResultButton.TextColor       = result ? Colors.White : Color.FromArgb("#333333");
        Tab3DButton.BackgroundColor     = show3D ? Color.FromArgb("#2196F3") : Color.FromArgb("#E0E0E0");
        Tab3DButton.TextColor           = show3D ? Colors.White : Color.FromArgb("#333333");
        Tab2DButton.BackgroundColor     = show2D ? Color.FromArgb("#2196F3") : Color.FromArgb("#E0E0E0");
        Tab2DButton.TextColor           = show2D ? Colors.White : Color.FromArgb("#333333");
    }

    private void OnTabResultClicked(object sender, EventArgs e) => SetTab(true,  false, false);
    private void OnTab3DClicked    (object sender, EventArgs e) => SetTab(false, true,  false);
    private void OnTab2DClicked    (object sender, EventArgs e) => SetTab(false, false, true);

    private async void OnExportClicked(object sender, EventArgs e)
    {
        byte[] pdf = PdfExporter.Export(
            (float)Result.SL,
            (float)Result.Nh,
            (float)Result.G,
            (int)Math.Round(ParamN),
            1,
            Result.Diff ?? 0,
            Result.Middle
        );

        using var stream = new MemoryStream(pdf);
        string fileName = $"stairs-{DateTime.Now:yyyy-MM-dd-HH-mm}.pdf";
        var result = await FileSaver.Default.SaveAsync(fileName, stream, CancellationToken.None);
        
        if (result.IsSuccessful)
            await DisplayAlert("Готово", "Файл сохранён", "OK");
        else
            await DisplayAlert("Ошибка", "Не удалось сохранить файл", "OK");
    }
}