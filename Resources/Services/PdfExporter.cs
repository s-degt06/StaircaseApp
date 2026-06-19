using System.Linq;
using SkiaSharp;
using Maui3DApp.Models;

namespace Maui3DApp.Services;

public static class PdfExporter
{
    public static byte[] Export(
        float w, float h, float d,
        int count, int mode, int diff, int middle)
    {
        const float pageW   = 794f;  // A4 в пикселях при 96dpi
        const float pageH   = 1123f;
        const float margin  = 40f;
        const float padding = 20f;

        var shapes = StairsLayout.Build(w, h, d, count, mode, diff, middle);
        var allPoints = shapes.SelectMany(s => s.Points).ToList();

        // Находим bounding box чертежа
        float minX = allPoints.Min(p => p.X);
        float minZ = allPoints.Min(p => p.Z);
        float maxX = allPoints.Max(p => p.X);
        float maxZ = allPoints.Max(p => p.Z);

        float drawW = maxX - minX;
        float drawH = maxZ - minZ;

        // Масштаб чтобы влезло в страницу
        float availW = pageW - margin * 2 - padding * 2;
        float availH = pageH - margin * 2 - padding * 2;
        float scale  = Math.Min(availW / drawW, availH / drawH);

        // Центрирование
        float offsetX = margin + padding + (availW - drawW * scale) / 2 - minX * scale;
        float offsetZ = margin + padding + (availH - drawH * scale) / 2 - minZ * scale;

        using var stream = new MemoryStream();
        using var doc    = SKDocument.CreatePdf(stream);
        using var canvas = doc.BeginPage(pageW, pageH);

        // Фон
        canvas.Clear(SKColors.White);

        // Рамка
        using var borderPaint = new SKPaint
        {
            Color       = SKColors.Black,
            StrokeWidth = 1f,
            Style       = SKPaintStyle.Stroke,
        };
        canvas.DrawRect(margin, margin, pageW - margin * 2, pageH - margin * 2, borderPaint);

        // Заливка фигур
        using var fillPaint = new SKPaint
        {
            Color = SKColors.White,
            Style = SKPaintStyle.Fill,
        };

        // Контур фигур
        using var strokePaint = new SKPaint
        {
            Color       = SKColors.Black,
            StrokeWidth = 1.5f,
            Style       = SKPaintStyle.Stroke,
        };

        foreach (var shape in shapes)
        {
            var pts = shape.Points;
            using var path = new SKPath();
            path.MoveTo(offsetX + pts[0].X * scale, offsetZ + pts[0].Z * scale);
            for (int i = 1; i < pts.Count; i++)
                path.LineTo(offsetX + pts[i].X * scale, offsetZ + pts[i].Z * scale);
            path.Close();

            canvas.DrawPath(path, fillPaint);
            canvas.DrawPath(path, strokePaint);
        }

        doc.EndPage();
        doc.Close();

        return stream.ToArray();
    }
}