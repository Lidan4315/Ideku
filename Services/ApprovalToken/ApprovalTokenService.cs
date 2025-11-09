using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace Ideku.Services.ApprovalToken
{
    public class ApprovalTokenService : IApprovalTokenService
    {
        private readonly string _encryptionKey;
        private readonly ILogger<ApprovalTokenService> _logger;

        public ApprovalTokenService(IConfiguration configuration, ILogger<ApprovalTokenService> logger)
        {
            _encryptionKey = configuration["ApprovalTokenSettings:EncryptionKey"] ?? "DefaultKey12345678901234567890123";
            _logger = logger;
        }

        public string GenerateToken(long ideaId, long approverId, string action, int stage)
        {
            try
            {
                var expiryDate = DateTime.Now.AddDays(7);
                var data = $"{ideaId}|{approverId}|{action}|{stage}|{expiryDate:yyyy-MM-dd HH:mm:ss}";

                var encrypted = EncryptString(data, _encryptionKey);
                return Convert.ToBase64String(Encoding.UTF8.GetBytes(encrypted))
                    .Replace("+", "-").Replace("/", "_").Replace("=", "");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating approval token");
                throw;
            }
        }

        public (bool IsValid, long IdeaId, long ApproverId, string Action, int Stage, string ErrorMessage) ValidateAndDecryptToken(string token)
        {
            try
            {
                var base64 = token.Replace("-", "+").Replace("_", "/");
                var padding = (4 - base64.Length % 4) % 4;
                base64 += new string('=', padding);

                var encryptedBytes = Convert.FromBase64String(base64);
                var encrypted = Encoding.UTF8.GetString(encryptedBytes);

                var decrypted = DecryptString(encrypted, _encryptionKey);
                var parts = decrypted.Split('|');

                if (parts.Length != 5)
                    return (false, 0, 0, string.Empty, 0, "Invalid token format");

                var ideaId = long.Parse(parts[0]);
                var approverId = long.Parse(parts[1]);
                var action = parts[2];
                var stage = int.Parse(parts[3]);
                var expiryDate = DateTime.Parse(parts[4]);

                if (expiryDate < DateTime.Now)
                    return (false, 0, 0, string.Empty, 0, "Token has expired");

                return (true, ideaId, approverId, action, stage, string.Empty);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating approval token");
                return (false, 0, 0, string.Empty, 0, "Invalid or corrupted token");
            }
        }

        private static string EncryptString(string plainText, string key)
        {
            var keyBytes = Encoding.UTF8.GetBytes(key.PadRight(32).Substring(0, 32));

            using var aes = Aes.Create();
            aes.Key = keyBytes;
            aes.GenerateIV();

            using var encryptor = aes.CreateEncryptor();
            var plainBytes = Encoding.UTF8.GetBytes(plainText);
            var encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

            var result = new byte[aes.IV.Length + encryptedBytes.Length];
            Array.Copy(aes.IV, 0, result, 0, aes.IV.Length);
            Array.Copy(encryptedBytes, 0, result, aes.IV.Length, encryptedBytes.Length);

            return Convert.ToBase64String(result);
        }

        private static string DecryptString(string cipherText, string key)
        {
            var keyBytes = Encoding.UTF8.GetBytes(key.PadRight(32).Substring(0, 32));
            var fullCipher = Convert.FromBase64String(cipherText);

            using var aes = Aes.Create();
            aes.Key = keyBytes;

            var iv = new byte[aes.IV.Length];
            var cipher = new byte[fullCipher.Length - iv.Length];

            Array.Copy(fullCipher, 0, iv, 0, iv.Length);
            Array.Copy(fullCipher, iv.Length, cipher, 0, cipher.Length);

            aes.IV = iv;

            using var decryptor = aes.CreateDecryptor();
            var decryptedBytes = decryptor.TransformFinalBlock(cipher, 0, cipher.Length);

            return Encoding.UTF8.GetString(decryptedBytes);
        }
    }
}
