using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using Xunit;

namespace Jering.Javascript.NodeJS.Tests
{
    public class FileWatcherFactoryUnitTests
    {
        private readonly MockRepository _mockRepository = new(MockBehavior.Default);

        [Fact]
        public void Create_CreatesFileWatcher()
        {
            // Arrange
            const string dummyDirectoryPath = "dummyDirectoryPath";
            const string dummyResolvedDirectoryPath = "dummyResolvedDirectoryPath";
            const bool dummyIncludeSubdirectories = false;
            string[] dummyFileNamePatterns = Array.Empty<string>();
            var dummyNodeJSProcessOptions = new NodeJSProcessOptions();
            Mock<IOptions<NodeJSProcessOptions>> mockNodeJSProcessOptionsAccessor = _mockRepository.Create<IOptions<NodeJSProcessOptions>>();
            mockNodeJSProcessOptionsAccessor.Setup(n => n.Value).Returns(dummyNodeJSProcessOptions);
            Mock<FileWatcherFactory> mockFileWatcherFactory = CreateMockFileWatcherFactory(mockNodeJSProcessOptionsAccessor.Object);
            mockFileWatcherFactory.CallBase = true;
            mockFileWatcherFactory.
                Setup(f => f.ResolveDirectoryPath(dummyDirectoryPath, dummyNodeJSProcessOptions)).
                Returns(dummyResolvedDirectoryPath);
            mockFileWatcherFactory.Setup(f => f.ResolveFilters(dummyFileNamePatterns)).Returns(new ReadOnlyCollection<Regex>(new[] { new Regex("dummy") }));

            // Act
            IFileWatcher result = mockFileWatcherFactory.Object.Create(dummyDirectoryPath, dummyIncludeSubdirectories, dummyFileNamePatterns, (_) => { });

            // Assert
            _mockRepository.VerifyAll();
        }

        [Theory]
        [MemberData(nameof(ResolveDirectoryPath_ResolvesDirectoryPath_Data))]
        public void ResolveDirectoryPath_ResolvesDirectoryPath(string dummyDirectoryPath, string dummyProjectPath, string expectedResult)
        {
            // Arrange
            var dummyNodeJSProcessOptions = new NodeJSProcessOptions() { ProjectPath = dummyProjectPath };
            FileWatcherFactory testSubject = CreateFileWatcherFactory();

            // Act
            string result = testSubject.ResolveDirectoryPath(dummyDirectoryPath, dummyNodeJSProcessOptions);

            // Assert
            Assert.Equal(expectedResult, result);
        }

        public static IEnumerable<object?[]> ResolveDirectoryPath_ResolvesDirectoryPath_Data()
        {
            const string dummyDirectoryPath = "dummyDirectoryPath";
            const string dummyProjectPath = "dummyProjectPath";

            return new object?[][]
            {
                new object?[]{dummyDirectoryPath, dummyProjectPath, dummyDirectoryPath},
                // Project path if directory path is null, whitespace or an empty string
                new object?[]{null, dummyProjectPath, dummyProjectPath},
                new object?[]{" ", dummyProjectPath, dummyProjectPath},
                new object?[]{string.Empty, dummyProjectPath, dummyProjectPath}
            };
        }

        [Fact]
        public void ResolveFilters_ResolvesFilters()
        {
            // Arrange
            const string dummyFileNamePattern1 = "dummyFileNamePattern1";
            const string dummyFileNamePattern2 = "dummyFileNamePattern2";
            var dummyRegex1 = new Regex(".");
            var dummyRegex2 = new Regex(".");
            string[] dummyFileNamePatterns = new[] { dummyFileNamePattern1, dummyFileNamePattern2 };
            Mock<FileWatcherFactory> mockTestSubject = CreateMockFileWatcherFactory();
            mockTestSubject.CallBase = true;
            mockTestSubject.Setup(t => t.CreateRegex(dummyFileNamePattern1)).Returns(dummyRegex1);
            mockTestSubject.Setup(t => t.CreateRegex(dummyFileNamePattern2)).Returns(dummyRegex2);

            // Act
            ReadOnlyCollection<Regex> result1 = mockTestSubject.Object.ResolveFilters(dummyFileNamePatterns);
            ReadOnlyCollection<Regex> result2 = mockTestSubject.Object.ResolveFilters(dummyFileNamePatterns); // Call twice to verify caching of regex instances

            // Assert
            _mockRepository.VerifyAll();
            mockTestSubject.Verify(t => t.CreateRegex(dummyFileNamePattern1), Times.Once); // Regices get cached
            mockTestSubject.Verify(t => t.CreateRegex(dummyFileNamePattern2), Times.Once); // Regices get cached
            Assert.Same(dummyRegex1, result1[0]);
            Assert.Same(dummyRegex2, result1[1]);
            Assert.Equal(result1, result2);
        }

        [Theory]
        [MemberData(nameof(CreateRegex_CreatesRegex_Data))]
        public void CreateRegex_CreatesRegex(string dummyFileNamePattern, string expectedRegexPattern, string[] expectedMatches, string[] expectedNonMatches)
        {
            // Arrange
            FileWatcherFactory testSubject = CreateFileWatcherFactory();

            // Act
            Regex result = testSubject.CreateRegex(dummyFileNamePattern);

            // Assert
            Assert.Equal(result.Options, RegexOptions.Compiled | RegexOptions.IgnoreCase);
            Assert.Equal(expectedRegexPattern, result.ToString());
            foreach (string expectedMatch in expectedMatches)
            {
                Assert.Matches(result, expectedMatch);
            }
            foreach (string expectedNonMatch in expectedNonMatches)
            {
                Assert.DoesNotMatch(result, expectedNonMatch);
            }
        }

        public static IEnumerable<object[]> CreateRegex_CreatesRegex_Data()
        {
            return new object[][]
            {
                // * = 0 or more characters
                new object[]
                {
                    "*.js",
                    @"^.*\.js$",
                    new[]
                    {
                        "test.js", // Many characters
                        ".js" // 0 characters
                    },
                    new[]
                    {
                        "test.ts", // Different literal
                        "test.jsx" // Extends past literal
                    }
                },
                // ? = 0 or 1 character
                new object[]
                {
                    "?.js",
                    @"^.?\.js$",
                    new[]
                    {
                        "a.js", // 1 character
                        ".js" // 0 characters
                    },
                    new[]
                    {
                        "aa.js", // More than 1 characters (extends before pattern)
                        "a.ts", // Different literal
                        "a.jsx", // Extends past literal
                    }
                },
                // Escapes regex. This is not an exhaustive test of regex characters, just verification escaping occurs. We rely on Regex.Escape to do its job.
                new object[]
                {
                    "$+*.js",
                    @"^\$\+.*\.js$",
                    new[]{ "$+test.js" },
                    Array.Empty<string>()
                }
            };
        }

        private Mock<FileWatcherFactory> CreateMockFileWatcherFactory(IOptions<NodeJSProcessOptions>? nodeJSProcessOptionsAccessor = null)
        {
            return _mockRepository.Create<FileWatcherFactory>(nodeJSProcessOptionsAccessor ?? _mockRepository.Create<IOptions<NodeJSProcessOptions>>().Object);
        }

        private FileWatcherFactory CreateFileWatcherFactory(IOptions<NodeJSProcessOptions>? nodeJSProcessOptionsAccessor = null)
        {
            return new FileWatcherFactory(nodeJSProcessOptionsAccessor ?? _mockRepository.Create<IOptions<NodeJSProcessOptions>>().Object);
        }
    }
}
