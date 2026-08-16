using System;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DLI.Connect.Utilities;

public static class AvatarProcessor
{
    /// <summary>
    /// Decodes image bytes, center-crops to a square, and re-encodes as JPEG.
    /// Returns the cropped square (max 512px) as (preview, jpeg bytes).
    /// Throws if the bytes are not a decodable image.
    /// </summary>
    public static (BitmapSource Preview, byte[] Jpeg) CropToSquare(byte[] bytes, int maxSize = 512)
    {
        BitmapSource source;
        using (var ms = new MemoryStream(bytes))
        {
            var decoder = BitmapDecoder.Create(ms, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
            if (decoder.Frames.Count == 0)
            {
                throw new InvalidOperationException("Görüntü okunamadı.");
            }
            source = decoder.Frames[0];
        }

        var w = source.PixelWidth;
        var h = source.PixelHeight;
        if (w <= 0 || h <= 0)
        {
            throw new InvalidOperationException("Görüntü okunamadı.");
        }

        // Center crop to square.
        var side = Math.Min(w, h);
        var x = (w - side) / 2;
        var y = (h - side) / 2;
        var cropped = new CroppedBitmap(source, new Int32Rect(x, y, side, side));

        // Downscale if larger than max.
        BitmapSource resized = cropped;
        if (side > maxSize)
        {
            var scale = (double)maxSize / side;
            resized = new TransformedBitmap(cropped, new ScaleTransform(scale, scale));
        }

        var encoder = new JpegBitmapEncoder { QualityLevel = 88 };
        encoder.Frames.Add(BitmapFrame.Create(resized));

        using var outMs = new MemoryStream();
        encoder.Save(outMs);

        var frozen = resized;
        if (frozen.CanFreeze) frozen.Freeze();
        return (frozen, outMs.ToArray());
    }
}
