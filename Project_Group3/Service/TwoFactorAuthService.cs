using Google.Authenticator;
using PRN222_Group3.Repository;
using PRN222_Group3.Models;
using System.Security.Cryptography;
using System.Text;

namespace PRN222_Group3.Service
{
    public interface ITwoFactorAuthService
    {
        (string secretKey, string qrCodeImageUrl, string manualEntryKey) GenerateSetupCode(string userEmail);
        bool ValidateTwoFactorPIN(string secretKey, string userPin);
        Task<bool> EnableTwoFactorAsync(int userId, string secretKey, string verificationCode);
        Task<bool> DisableTwoFactorAsync(int userId, string verificationCode);
        List<string> GenerateRecoveryCodes(int count = 10);
        Task<bool> ValidateRecoveryCodeAsync(int userId, string recoveryCode);
    }

    public class TwoFactorAuthService : ITwoFactorAuthService
    {
        private const string Issuer = "PRN222_Group3";
        private readonly UserRepository _userRepository;

        public TwoFactorAuthService(UserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public (string secretKey, string qrCodeImageUrl, string manualEntryKey) GenerateSetupCode(string userEmail)
        {
            var authenticator = new TwoFactorAuthenticator();
            var secretKey = Guid.NewGuid().ToString().Replace("-", "").Substring(0, 16).ToUpper();

            var setupInfo = authenticator.GenerateSetupCode(Issuer, userEmail, secretKey, false, 3);

            return (secretKey, setupInfo.QrCodeSetupImageUrl, setupInfo.ManualEntryKey);
        }

        public bool ValidateTwoFactorPIN(string secretKey, string userPin)
        {
            var authenticator = new TwoFactorAuthenticator();
            return authenticator.ValidateTwoFactorPIN(secretKey, userPin);
        }

        public List<string> GenerateRecoveryCodes(int count = 10)
        {
            var recoveryCodes = new List<string>();
            using (var rng = RandomNumberGenerator.Create())
            {
                for (int i = 0; i < count; i++)
                {
                    var bytes = new byte[4];
                    rng.GetBytes(bytes);
                    var code = BitConverter.ToUInt32(bytes, 0).ToString("D8");
                    recoveryCodes.Add(code);
                }
            }
            return recoveryCodes;
        }

        public async Task<bool> EnableTwoFactorAsync(int userId, string secretKey, string verificationCode)
        {
            // Verify the code before enabling
            if (!ValidateTwoFactorPIN(secretKey, verificationCode))
            {
                return false;
            }

            // Generate recovery codes
            var recoveryCodes = GenerateRecoveryCodes();
            var recoveryCodesString = string.Join(",", recoveryCodes);

            return await _userRepository.EnableTwoFactorAsync(userId, secretKey, recoveryCodesString);
        }

        public async Task<bool> DisableTwoFactorAsync(int userId, string verificationCode)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null || string.IsNullOrEmpty(user.TwoFactorSecret))
            {
                return false;
            }

            // Verify the 2FA code before disabling
            if (!ValidateTwoFactorPIN(user.TwoFactorSecret, verificationCode))
            {
                return false;
            }

            return await _userRepository.DisableTwoFactorAsync(userId);
        }

        public async Task<bool> ValidateRecoveryCodeAsync(int userId, string recoveryCode)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null || string.IsNullOrEmpty(user.TwoFactorRecoveryCodes))
            {
                return false;
            }

            var codes = user.TwoFactorRecoveryCodes.Split(',').ToList();
            if (!codes.Contains(recoveryCode))
            {
                return false;
            }

            // Remove used recovery code
            codes.Remove(recoveryCode);
            var updatedCodes = string.Join(",", codes);
            await _userRepository.UpdateRecoveryCodesAsync(userId, updatedCodes);

            return true;
        }
    }
}