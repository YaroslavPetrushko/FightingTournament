using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FightingTournament.Services;

public static class PngExporter
{
    public static void Export(FrameworkElement element, string filePath)
    {
        if (element == null) return;

        // Force a layout update to ensure we capture actual dimensions
        double width = element.ActualWidth;
        double height = element.ActualHeight;

        if (width <= 0 || height <= 0)
        {
            element.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            element.Arrange(new Rect(element.DesiredSize));
            width = element.DesiredSize.Width;
            height = element.DesiredSize.Height;
        }

        if (width <= 0 || height <= 0) return;

        var source = PresentationSource.FromVisual(element);
        double dpiX = 96d, dpiY = 96d;
        if (source?.CompositionTarget != null)
        {
            dpiX = 96d * source.CompositionTarget.TransformToDevice.M11;
            dpiY = 96d * source.CompositionTarget.TransformToDevice.M22;
        }

        // Render the visual element onto a Bitmap
        RenderTargetBitmap rtb = new RenderTargetBitmap(
            (int)(width * dpiX / 96d),
            (int)(height * dpiY / 96d),
            dpiX, dpiY,
            PixelFormats.Pbgra32);

        rtb.Render(element);

        // Encode to PNG and save to disk
        PngBitmapEncoder encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(rtb));

        using var fs = File.OpenWrite(filePath);
        encoder.Save(fs);
    }
}
