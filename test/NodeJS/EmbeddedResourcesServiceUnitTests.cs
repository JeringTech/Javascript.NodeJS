using System.IO;
using Xunit;

namespace Jering.Javascript.NodeJS.Tests
{
    public class EmbeddedResourcesServiceUnitTests
    {
        private const string _dummyEmbeddedResourceName = "dummyEmbed"; // Name defined in .csproj (LogicalName attribute).
        private const string _dummyEmbeddedResourceContent = "This file is embedded in the assembly and used to test the EmbeddedResourcesService.";

        [Fact]
        public void ReadAsString_AssemblyOverload_ReadsEmbeddedResourceAsString()
        {
            // Arrange
            var testSubject = new EmbeddedResourcesService();

            // Act
            string result = testSubject.ReadAsString(typeof(EmbeddedResourcesServiceUnitTests).Assembly, _dummyEmbeddedResourceName);

            // Assert
            Assert.Equal(_dummyEmbeddedResourceContent, result);
        }

        [Fact]
        public void ReadAsString_TypeOverload_ReadsEmbeddedResourceAsString()
        {
            // Arrange
            var testSubject = new EmbeddedResourcesService();

            // Act
            string result = testSubject.ReadAsString(typeof(EmbeddedResourcesServiceUnitTests), _dummyEmbeddedResourceName);

            // Assert
            Assert.Equal(_dummyEmbeddedResourceContent, result);
        }

        [Fact]
        public void ReadAsStream_AssemblyOverload_ReadsEmbeddedResourceAsStream()
        {
            // Arrange
            var testSubject = new EmbeddedResourcesService();

            // Act
            Stream result = testSubject.ReadAsStream(typeof(EmbeddedResourcesServiceUnitTests).Assembly, _dummyEmbeddedResourceName);

            // Assert
            using var streamReader = new StreamReader(result);
            Assert.Equal(_dummyEmbeddedResourceContent, streamReader.ReadToEnd());
        }

        [Fact]
        public void ReadAsStream_TypeOverload_ReadsEmbeddedResourceAsStream()
        {
            // Arrange
            var testSubject = new EmbeddedResourcesService();

            // Act
            Stream result = testSubject.ReadAsStream(typeof(EmbeddedResourcesServiceUnitTests), _dummyEmbeddedResourceName);

            // Assert
            using var streamReader = new StreamReader(result);
            Assert.Equal(_dummyEmbeddedResourceContent, streamReader.ReadToEnd());
        }
    }
}
