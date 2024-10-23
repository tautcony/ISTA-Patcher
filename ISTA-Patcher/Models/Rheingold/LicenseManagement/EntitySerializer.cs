// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2022-2024 TautCony

namespace ISTA_Patcher.Models.Rheingold.LicenseManagement;

using System.Xml;
using System.Xml.Serialization;

public class EntitySerializer<T>
{
    private static XmlSerializer Serializer { get; } = new(typeof(T));

    public string Serialize()
    {
        using var memoryStream = new MemoryStream();
        Serializer.Serialize(memoryStream, this);
        memoryStream.Seek(0L, SeekOrigin.Begin);
        using var streamReader = new StreamReader(memoryStream);
        return streamReader.ReadToEnd();
    }

    public static bool Deserialize(string xmlContent, out T? data, out Exception? exception)
    {
        exception = null;
        data = default;
        try
        {
            data = Deserialize(xmlContent);
            return true;
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
            exception = ex;
            return false;
        }
    }

    public static bool Deserialize(string xmlContent, out T data)
    {
        return Deserialize(xmlContent, out data, out _);
    }

    public static T Deserialize(string xmlContent)
    {
        StringReader? stringReader = null;
        try
        {
            stringReader = new StringReader(xmlContent);
            return (T)Serializer.Deserialize(XmlReader.Create(stringReader)) ?? throw new InvalidOperationException();
        }
        finally
        {
            stringReader?.Dispose();
        }
    }

    public bool SaveToFile(string fileName, out Exception? exception)
    {
        exception = null;
        try
        {
            this.SaveToFile(fileName);
            return true;
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
            exception = ex;
            return false;
        }
    }

    public void SaveToFile(string fileName)
    {
        using var streamWriter = new FileInfo(fileName).CreateText();
        var value = this.Serialize();
        streamWriter.WriteLine(value);
    }

    public static bool LoadFromFile(string fileName, out T? data, out Exception? exception)
    {
        exception = null;
        data = default;
        try
        {
            data = LoadFromFile(fileName);
            return true;
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
            exception = ex;
            return false;
        }
    }

    public static bool LoadFromFile(string fileName, out T data)
    {
        return LoadFromFile(fileName, out data, out _);
    }

    public static T LoadFromFile(string fileName)
    {
        using var fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read);
        using var streamReader = new StreamReader(fileStream);
        var content = streamReader.ReadToEnd();
        return Deserialize(content);
    }
}
