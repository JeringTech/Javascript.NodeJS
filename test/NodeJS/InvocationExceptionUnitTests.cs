using System.Collections.Generic;
using Xunit;

namespace Jering.Javascript.NodeJS.Tests
{
    public class InvocationExceptionUnitTests
    {
        [Theory]
        [MemberData(nameof(Constructor_JoinsMessageAndStackParameters_Data))]
        public void Constructor_JoinsMessageAndStackParameters(string? dummyMessage, string? dummyStack, string expectedResult)
        {
            // Act
            var testSubject = new InvocationException(dummyMessage!, dummyStack); // Testing situation where user passes null despite nullable reference type warnings

            // Assert
            Assert.Equal(expectedResult, testSubject.Message);
        }

        public static IEnumerable<object?[]> Constructor_JoinsMessageAndStackParameters_Data()
        {
            string frameworkGeneratedMessage = new InvocationException().Message; // If no message is supplied to Exception, the framework generates a default

            return new object?[][]
            {
                new object?[]{ "dummyMessage", "dummyStack", "dummyMessage\ndummyStack" },
                new object?[]{ null, "dummyStack", "dummyStack" },
                new object?[]{ "dummyMessage", null, "dummyMessage" },
                new object?[]{ null, null, frameworkGeneratedMessage },
                new object?[]{ string.Empty, "dummyStack", "dummyStack" },
                new object?[]{ "dummyMessage", string.Empty, "dummyMessage" },
                new object?[]{ string.Empty, string.Empty, string.Empty }
            };
        }
    }
}
