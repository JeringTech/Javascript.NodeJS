using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace Jering.Javascript.NodeJS.Tests
{
    public class InvocationRequestUnitTests
    {
        private readonly MockRepository _mockRepository = new MockRepository(MockBehavior.Default);

        [Fact]
        public void Constructor_ThrowsArgumentExceptionIfModuleSourceTypeIsStreamButModuleStreamSourceIsNull()
        {
            // Act and assert
            ArgumentException result = Assert.Throws<ArgumentException>(() => new InvocationRequest(ModuleSourceType.Stream));
            Assert.Equal(Strings.ArgumentException_InvocationRequest_ModuleStreamSourceCannotBeNull + "\nParameter name: moduleStreamSource", result.Message, ignoreLineEndingDifferences: true);
        }

        [Theory]
        [MemberData(nameof(Constructor_ThrowsArgumentExceptionIfModuleSourceTypeIsFileOrStringButModuleSourceIsNullWhitespaceOrAnEmptyString_Data))]
        public void Constructor_ThrowsArgumentExceptionIfModuleSourceTypeIsFileOrStringButModuleSourceIsNullWhitespaceOrAnEmptyString(ModuleSourceType dummyModuleSourceType, string dummyModuleSource)
        {
            // Act and assert
            ArgumentException result = Assert.Throws<ArgumentException>(() => new InvocationRequest(dummyModuleSourceType, dummyModuleSource));
            Assert.Equal(Strings.ArgumentException_InvocationRequest_ModuleSourceCannotBeNullWhitespaceOrAnEmptyString + "\nParameter name: moduleSource", result.Message, ignoreLineEndingDifferences: true);
        }

        public static IEnumerable<object[]> Constructor_ThrowsArgumentExceptionIfModuleSourceTypeIsFileOrStringButModuleSourceIsNullWhitespaceOrAnEmptyString_Data()
        {
            return new object[][]
            {
                new object[]{ModuleSourceType.File, null},
                new object[]{ModuleSourceType.File, string.Empty},
                new object[]{ModuleSourceType.File, " "},
                new object[]{ModuleSourceType.String, null},
                new object[]{ModuleSourceType.String, string.Empty},
                new object[]{ModuleSourceType.String, " "}
            };
        }

        [Fact]
        public void Constructor_ThrowsArgumentExceptionIfModuleSourceTypeIsCacheButModuleSourceIsNull()
        {
            // Act and assert
            ArgumentException result = Assert.Throws<ArgumentException>(() => new InvocationRequest(ModuleSourceType.Cache));
            Assert.Equal(Strings.ArgumentException_InvocationRequest_ModuleSourceCannotBeNull + "\nParameter name: moduleSource", result.Message, ignoreLineEndingDifferences: true);
        }

        [Fact]
        public void Constructor_CreatesInvocationRequestIfSuccessful()
        {
            // Arrange
            const string dummyModuleSource = "dummyModuleSource";
            const string dummyNewCacheIdentifier = "dummyNewCacheIdentifier";
            const string dummyExportName = "dummyExportName";
            var dummyArgs = new object[0];
            Stream dummyModuleStreamSource = new MemoryStream();
            const ModuleSourceType dummyModuleSourceType = ModuleSourceType.String;

            // Act
            var invocationRequest = new InvocationRequest(dummyModuleSourceType, dummyModuleSource, dummyNewCacheIdentifier, dummyExportName, dummyArgs, dummyModuleStreamSource);

            // Assert
            Assert.Equal(dummyModuleSourceType, invocationRequest.ModuleSourceType);
            Assert.Equal(dummyModuleSource, invocationRequest.ModuleSource);
            Assert.Equal(dummyNewCacheIdentifier, invocationRequest.NewCacheIdentifier);
            Assert.Equal(dummyExportName, invocationRequest.ExportName);
            Assert.Same(dummyArgs, invocationRequest.Args);
            Assert.Equal(dummyModuleStreamSource, invocationRequest.ModuleStreamSource);
        }

        [Fact]
        public void ResetStreamPosition_ThrowsInvalidOperationExceptionIfModuleStreamSourceIsNull()
        {
            // Arrange
            var testSubject = new InvocationRequest(ModuleSourceType.String, "dummyModuleSource");

            // Act and assert
            InvalidOperationException result = Assert.Throws<InvalidOperationException>(() => testSubject.ResetStreamPosition());
            Assert.Equal(Strings.InvalidOperationException_InvocationRequest_StreamIsNull, result.Message);
        }

        [Fact]
        public void ResetStreamPosition_ThrowsInvalidOperationExceptionIfModuleStreamSourceIsUnseekable()
        {
            // Arrange
            Mock<Stream> mockStream = _mockRepository.Create<Stream>();
            mockStream.Setup(s => s.CanSeek).Returns(false);
            var testSubject = new InvocationRequest(ModuleSourceType.Stream, moduleStreamSource: mockStream.Object);

            // Act and assert
            InvalidOperationException result = Assert.Throws<InvalidOperationException>(() => testSubject.ResetStreamPosition());
            _mockRepository.VerifyAll();
            Assert.Equal(Strings.InvalidOperationException_InvocationRequest_StreamIsUnseekable, result.Message);
        }

        [Fact]
        public void ResetStreamPosition_ResetsModuleStreamSourcePosition()
        {
            // Arrange
            const int dummyInitialPosition = 1;
            Mock<Stream> mockStream = _mockRepository.Create<Stream>();
            mockStream.Setup(s => s.Position).Returns(dummyInitialPosition); // Constructor saves initial position
            mockStream.Setup(s => s.CanSeek).Returns(true);
            var testSubject = new InvocationRequest(ModuleSourceType.Stream, moduleStreamSource: mockStream.Object);

            // Act
            testSubject.ResetStreamPosition();

            // Assert
            _mockRepository.VerifyAll();
            mockStream.VerifySet(s => s.Position = dummyInitialPosition);
        }

        [Fact]
        public void CheckStreamAtInitialPosition_ThrowsInvalidOperationExceptionIfModuleStreamSourceIsNull()
        {
            // Arrange
            var testSubject = new InvocationRequest(ModuleSourceType.String, "dummyModuleSource");

            // Act and assert
            InvalidOperationException result = Assert.Throws<InvalidOperationException>(() => testSubject.CheckStreamAtInitialPosition());
            Assert.Equal(Strings.InvalidOperationException_InvocationRequest_StreamIsNull, result.Message);
        }

        [Fact]
        public void CheckStreamAtInitialPosition_ThrowsInvalidOperationExceptionIfModuleStreamSourceIsUnseekable()
        {
            // Arrange
            Mock<Stream> mockStream = _mockRepository.Create<Stream>();
            mockStream.Setup(s => s.CanSeek).Returns(false);
            var testSubject = new InvocationRequest(ModuleSourceType.Stream, moduleStreamSource: mockStream.Object);

            // Act and assert
            InvalidOperationException result = Assert.Throws<InvalidOperationException>(() => testSubject.CheckStreamAtInitialPosition());
            _mockRepository.VerifyAll();
            Assert.Equal(Strings.InvalidOperationException_InvocationRequest_StreamIsUnseekable, result.Message);
        }

        [Fact]
        public void CheckStreamAtInitialPosition_ReturnsTrueIfModuleStreamSourceIsAtInitialPosition()
        {
            // Arrange
            Mock<Stream> mockStream = _mockRepository.Create<Stream>();
            mockStream.SetupSequence(s => s.Position).
                Returns(1).
                Returns(1); // Return same value when caching initial position and when comparing
            mockStream.Setup(s => s.CanSeek).Returns(true);
            var testSubject = new InvocationRequest(ModuleSourceType.Stream, moduleStreamSource: mockStream.Object);

            // Act
            bool result = testSubject.CheckStreamAtInitialPosition();

            // Assert
            _mockRepository.VerifyAll();
            Assert.True(result);
        }

        [Fact]
        public void CheckStreamAtInitialPosition_ReturnsFalseIfModuleStreamSourceIsNotAtInitialPosition()
        {
            // Arrange
            Mock<Stream> mockStream = _mockRepository.Create<Stream>();
            mockStream.SetupSequence(s => s.Position).
                Returns(1).
                Returns(2); // Return different values when caching initial position and when comparing
            mockStream.Setup(s => s.CanSeek).Returns(true);
            var testSubject = new InvocationRequest(ModuleSourceType.Stream, moduleStreamSource: mockStream.Object);

            // Act
            bool result = testSubject.CheckStreamAtInitialPosition();

            // Assert
            _mockRepository.VerifyAll();
            Assert.False(result);
        }
    }
}
