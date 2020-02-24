using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using Xunit;

namespace Jering.Javascript.NodeJS.Tests
{
    public class ConnectionExceptionUnitTests
    {
        [Fact]
        public void ConnectionException_CanBeSerialized()
        {
            // Arrange
            const string dummyMessage = "dummyMessage";
            IFormatter dummyFormatter = new BinaryFormatter();
            var dummyStream = new MemoryStream();
            var testSubject = new ConnectionException(dummyMessage);

            // Act
            dummyFormatter.Serialize(dummyStream, testSubject);
            dummyStream.Position = 0;
            var result = (ConnectionException)dummyFormatter.Deserialize(dummyStream);

            // Assert
            Assert.Equal(dummyMessage, result.Message);
        }
    }
}
