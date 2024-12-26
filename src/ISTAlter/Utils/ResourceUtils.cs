// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2024 TautCony

// ReSharper disable AccessToDisposedClosure
namespace ISTAlter.Utils;

using System.Collections;
using System.Globalization;
using System.Numerics;
using System.Resources;
using dnlib.DotNet;
using Serilog;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

public static class ResourceUtils
{
    public static Stream? GetFromResource(ModuleDefMD module, string resourceName, string fileName)
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

    public static void UpdateResource(ModuleDefMD module, string resourceName, string fileName, byte[] newContent)
    {
        UpdateResource(module, resourceName, fileName, _ => newContent);
    }

    public static void UpdateResource(ModuleDefMD module, string resourceName, string fileName, Func<DictionaryEntry, byte[]> handler)
    {
        var resourceFound = false;
        var fileFound = false;
        foreach (var resource in module.Resources)
        {
            if (resource is not EmbeddedResource embeddedResource || resource.Name != resourceName)
            {
                continue;
            }

            resourceFound = true;

            using var resourceStream = embeddedResource.CreateReader().AsStream();
            using var resourceReader = new ResourceReader(resourceStream);

            using var updatedResourceStream = new MemoryStream();
            using var resourceWriter = new ResourceWriter(updatedResourceStream);

            foreach (DictionaryEntry entry in resourceReader)
            {
                var key = entry.Key.ToString()!;
                if (string.Equals(key, fileName, StringComparison.Ordinal))
                {
                    fileFound = true;
                    resourceWriter.AddResource(key, handler(entry));
                }
                else
                {
                    if (entry.Value is Stream stream)
                    {
                        using var valueStream = new MemoryStream();
                        stream.CopyTo(valueStream);
                        resourceWriter.AddResource(key, valueStream.ToArray());
                    }
                    else
                    {
                        resourceWriter.AddResource(key, entry.Value);
                    }
                }
            }

            resourceWriter.Generate();
            updatedResourceStream.Position = 0;
            var updatedResource = new EmbeddedResource(resource.Name, updatedResourceStream.ToArray(), ManifestResourceAttributes.Public);

            module.Resources.Remove(resource);
            module.Resources.Add(updatedResource);
        }

        if (!resourceFound)
        {
            Log.Error($"Resource {resourceName} not found.");
            return;
        }

        if (!fileFound)
        {
            Log.Error($"File {fileName} in resource {resourceName} not found.");
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
        using var image = Image.Load(input);
        var fontFamily = SystemFonts.Get("Arial", CultureInfo.CurrentUICulture);
        var font = fontFamily.CreateFont(32, FontStyle.Regular);

        var textMetrics = TextMeasurer.MeasureBounds(watermarkText, new TextOptions(font));
        var stepX = textMetrics.Width * 1.2f;
        var stepY = textMetrics.Height * 1.5f;

        using var watermarkLayer = new Image<Rgba32>(image.Width * 2, image.Height * 3);
        watermarkLayer.Mutate(ctx =>
        {
            for (float x = 0; x < watermarkLayer.Height; x += stepX)
            {
                for (float y = 0; y < watermarkLayer.Width; y += stepY)
                {
                    ctx.DrawText(
                        new RichTextOptions(font)
                        {
                            Origin = new PointF(x + 1, y + 1),
                            HorizontalAlignment = HorizontalAlignment.Center,
                        },
                        watermarkText,
                        Color.Black.WithAlpha(0.2f));

                    ctx.DrawText(
                        new RichTextOptions(font)
                        {
                            Origin = new PointF(x, y),
                            HorizontalAlignment = HorizontalAlignment.Center,
                        },
                        watermarkText,
                        Color.White.WithAlpha(0.6f));
                }
            }
        });

        watermarkLayer.Mutate(ctx =>
        {
            var affine = new AffineTransformBuilder()
                .AppendRotationDegrees(-27.1828f)
                .AppendTranslation(new Vector2(-image.Height, -image.Width));

            ctx.Transform(affine);
        });

        image.Mutate(ctx => ctx.DrawImage(watermarkLayer, 1f));

        using var ms = new MemoryStream();
        image.SaveAsPng(ms);
        return ms.ToArray();
    }
}
