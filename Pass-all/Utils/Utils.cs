using System;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace Passall.Utils;

public static class Utils
{
    // AES 256 bits key (exactly 32 characters)
    private const string AesKey = "PassAllSecretKey1234567890123456";

    /// <summary>
    /// Encrypts the given string using AES and returns the encrypted string
    /// </summary>
    /// <param name="input">String to encrypt</param>
    /// <returns>Encrypted string, null if error</returns>
    public static string? Encrypt(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return null;

        byte[] iv = new byte[16];
        byte[] result;

        using (Aes aes = Aes.Create())
        {
            aes.Key = Encoding.UTF8.GetBytes(AesKey);
            aes.IV = iv;

            ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

            using (MemoryStream memoryStream = new MemoryStream())
            {
                using (CryptoStream cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                {
                    using (StreamWriter streamWriter = new StreamWriter(cryptoStream))
                    {
                        streamWriter.Write(input);
                    }
                    result = memoryStream.ToArray();
                }
            }
        }

        string output = Convert.ToBase64String(result);
        return string.IsNullOrEmpty(output) ? null : output;
    }

    /// <summary>
    /// Decrypts the given AES encrypted string and returns the decrypted string
    /// </summary>
    /// <param name="input">String to decrypt</param>
    /// <returns>Decrypted string, null if error</returns>
    public static string? Decrypt(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return null;

        byte[] iv = new byte[16];
        byte[] buffer = Convert.FromBase64String(input);
        string? output;

        using (Aes aes = Aes.Create())
        {
            aes.Key = Encoding.UTF8.GetBytes(AesKey);
            aes.IV = iv;

            ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

            using (MemoryStream memoryStream = new MemoryStream(buffer))
            {
                using (CryptoStream cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
                {
                    using (StreamReader streamReader = new StreamReader(cryptoStream))
                    {
                        output = streamReader.ReadToEnd();
                    }
                }
            }
        }

        return string.IsNullOrEmpty(output) ? null : output;
    }

    /// <summary>
    /// Hashes the given string using SHA256 and returns the hash as a hex string
    /// </summary>
    /// <param name="input">String to hash</param>
    /// <returns>SHA256 hash as lowercase hex string, null if error</returns>
    public static string? Hash(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return null;

        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}

public class FirstLetterConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is string s && s.Length > 0 ? s[0].ToString().ToUpperInvariant() : "?";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public class StringToSolidColorBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string colorStr)
        {
            try { return new SolidColorBrush(Color.Parse(colorStr)); }
            catch { }
        }
        return Brushes.Transparent;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
