using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace Jering.Javascript.NodeJS.Tests
{
    public class InvocationRequestUnitTests
    {
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
    }
}
