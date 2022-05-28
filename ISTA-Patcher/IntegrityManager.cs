using System.Security.Cryptography;
using System.Text;

namespace ISTA_Patcher;

public class IntegrityManager
{
    private static readonly byte[] _salt = { 0xd, 0xca, 0x32, 0xe0, 0x7f, 0xa4, 0xdf, 0xf1 };

    private const int _iterations = 1100;

    private const string _password = "3/3HexbKKFs4LqpiCSgKAXGUYCtqjoFchfPitAmI8wE=";
    
    private const string pk_xml = "<RSAKeyValue><Modulus>xW33nQA29jyJSYn24fVcSIU3gQmzQArcT0lrPAj94PS8wuZZBpPZsLEWo4pkq2/w9ne4V9PTOkB2frVBvA/bmGF/gyHivqkzi7znX/TwcTM6GbX/MN4isNeXqgFZzjmxOh9EYPt8pnJ/j02Djbg8LceG98grBCehBe/2wFxxYQQa+YoJ0a1ymzs/3geBTeqtwYgayZeLEWOxckoDuDu0RWF8zvVcWxUNpwqHNH/4Boo+xLqByfEv2wDS1zchGtjCL+g2qdDWlHgASEgGZ6Z8hbirrxxWYZ7zaZxjSADQM8nweKn4t4+p44uD1Aoktq3Mm+jZtTsgk8i1YjbCQN8J1Q==</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>";

    public static List<HashFileInfo>? DecryptFile(string sourceFilename)
    {
        try
        {
            var aesManaged = new AesManaged();
            aesManaged.BlockSize = aesManaged.LegalBlockSizes[0].MaxSize;
            aesManaged.KeySize = aesManaged.LegalKeySizes[0].MaxSize;
            var rfc2898DeriveBytes = new Rfc2898DeriveBytes(_password, _salt, _iterations);
            aesManaged.Key = rfc2898DeriveBytes.GetBytes(aesManaged.KeySize / 8);
            aesManaged.IV = rfc2898DeriveBytes.GetBytes(aesManaged.BlockSize / 8);
            aesManaged.Mode = CipherMode.CBC;
            var transform = aesManaged.CreateDecryptor(aesManaged.Key, aesManaged.IV);
            using var memoryStream = new MemoryStream();
            using var cryptoStream = new CryptoStream(memoryStream, transform, CryptoStreamMode.Write);
            using (var fileStream = new FileStream(sourceFilename, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                fileStream.CopyTo(cryptoStream);
            }
            var bytes = memoryStream.ToArray();
            return (from row in Encoding.UTF8.GetString(bytes).Split(";;\r\n", StringSplitOptions.RemoveEmptyEntries)
                select new HashFileInfo(row.Split(";;", StringSplitOptions.RemoveEmptyEntries))).ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to decrypt file: {ex.Message}");
        }
        return null;
    }
}

public class HashFileInfo
{
    public string FileName { get; private set; }

    public string FilePath { get; private set; }

    public string Hash { get; set; }

    protected internal HashFileInfo(string[] fileInfos)
    {
        FilePath = fileInfos[0];
        FileName = Path.GetFileName(FilePath);
        Hash = fileInfos[1];
    }
}