using System;
using System.Globalization;
using System.IO;
using Microsoft.Maui.Controls;

namespace Maui3DApp;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();

        // ===============================
        // WebView platform settings
        // ===============================
        WebView3D.HandlerChanged += (s, e) =>
        {
#if ANDROID
            if (WebView3D.Handler?.PlatformView is Android.Webkit.WebView wv)
            {
                wv.Settings.JavaScriptEnabled = true;
                wv.Settings.DomStorageEnabled = true;
                wv.Settings.AllowFileAccess = true;
            }
#endif
        };

        // ===============================
        // Load local HTML
        // ===============================
        WebView3D.Source = new HtmlWebViewSource
        {
            Html = LoadHtmlFromResources("cube.html")
        };

        // ===============================
        // Bind sliders and entries
        // ===============================
        BindSizeControl(WidthSlider, WidthEntry);
        BindSizeControl(HeightSlider, HeightEntry);
        BindSizeControl(DepthSlider, DepthEntry);

        // Bind CountEntry
        CountEntry.Completed += (s, e) => UpdateScene();

        // Initial update
        UpdateScene();
    }

    private string LoadHtmlFromResources(string fileName)
    {
        using var stream = FileSystem.OpenAppPackageFileAsync(fileName).Result;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    // ===============================
    // Slider <-> Entry binding
    // ===============================
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
            if (double.TryParse(entry.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
            {
                value = Math.Clamp(value, slider.Minimum, slider.Maximum);
                slider.Value = value;
            }
        };
    }

    // ===============================
    // Call JS function updateParams
    // ===============================
    private void UpdateScene()
    {
        int count = 1;
        if (!string.IsNullOrWhiteSpace(CountEntry.Text))
        {
            int.TryParse(CountEntry.Text, out count);
            if (count < 1) count = 1;
        }

        string js = string.Format(CultureInfo.InvariantCulture,
            "updateParams({0},{1},{2},{3});",
            WidthSlider.Value,
            HeightSlider.Value,
            DepthSlider.Value,
            count
        );

        CallJS(js);
    }

    private void CallJS(string js)
    {
#if ANDROID
        if (WebView3D.Handler?.PlatformView is Android.Webkit.WebView wv)
        {
            wv.EvaluateJavascript(js, null);
        }
#else
        WebView3D.Eval(js);
#endif
    }
}
