using System.Globalization;

namespace Maui3DApp.Pages;

public partial class StairsPage : ContentPage
{
    public StairsPage()
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

        WebView3D.Source = new HtmlWebViewSource
        {
            Html = LoadHtml("cube.html")
        };

        BindSizeControl(WidthSlider,  WidthEntry);
        BindSizeControl(HeightSlider, HeightEntry);
        BindSizeControl(DepthSlider,  DepthEntry);

        CountEntry.Completed  += (s, e) => UpdateScene();
        DiffEntry.Completed   += (s, e) => UpdateScene();
        MiddleEntry.Completed += (s, e) => UpdateScene();

        ModePicker.SelectedIndex = 0;
        ModePicker.SelectedIndexChanged += (s, e) => UpdateScene();

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

        WebView2D.Source = new HtmlWebViewSource
        {
            Html = LoadHtml("stairs2d.html")
        };
    }

    // ── Переключение вкладок ──────────────────────────────

    private void OnTab3DClicked(object sender, EventArgs e)
    {
        WebView3D.IsVisible      = true;
        Tab2DContent.IsVisible   = false;
        Tab3DButton.BackgroundColor = Color.FromArgb("#2196F3");
        Tab3DButton.TextColor       = Colors.White;
        Tab2DButton.BackgroundColor = Color.FromArgb("#E0E0E0");
        Tab2DButton.TextColor       = Color.FromArgb("#333333");
    }

    private void OnTab2DClicked(object sender, EventArgs e)
    {
        WebView3D.IsVisible      = false;
        Tab2DContent.IsVisible   = true;
        Tab2DButton.BackgroundColor = Color.FromArgb("#2196F3");
        Tab2DButton.TextColor       = Colors.White;
        Tab3DButton.BackgroundColor = Color.FromArgb("#E0E0E0");
        Tab3DButton.TextColor       = Color.FromArgb("#333333");
    }

    private async void OnExportClicked(object sender, EventArgs e)
        => await DisplayAlert("Экспорт", "PDF сохранён (заглушка)", "OK");

    // ── WebGL ─────────────────────────────────────────────

    private static string LoadHtml(string fileName)
    {
        using var stream = FileSystem.OpenAppPackageFileAsync(fileName).Result;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private void BindSizeControl(Slider slider, Entry entry)
    {
        entry.Text = slider.Value.ToString("0.00", CultureInfo.InvariantCulture);

        slider.ValueChanged += (s, e) =>
        {
            entry.Text = e.NewValue.ToString("0.00", CultureInfo.InvariantCulture);
            UpdateScene();
        };

        entry.Completed += (s, e) =>
        {
            if (double.TryParse(entry.Text, NumberStyles.Float,
                                CultureInfo.InvariantCulture, out double v))
                slider.Value = Math.Clamp(v, slider.Minimum, slider.Maximum);
        };
    }

    private void UpdateScene()
    {
        int count = 1;
        if (!string.IsNullOrWhiteSpace(CountEntry.Text))
        {
            int.TryParse(CountEntry.Text, out count);
            if (count < 1) count = 1;
        }

        int diff = 0;
        if (!string.IsNullOrWhiteSpace(DiffEntry.Text))
            int.TryParse(DiffEntry.Text, out diff);

        int middle = 0;
        if (!string.IsNullOrWhiteSpace(MiddleEntry.Text))
            int.TryParse(MiddleEntry.Text, out middle);

        string js = string.Format(CultureInfo.InvariantCulture,
            "updateParams({0},{1},{2},{3},{4},{5},{6});",
            WidthSlider.Value, HeightSlider.Value, DepthSlider.Value,
            count, ModePicker.SelectedIndex, diff, middle);

        CallJS(WebView3D, js);

        string js2d = string.Format(CultureInfo.InvariantCulture,
            "updateParams({0},{1},{2},{3},{4},{5},{6});",
            WidthSlider.Value, HeightSlider.Value, DepthSlider.Value,
            count, ModePicker.SelectedIndex, diff, middle);

        CallJS(WebView2D, js2d);
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
}