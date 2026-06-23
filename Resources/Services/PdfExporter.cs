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

        (float X, float Z) Rotate((float X, float Z) p)
            => (-p.Z, p.X);
        var allPoints = shapes
            .SelectMany(s => s.Points)
            .Select(Rotate)
            .ToList();

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
            var p0 = Rotate(pts[0]);

            path.MoveTo(
                offsetX + p0.X * scale,
                offsetZ + p0.Z * scale);

            for (int i = 1; i < pts.Count; i++)
            {
                var p = Rotate(pts[i]);

                path.LineTo(
                    offsetX + p.X * scale,
                    offsetZ + p.Z * scale);
            }
            path.Close();

            canvas.DrawPath(path, fillPaint);
            canvas.DrawPath(path, strokePaint);
        }

        // --- Размерные линии (ширина) ---

        // Координаты конкретной фигуры в системе страницы (после поворота/масштаба).
        (float MinX, float MaxX, float MinY, float MaxY) RotatedBounds(IEnumerable<StairShape> group)
        {
            var pagePts = group
                .SelectMany(s => s.Points)
                .Select(Rotate)
                .Select(p => (X: offsetX + p.X * scale, Y: offsetZ + p.Z * scale))
                .ToList();
            return (pagePts.Min(p => p.X), pagePts.Max(p => p.X),
                    pagePts.Min(p => p.Y), pagePts.Max(p => p.Y));
        }

        bool isUnmirroredL = mode == 4;

        // 1) Площадка / блок забежных ступеней — во всех режимах, где он есть (1..4).
        var platformShapes = shapes.Where(s => s.IsPlatform).ToList();
        if (platformShapes.Count > 0)
        {
            var pb = RotatedBounds(platformShapes);
            DrawWidthDimension(
                canvas,
                pb.MinX, pb.MaxX,
                isUnmirroredL ? pb.MaxY : pb.MinY,
                scale,
                above: !isUnmirroredL); // в mode==3 — под платформой, иначе над
        }

        // 2) Самая правая (верхняя) ступень.
        var stepShapes = shapes.Where(s => !s.IsPlatform).ToList();
        if (stepShapes.Count > 0)
        {
            var upperFlight = stepShapes.Where(s => s.Flight == 1).ToList();

            // В режиме 3 (он же "режим 4" по вашей нумерации) — просто самая
            // правая ступень среди всех, без привязки к "верхнему" пролёту,
            // т.к. боковой пролёт там не "вытягивается" вправо по странице.
            var rightmost = stepShapes
                .OrderByDescending(s =>
                    s.Points.Select(Rotate)
                            .Max(p => offsetX + p.X * scale))
                .First();

            var sb = RotatedBounds(new[] { rightmost });
            DrawWidthDimension(canvas, sb.MinX, sb.MaxX, sb.MinY, scale, above: true);
        }

        doc.EndPage();
        doc.Close();

        return stream.ToArray();
    }

    // Рисует горизонтальную размерную линию (выносные линии + стрелки + текст)
    // над (above=true) или под (above=false) указанным отрезком [xMin..xMax] на
    // уровне yEdge (координаты страницы). Значение для подписи вычисляется как
    // фактическая протяжённость отрезка в мировых единицах (xMax-xMin)/scale —
    // то есть всегда соответствует тому, что нарисовано на странице.
    private static void DrawWidthDimension(
        SKCanvas canvas, float xMin, float xMax, float yEdge, float scale, bool above)
    {
        const float gap      = 18f; // отступ размерной линии от контура фигуры
        const float arrowLen = 6f;
        const float arrowWing = 3f;
        const float fontSize = 13f;

        float y = above ? yEdge - gap : yEdge + gap;

        using var linePaint = new SKPaint
        {
            Color       = SKColors.Black,
            StrokeWidth = 1f,
            Style       = SKPaintStyle.Stroke,
            IsAntialias = true,
        };

        // выносные линии от контура фигуры до размерной линии
        canvas.DrawLine(xMin, yEdge, xMin, y, linePaint);
        canvas.DrawLine(xMax, yEdge, xMax, y, linePaint);

        // сама размерная линия
        canvas.DrawLine(xMin, y, xMax, y, linePaint);

        void Arrow(float x, int dir)
        {
            using var arrowPath = new SKPath();
            arrowPath.MoveTo(x, y);
            arrowPath.LineTo(x + dir * arrowLen, y - arrowWing);
            arrowPath.MoveTo(x, y);
            arrowPath.LineTo(x + dir * arrowLen, y + arrowWing);
            canvas.DrawPath(arrowPath, linePaint);
        }

        Arrow(xMin, +1);
        Arrow(xMax, -1);

        float value = MathF.Round((xMax - xMin) / scale);

        using var textPaint = new SKPaint
        {
            Color       = SKColors.Black,
            TextSize    = fontSize,
            TextAlign   = SKTextAlign.Center,
            IsAntialias = true,
        };

        float textY = above ? y - 4f : y + fontSize + 2f;
        canvas.DrawText($"{value:0}", (xMin + xMax) / 2f, textY, textPaint);
    }
}