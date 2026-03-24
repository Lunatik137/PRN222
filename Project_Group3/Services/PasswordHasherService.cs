using Microsoft.AspNetCore.Identity;

namespace Project_Group3.Services;
public interface IPasswordHasherService
{
    string HashPassword(string password);
    bool VerifyPassword(string storedPassword, string providedPassword);
    bool IsPasswordHashed(string password);
}
public sealed class PasswordHasherService : IPasswordHasherService
{
    private static readonly PasswordHasher<object> Hasher = new();

    public string HashPassword(string password)
        => Hasher.HashPassword(new object(), password);
    public bool VerifyPassword(string storedPassword, string providedPassword)
    {
        if (string.IsNullOrWhiteSpace(storedPassword) || string.IsNullOrEmpty(providedPassword))
        {
            return false;
        }

        try
        {
            var result = Hasher.VerifyHashedPassword(new object(), storedPassword, providedPassword);
            if (result == PasswordVerificationResult.Success || result == PasswordVerificationResult.SuccessRehashNeeded)
            {
                return true;
            }
        }
        catch (FormatException)
        {
            // If the old password is plain text, VerifyHashedPassword can throw.
            // We'll fall back to plain-text comparison for backward compatibility.
        }
        // Backward compatibility for existing plain-text records.
        return string.Equals(storedPassword, providedPassword, StringComparison.Ordinal);
    }

    public bool IsPasswordHashed(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            return false;
        }

        try
        {
            _ = Hasher.VerifyHashedPassword(new object(), password, "hash-format-probe");
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}