using NUnit.Framework;
using Sophic.Services;
using System;
using System.Security.Cryptography;
using System.Text;

namespace SecurityTests
{
    [TestFixture]
    public class EncryptionSecurityTests
    {
        private readonly string _testPassword = "TestPassword123!";

        [Test]
        public void EncryptSameData_TwiceInRow_ProducesDifferentCiphertext()
        {
            var plaintext = "Sensitive journal entry data";
            
            var encrypted1 = EncryptionService.Encrypt(plaintext, _testPassword);
            var encrypted2 = EncryptionService.Encrypt(plaintext, _testPassword);
            
            Assert.That(encrypted1, Is.Not.EqualTo(encrypted2), 
                "Same plaintext should produce different ciphertext (proper IV randomization)");
        }

        [Test]
        public void EncryptDecrypt_RoundTrip_ReturnsOriginalData()
        {
            var originalText = "Test journal entry with special chars: àáâãäåæçèé!@#$%^&*()";
            
            var encrypted = EncryptionService.Encrypt(originalText, _testPassword);
            var decrypted = EncryptionService.Decrypt(encrypted, _testPassword);
            
            Assert.That(decrypted, Is.EqualTo(originalText), 
                "Decryption should perfectly restore original data");
        }

        [Test]
        public void Encrypt_EmptyString_DoesNotThrowException()
        {
            Assert.DoesNotThrow(() => EncryptionService.Encrypt("", _testPassword), 
                "Encryption should handle empty strings gracefully");
        }

        [Test]
        public void Decrypt_WrongPassword_ShouldFailGracefully()
        {
            var originalText = "Secret journal entry";
            var encrypted = EncryptionService.Encrypt(originalText, _testPassword);
            
            // FIXED: Expect CryptographicException specifically
            Assert.Throws<CryptographicException>(() => EncryptionService.Decrypt(encrypted, "WrongPassword123!"), 
                "Decryption with wrong password should fail");
        }

        [Test]
        public void Decrypt_TamperedData_ShouldFailGracefully()
        {
            var originalText = "Original journal entry";
            var encrypted = EncryptionService.Encrypt(originalText, _testPassword);
            
            // FIXED: Tamper with actual encrypted bytes, not base64 string
            var encryptedBytes = Convert.FromBase64String(encrypted);
            
            // Flip some bits in the encrypted data (not the base64)
            if (encryptedBytes.Length > 10)
            {
                encryptedBytes[encryptedBytes.Length - 5] ^= 0xFF; // Tamper with HMAC
            }
            
            var tamperedData = Convert.ToBase64String(encryptedBytes);
            
            // FIXED: Expect CryptographicException specifically  
            Assert.Throws<CryptographicException>(() => EncryptionService.Decrypt(tamperedData, _testPassword), 
                "Decryption should detect and reject tampered data");
        }
    }
}