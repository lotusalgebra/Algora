using Algora.Application.Interfaces;
using Microsoft.AspNetCore.DataProtection;

namespace Algora.Infrastructure.Services;

/// <summary>
/// Service for encrypting and decrypting sensitive configuration values using ASP.NET Core Data Protection.
/// </summary>
public class EncryptionService : IEncryptionService
{
    private readonly IDataProtector _protector;

    private static readonly HashSet<string> SensitiveKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "AI:OpenAi:ApiKey",
        "AI:Anthropic:ApiKey",
        "AI:Gemini:ApiKey",
        "AI:StabilityAi:ApiKey",
        "ScraperApi:ApiKey",
        "WhatsApp:AccessToken",
        "WhatsApp:AppSecret"
    };

    public EncryptionService(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector("Algora.Settings.v1");
    }

    public string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return plainText;

        return _protector.Protect(plainText);
    }

    public string Decrypt(string cipherText)
    {
        if (string.IsNullOrEmpty(cipherText))
            return cipherText;

        try
        {
            return _protector.Unprotect(cipherText);
        }
        catch
        {
            // If decryption fails, assume it's already plaintext (migration scenario)
            return cipherText;
        }
    }

    public bool IsSensitiveKey(string key)
    {
        return SensitiveKeys.Contains(key);
    }

    public string MaskValue(string value, int visibleChars = 4)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        if (value.Length <= visibleChars * 2)
            return new string('*', value.Length);

        var prefix = value[..visibleChars];
        var suffix = value[^visibleChars..];
        var masked = new string('*', Math.Min(8, value.Length - visibleChars * 2));

        return $"{prefix}{masked}{suffix}";
    }
}
