using NUnit.Framework;
using Sophic.Services;

namespace SecurityTests
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
        public void AddCategory_WithMaliciousInput_ShouldReject(string maliciousInput)
        {
            var result = _categoryService.AddCategoryAsync(maliciousInput).Result;
            
            Assert.That(result, Is.False, 
                $"Category service should reject malicious input: {maliciousInput}");
        }
    }
}