using System.IO;
using Xunit;

namespace Jering.Javascript.NodeJS.Tests
{
    public class EmbeddedResourcesServiceUnitTests
    {
        private const string DUMMY_EMBEDDED_RESOURCE_NAME = "dummyEmbed"; // Name defined in .csproj (LogicalName attribute).
        private const string DUMMY_EMBEDDED_RESOURCE_CONTENT = "This file is embedded in the assembly and used to test the EmbeddedResourcesService.";

        [Fact]
        public void ReadAsString_AssemblyOverload_ReadsEmbeddedResourceAsString()
        {
            // Arrange
            var testSubject = new EmbeddedResourcesService();

            // Act
            string result = testSubject.ReadAsString(typeof(EmbeddedResourcesServiceUnitTests).Assembly, DUMMY_EMBEDDED_RESOURCE_NAME);

            // Assert
            Assert.Equal(DUMMY_EMBEDDED_RESOURCE_CONTENT, result);
        }

        [Fact]
        public void ReadAsString_TypeOverload_ReadsEmbeddedResourceAsString()
        {
            // Arrange
            var testSubject = new EmbeddedResourcesService();

            // Act
            string result = testSubject.ReadAsString(typeof(EmbeddedResourcesServiceUnitTests), DUMMY_EMBEDDED_RESOURCE_NAME);

            // Assert
            Assert.Equal(DUMMY_EMBEDDED_RESOURCE_CONTENT, result);
        }

        [Fact]
        public void ReadAsStream_AssemblyOverload_ReadsEmbeddedResourceAsStream()
        {
            // Arrange
            var testSubject = new EmbeddedResourcesService();

            // Act
            Stream result = testSubject.ReadAsStream(typeof(EmbeddedResourcesServiceUnitTests).Assembly, DUMMY_EMBEDDED_RESOURCE_NAME);

            // Assert
            using var streamReader = new StreamReader(result);
            Assert.Equal(DUMMY_EMBEDDED_RESOURCE_CONTENT, streamReader.ReadToEnd());
        }

        [Fact]
        public void ReadAsStream_TypeOverload_ReadsEmbeddedResourceAsStream()
        {
            // Arrange
            var testSubject = new EmbeddedResourcesService();

            // Act
            Stream result = testSubject.ReadAsStream(typeof(EmbeddedResourcesServiceUnitTests), DUMMY_EMBEDDED_RESOURCE_NAME);

            // Assert
            using var streamReader = new StreamReader(result);
            Assert.Equal(DUMMY_EMBEDDED_RESOURCE_CONTENT, streamReader.ReadToEnd());
        }
    }
}
