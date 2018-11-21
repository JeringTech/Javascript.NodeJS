using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using Xunit;

namespace Jering.Javascript.NodeJS.Tests
{
    public class InvocationExceptionUnitTests
    {
        [Fact]
        public void InvocationException_CanBeSerialized()
        {
            // Arrange
            const string dummyMessage = "dummyMessage";
            IFormatter dummyFormatter = new BinaryFormatter();
            var dummyStream = new MemoryStream();
            var testSubject = new InvocationException(dummyMessage);

            // Act
            dummyFormatter.Serialize(dummyStream, testSubject);
            dummyStream.Position = 0;
            var result = (InvocationException)dummyFormatter.Deserialize(dummyStream);

            // Assert
            Assert.Equal(dummyMessage, result.Message);
        }
    }
}
