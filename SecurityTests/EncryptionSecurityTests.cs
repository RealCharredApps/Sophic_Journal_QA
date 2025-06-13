using NUnit.Framework;
using Sophic.Services;
using System.Text;

namespace SecurityTests
{
    [TestFixture]
    public class EncryptionSecurityTests
    {
        private EncryptionService _encryptionService;
        private readonly string _testPassword = "TestPassword123!";

        [SetUp]
        public void Setup()
        {
            _encryptionService = new EncryptionService();
        }

        [Test]
        public void EncryptSameData_TwiceInRow_ProducesDifferentCiphertext()
        {
            var plaintext = "Sensitive journal entry data";
            
            var encrypted1 = _encryptionService.Encrypt(plaintext, _testPassword);
            var encrypted2 = _encryptionService.Encrypt(plaintext, _testPassword);
            
            Assert.That(encrypted1, Is.Not.EqualTo(encrypted2), 
                "Same plaintext should produce different ciphertext (proper IV randomization)");
        }

        [Test]
        public void EncryptDecrypt_RoundTrip_ReturnsOriginalData()
        {
            var originalText = "Test journal entry with special chars: àáâãäåæçèé!@#$%^&*()";
            
            var encrypted = _encryptionService.Encrypt(originalText, _testPassword);
            var decrypted = _encryptionService.Decrypt(encrypted, _testPassword);
            
            Assert.That(decrypted, Is.EqualTo(originalText), 
                "Decryption should perfectly restore original data");
        }

        [Test]
        public void Encrypt_EmptyString_DoesNotThrowException()
        {
            Assert.DoesNotThrow(() => _encryptionService.Encrypt("", _testPassword), 
                "Encryption should handle empty strings gracefully");
        }

        [Test]
        public void Decrypt_WrongPassword_ShouldFailGracefully()
        {
            var originalText = "Secret journal entry";
            var encrypted = _encryptionService.Encrypt(originalText, _testPassword);
            
            Assert.Throws<Exception>(() => _encryptionService.Decrypt(encrypted, "WrongPassword123!"), 
                "Decryption with wrong password should fail");
        }

        [Test]
        public void Decrypt_TamperedData_ShouldFailGracefully()
        {
            var originalText = "Original journal entry";
            var encrypted = _encryptionService.Encrypt(originalText, _testPassword);
            
            // Tamper with the encrypted data
            var tamperedData = encrypted.Substring(0, encrypted.Length - 10) + "TAMPERED!!";
            
            Assert.Throws<Exception>(() => _encryptionService.Decrypt(tamperedData, _testPassword), 
                "Decryption should detect and reject tampered data");
        }
    }
}