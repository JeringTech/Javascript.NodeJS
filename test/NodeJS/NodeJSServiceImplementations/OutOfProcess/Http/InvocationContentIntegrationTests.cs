using Moq;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Jering.Javascript.NodeJS.Tests
{
    public class InvocationContentIntegrationTests
    {
        private readonly MockRepository _mockRepository = new(MockBehavior.Default);

        [Theory]
        [MemberData(nameof(SerializeToStreamAsync_SerializesNonModuleSourceTypeStreamInvocationRequestsAsync_Data))]
        public async Task SerializeToStreamAsync_SerializesNonModuleSourceTypeStreamInvocationRequestsAsync(ModuleSourceType dummyModuleSourceType)
        {
            // Arrange
            const string dummyModuleSource = "dummyModuleSource";
            var dummyInvocationRequest = new InvocationRequest(dummyModuleSourceType, dummyModuleSource);
            var dummyJsonService = new JsonService();
            using ExposedInvocationContent testSubject = CreateInvocationContent(dummyJsonService, dummyInvocationRequest);
            var resultMemoryStream = new MemoryStream();

            // Act
            await testSubject.ExposedSerializeToStreamAsync(resultMemoryStream, null).ConfigureAwait(false);

            // Assert
            resultMemoryStream.Position = 0;
            using var resultReader = new StreamReader(resultMemoryStream);
            string result = resultReader.ReadToEnd();
            Assert.Equal($"{{\"moduleSourceType\":{(int)dummyModuleSourceType},\"moduleSource\":\"{dummyModuleSource}\"}}", result);
        }

        public static IEnumerable<object[]> SerializeToStreamAsync_SerializesNonModuleSourceTypeStreamInvocationRequestsAsync_Data()
        {
            return new object[][]
            {
                new object[]{ModuleSourceType.String},
                new object[]{ModuleSourceType.File},
                new object[]{ModuleSourceType.Cache},
            };
        }

        [Fact]
        public async Task SerializeToStreamAsync_SerializesModuleSourceTypeStreamInvocationRequestsAsync()
        {
            // Arrange
            const ModuleSourceType dummyModuleSourceType = ModuleSourceType.Stream;
            const string dummyModuleSource = "dummyModuleSource";
            var dummyModuleStreamSource = new MemoryStream(Encoding.UTF8.GetBytes(dummyModuleSource));
            var dummyInvocationRequest = new InvocationRequest(dummyModuleSourceType, moduleStreamSource: dummyModuleStreamSource);
            var dummyJsonService = new JsonService();
            using ExposedInvocationContent testSubject = CreateInvocationContent(dummyJsonService, dummyInvocationRequest);
            var resultMemoryStream = new MemoryStream();

            // Act
            await testSubject.ExposedSerializeToStreamAsync(resultMemoryStream, null).ConfigureAwait(false);

            // Assert
            resultMemoryStream.Position = 0;
            using var resultReader = new StreamReader(resultMemoryStream);
            string result = resultReader.ReadToEnd();
#if NET461
            Assert.Equal($"{{\"moduleSourceType\":{(int)dummyModuleSourceType}}}{Encoding.UTF8.GetString(InvocationContent._boundaryBytes)}{dummyModuleSource}", result);
#else
            Assert.Equal($"{{\"moduleSourceType\":{(int)dummyModuleSourceType}}}{Encoding.UTF8.GetString(InvocationContent._boundaryBytes.Span)}{dummyModuleSource}", result);
#endif
        }

        [Theory]
        [MemberData(nameof(Constructor_SetsContentTypeDependingOnModuleSourceType_Data))]
        public void Constructor_SetsContentTypeDependingOnModuleSourceType(InvocationRequest dummyInvocationRequest, string expectedMediaType)
        {
            // Act
            using var result = new InvocationContent(_mockRepository.Create<IJsonService>().Object, dummyInvocationRequest);

            // Assert
            Assert.Equal(expectedMediaType, result.Headers.ContentType?.MediaType);
            _mockRepository.VerifyAll(); // No calls should have been made on IJsonService
        }

        public static IEnumerable<object?[]> Constructor_SetsContentTypeDependingOnModuleSourceType_Data()
        {
            return new object?[][]{
                new object?[]{
                    new InvocationRequest(ModuleSourceType.Cache, "dummyModuleSource"),
                    null
                },
                new object?[]{
                    new InvocationRequest(ModuleSourceType.File, "dummyModuleSource"),
                    null
                },
                new object?[]{
                    new InvocationRequest(ModuleSourceType.String, "dummyModuleSource"),
                    null
                },
                new object?[]{
                    new InvocationRequest(ModuleSourceType.Stream, moduleStreamSource: new MemoryStream()),
                    "multipart/mixed"
                }
            };
        }

        private static ExposedInvocationContent CreateInvocationContent(IJsonService jsonService, InvocationRequest invocationRequest)
        {
            return new ExposedInvocationContent(jsonService, invocationRequest);
        }

        private class ExposedInvocationContent : InvocationContent
        {
            public ExposedInvocationContent(IJsonService jsonService,
                InvocationRequest invocationRequest) :
                base(jsonService, invocationRequest)
            {
            }

            public async Task ExposedSerializeToStreamAsync(Stream stream, TransportContext? context)
            {
                await SerializeToStreamAsync(stream, context).ConfigureAwait(false);
            }
        }
    }
}
