using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace Jering.Javascript.NodeJS.Tests
{
    public class InvocationRequestUnitTests
    {
        private readonly MockRepository _mockRepository = new(MockBehavior.Default);

        [Fact]
        public void Constructor_ThrowsArgumentNullExceptionIfModuleSourceTypeIsStreamButModuleStreamSourceIsNull()
        {
            // Act and assert
            ArgumentNullException result = Assert.Throws<ArgumentNullException>(() => new InvocationRequest(ModuleSourceType.Stream));
        }

        [Theory]
        [MemberData(nameof(Constructor_ThrowsArgumentExceptionIfModuleSourceTypeIsFileOrStringButModuleSourceIsNullWhitespaceOrAnEmptyString_Data))]
        public void Constructor_ThrowsArgumentExceptionIfModuleSourceTypeIsFileOrStringButModuleSourceIsNullWhitespaceOrAnEmptyString(ModuleSourceType dummyModuleSourceType, string dummyModuleSource)
        {
            // Act and assert
            ArgumentException result = Assert.Throws<ArgumentException>(() => new InvocationRequest(dummyModuleSourceType, dummyModuleSource));
        }

        public static IEnumerable<object?[]> Constructor_ThrowsArgumentExceptionIfModuleSourceTypeIsFileOrStringButModuleSourceIsNullWhitespaceOrAnEmptyString_Data()
        {
            return new object?[][]
            {
                new object?[]{ModuleSourceType.File, null},
                new object?[]{ModuleSourceType.File, string.Empty},
                new object?[]{ModuleSourceType.File, " "},
                new object?[]{ModuleSourceType.String, null},
                new object?[]{ModuleSourceType.String, string.Empty},
                new object?[]{ModuleSourceType.String, " "}
            };
        }

        [Fact]
        public void Constructor_ThrowsArgumentNullExceptionIfModuleSourceTypeIsCacheButModuleSourceIsNull()
        {
            // Act and assert
            ArgumentNullException result = Assert.Throws<ArgumentNullException>(() => new InvocationRequest(ModuleSourceType.Cache));
        }

        [Fact]
        public void Constructor_CreatesInvocationRequestIfSuccessful()
        {
            // Arrange
            const string dummyModuleSource = "dummyModuleSource";
            const string dummyCacheIdentifier = "dummyCacheIdentifier";
            const string dummyExportName = "dummyExportName";
            object[] dummyArgs = Array.Empty<object>();
            const ModuleSourceType dummyModuleSourceType = ModuleSourceType.String;

            // Act
            var invocationRequest = new InvocationRequest(dummyModuleSourceType, dummyModuleSource, dummyCacheIdentifier, dummyExportName, dummyArgs);

            // Assert
            Assert.Equal(dummyModuleSourceType, invocationRequest.ModuleSourceType);
            Assert.Equal(dummyModuleSource, invocationRequest.ModuleSource);
            Assert.Equal(dummyCacheIdentifier, invocationRequest.CacheIdentifier);
            Assert.Equal(dummyExportName, invocationRequest.ExportName);
            Assert.Same(dummyArgs, invocationRequest.Args);
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
