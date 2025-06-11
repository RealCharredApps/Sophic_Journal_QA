using NUnit.Framework;

namespace MyNUnitTestProject.Tests.UnitTests
{
    [TestFixture]
    public class SampleTests
    {
        [Test]
        public void TestMethod1()
        {
            Assert.Pass("Test method 1 passed.");
        }

        [Test]
        public void TestMethod2()
        {
            Assert.Fail("Test method 2 failed.");
        }
    }
}