namespace Algora.Application.Interfaces;

/// <summary>
/// Service for encrypting and decrypting sensitive configuration values.
/// </summary>
public interface IEncryptionService
{
    /// <summary>
    /// Encrypts a plain text value.
    /// </summary>
    string Encrypt(string plainText);

    /// <summary>
    /// Decrypts an encrypted value.
    /// </summary>
    string Decrypt(string cipherText);

    /// <summary>
    /// Checks if a configuration key contains sensitive data that should be encrypted.
    /// </summary>
    bool IsSensitiveKey(string key);

    /// <summary>
    /// Masks a sensitive value for display (e.g., "sk-abc...xyz" or "********").
    /// </summary>
    string MaskValue(string value, int visibleChars = 4);
}
