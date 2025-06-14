using NUnit.Framework;
using Sophic.Services;

namespace MyNUnitTestProject.UnitTests
{
    [TestFixture]
    public class CategorySecurityTests
    {
        private CategoryService _categoryService;

        [SetUp]
        public void Setup()
        {
            _categoryService = new CategoryService(new LocalStorageService());
        }

        [Test]
        [TestCase("<script>alert('xss')</script>")]
        [TestCase("'; DROP TABLE Users; --")]
        [TestCase("../../../etc/passwd")]
        [TestCase("{{7*7}}")]  // Template injection
        public void AddCategory_WithMaliciousInput_ShouldReject(string maliciousInput)
        {
            var result = _categoryService.AddCategoryAsync(maliciousInput).Result;
            
            Assert.That(result, Is.False, 
                $"Category service should reject malicious input: {maliciousInput}");
        }

        [Test]
        public void AddCategory_ValidInput_ShouldSucceed()
        {
            var result = _categoryService.AddCategoryAsync("Valid Category").Result;
            Assert.That(result, Is.True);
        }
    }
}