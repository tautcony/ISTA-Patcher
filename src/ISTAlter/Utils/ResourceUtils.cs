// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2024-2026 TautCony

namespace ISTAlter.Utils;

using System.Collections;
using System.Resources;
using dnlib.DotNet;
using Serilog;
using SkiaSharp;

public static class ResourceUtils
{
    public static Stream? GetFromResource(ModuleDef module, string resourceName, string fileName)
    {
        foreach (var resource in module.Resources)
        {
            if (resource is not EmbeddedResource embeddedResource || resource.Name != resourceName)
            {
                continue;
            }

            using var resourceStream = embeddedResource.CreateReader().AsStream();
            using var resourceReader = new ResourceReader(resourceStream);
            foreach (DictionaryEntry entry in resourceReader)
            {
                if (string.Equals(entry.Key.ToString(), fileName, StringComparison.Ordinal))
                {
                    return entry.Value as Stream;
                }
            }
        }

        throw new Exception("Resource not found.");
    }

    public static void UpdateResource(ModuleDef module, string resourceName, string fileName, byte[] newContent)
    {
        UpdateResource(module, resourceName, fileName, _ => newContent);
    }

    public static void UpdateResource(ModuleDef module, string resourceName, string fileName, Func<DictionaryEntry, byte[]> handler)
    {
        if (module.Resources.FirstOrDefault(r => r.Name == resourceName) is not EmbeddedResource embeddedResource)
        {
            Log.Error("Resource {ResourceName} not found.", resourceName);
            return;
        }

        using var resourceStream = embeddedResource.CreateReader().AsStream();
        using var resourceReader = new ResourceReader(resourceStream);

        using var updatedResourceStream = new MemoryStream();
        using var resourceWriter = new ResourceWriter(updatedResourceStream);

        var fileFound = false;

        foreach (DictionaryEntry entry in resourceReader)
        {
            var key = entry.Key.ToString()!;
            if (string.Equals(key, fileName, StringComparison.Ordinal))
            {
                fileFound = true;
                var newValue = handler(entry);
                var newValueStream = new MemoryStream(newValue);
                resourceWriter.AddResource(key, newValueStream, closeAfterWrite: true);
            }
            else
            {
                if (entry.Value is Stream stream)
                {
                    var valueStream = new MemoryStream();
                    stream.CopyTo(valueStream);
                    resourceWriter.AddResource(key, valueStream, closeAfterWrite: true);
                }
                else
                {
                    resourceWriter.AddResource(key, entry.Value);
                }
            }
        }

        resourceWriter.Generate();
        updatedResourceStream.Position = 0;
        var updatedResource = new EmbeddedResource(embeddedResource.Name, updatedResourceStream.ToArray(), embeddedResource.Attributes);

        module.Resources.Remove(embeddedResource);
        module.Resources.Add(updatedResource);

        if (!fileFound)
        {
            Log.Error("File {FileName} in resource {ResourceName} not found.", fileName, resourceName);
        }
    }

    public static byte[] AddWatermark(Stream stream, string watermarkText)
    {
        using var memoryStream = new MemoryStream();
        stream.CopyTo(memoryStream);
        return AddWatermark(memoryStream.ToArray(), watermarkText);
    }

    public static byte[] AddWatermark(byte[] input, string watermarkText)
    {
        using var original = SKBitmap.Decode(input);
        using var typeface = SKTypeface.FromFamilyName("Arial", SKFontStyle.Normal);
        using var font = new SKFont(typeface, 32);

        using var shadowPaint = new SKPaint();
        shadowPaint.IsAntialias = true;
        shadowPaint.Color = new SKColor(0, 0, 0, (byte)(255 * 0.2f));
        using var textPaint = new SKPaint();
        textPaint.IsAntialias = true;
        textPaint.Color = new SKColor(255, 255, 255, (byte)(255 * 0.6f));

        font.GetFontMetrics(out var metrics);
        var textWidth = font.MeasureText(watermarkText);
        var textHeight = metrics.Descent - metrics.Ascent;
        var stepX = textWidth * 1.2f;
        var stepY = textHeight * 1.5f;

        using var layerBitmap = new SKBitmap(original.Width * 2, original.Height * 3);
        using var layerCanvas = new SKCanvas(layerBitmap);
        layerCanvas.Clear(SKColors.Transparent);

        for (var x = 0f; x < layerBitmap.Height; x += stepX)
        {
            for (var y = 0f; y < layerBitmap.Width; y += stepY)
            {
                layerCanvas.DrawText(watermarkText, x + 1, y + 1 - metrics.Ascent, SKTextAlign.Left, font, shadowPaint);
                layerCanvas.DrawText(watermarkText, x, y - metrics.Ascent, SKTextAlign.Left, font, textPaint);
            }
        }

        using var resultBitmap = new SKBitmap(original.Width, original.Height);
        using var resultCanvas = new SKCanvas(resultBitmap);
        resultCanvas.DrawBitmap(original, 0, 0, SKSamplingOptions.Default);

        resultCanvas.Save();
        resultCanvas.Translate(-original.Height, -original.Width);
        resultCanvas.RotateDegrees(-27.1828f);
        resultCanvas.DrawBitmap(layerBitmap, 0, 0, SKSamplingOptions.Default);
        resultCanvas.Restore();

        using var resultImage = SKImage.FromBitmap(resultBitmap);
        using var data = resultImage.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }
}
