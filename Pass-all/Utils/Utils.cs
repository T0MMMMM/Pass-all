using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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
    public static string? OldEncrypt(string? input)
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
    public static string? OldDecrypt(string? input)
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
    public static string? OldHash(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return null;

        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}

public static class Logger
{
    private static readonly string LogPath = Path.Combine(AppContext.BaseDirectory, "passall.log");

    public static void Log(Exception ex)
    {
        try
        {
            var entry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {ex.GetType().Name}: {ex.Message}{Environment.NewLine}{ex.StackTrace}{Environment.NewLine}";
            File.AppendAllText(LogPath, entry + Environment.NewLine);
        }
        catch { }
    }
}

public class FirstLetterConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is string s && s.Length > 0 ? s[0].ToString().ToUpperInvariant() : "?";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public static class PasswordGenerator
{
    private const string Specials = "!@#$%&*?";

    public static string? GenerateFromWords(IReadOnlyList<string> words)
    {
        if (words.Count == 0) return null;
        var rng = Random.Shared;
        var w1 = words[rng.Next(words.Count)];
        var w2 = words[rng.Next(words.Count)];
        var w3 = words[rng.Next(words.Count)];
        return $"{w1}{w2}{w3}{Specials[rng.Next(Specials.Length)]}";
    }
}

public enum StrengthLevel { None, Weak, Medium, Good, Strong }   // 0..4

public readonly record struct StrengthResult(StrengthLevel Level, string Label, string Color);

public static class PasswordStrength
{
    public const string OffColor = "#22FFFFFF";

    /// <summary>
    /// Évalue la robustesse d'un mot de passe via une heuristique simple
    /// combinant la longueur et le nombre de classes de caractères présentes
    /// (minuscules, majuscules, chiffres, symboles).
    /// </summary>
    public static StrengthResult Evaluate(string? password)
    {
        if (string.IsNullOrEmpty(password))
            return new StrengthResult(StrengthLevel.None, string.Empty, OffColor);

        var classes = 0;
        if (password.Any(char.IsLower)) classes++;
        if (password.Any(char.IsUpper)) classes++;
        if (password.Any(char.IsDigit)) classes++;
        if (password.Any(c => !char.IsLetterOrDigit(c))) classes++;

        var len = password.Length;

        StrengthLevel level;
        if (len < 8)
            level = StrengthLevel.Weak;
        else if (len <= 11)
            level = classes >= 2 ? StrengthLevel.Medium : StrengthLevel.Weak;
        else if (len <= 15)
            level = classes >= 3 ? StrengthLevel.Good : StrengthLevel.Medium;
        else
            level = classes >= 4 ? StrengthLevel.Strong : StrengthLevel.Good;

        return level switch
        {
            StrengthLevel.Weak   => new StrengthResult(level, "Faible", "#E5534B"),
            StrengthLevel.Medium => new StrengthResult(level, "Moyen",  "#E0A030"),
            StrengthLevel.Good   => new StrengthResult(level, "Bon",    "#3DA5D9"),
            StrengthLevel.Strong => new StrengthResult(level, "Fort",   "#2FBF71"),
            _ => new StrengthResult(StrengthLevel.None, string.Empty, OffColor),
        };
    }
}

public class BreachCountConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not int n) return string.Empty;
        var s = n.ToString();
        var sb = new StringBuilder();
        for (var i = 0; i < s.Length; i++)
        {
            if (i > 0 && (s.Length - i) % 3 == 0)
                sb.Append(' ');
            sb.Append(s[i]);
        }
        return sb.ToString();
    }

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
            catch (Exception ex) { Logger.Log(ex); }
        }
        return Brushes.Transparent;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
