using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Jering.Javascript.NodeJS.Tests
{
    public sealed class FileWatcherServiceUnitTests
    {
        private readonly MockRepository _mockRepository = new(MockBehavior.Default);

        [Fact]
        public async Task AddFileChangedListenerAsync_AddsFileChangedListener()
        {
            // Arrange
            Mock<FileWatcherService> mockTestSubject = CreateMockFileWatcherService();
            mockTestSubject.CallBase = true;
            mockTestSubject.Setup(t => t.CreateFileSystemWatcher()).Returns((FileSystemWatcher)null!);
            bool resultFileChangedListenerCalled = false;
            void dummyAction() { resultFileChangedListenerCalled = true; }
            FileWatcherService testSubject = mockTestSubject.Object;

            // Act
            await testSubject.AddFileChangedListenerAsync(dummyAction).ConfigureAwait(false);

            // Assert
            _mockRepository.VerifyAll();
            await testSubject.InternalFileHandlerCoreAsync("dummyPath").ConfigureAwait(false);
            Assert.True(resultFileChangedListenerCalled);

        }

        [Fact]
        public void CreateFileSystemWatcher_CreatesFileSystemWatcher()
        {
            // Arrange
            const bool dummyIncludeSubdirectories = true;
            string[] dummyWatchFileNamePattern = new string[] { "dummyWatchFileNamePattern" };
            const string dummyWatchPath = "dummyWatchPath";
            var dummyOutOfProcessNodeJSServiceOptions = new OutOfProcessNodeJSServiceOptions()
            {
                WatchSubdirectories = dummyIncludeSubdirectories,
                WatchFileNamePatterns = dummyWatchFileNamePattern,
                WatchPath = dummyWatchPath
            };
            Mock<IOptions<OutOfProcessNodeJSServiceOptions>> mockOutOfProcessNodeJSServiceOptionsAccessor = _mockRepository.Create<IOptions<OutOfProcessNodeJSServiceOptions>>();
            mockOutOfProcessNodeJSServiceOptionsAccessor.Setup(o => o.Value).Returns(dummyOutOfProcessNodeJSServiceOptions);
            var dummyNodeJSProcessOptions = new NodeJSProcessOptions();
            Mock<IOptions<NodeJSProcessOptions>> mockNodeJSProcessOptionsAccessor = _mockRepository.Create<IOptions<NodeJSProcessOptions>>();
            mockNodeJSProcessOptionsAccessor.Setup(n => n.Value).Returns(dummyNodeJSProcessOptions);
            string dummyDirectoryPath = Directory.GetCurrentDirectory(); // FileSystemWatcher constructor requires an existing path
            Mock<FileWatcherService> mockTestSubject = CreateMockFileWatcherService(mockOutOfProcessNodeJSServiceOptionsAccessor.Object,
                mockNodeJSProcessOptionsAccessor.Object);
            mockTestSubject.CallBase = true;
            mockTestSubject.Setup(t => t.ResolveFilters(dummyWatchFileNamePattern)).Returns((ReadOnlyCollection<Regex>)null!);
            mockTestSubject.Setup(t => t.ResolveDirectoryPath(dummyWatchPath, dummyNodeJSProcessOptions.ProjectPath)).Returns(dummyDirectoryPath);

            // Act
            FileSystemWatcher result = mockTestSubject.Object.CreateFileSystemWatcher();

            // Assert
            Assert.Equal(dummyDirectoryPath, result.Path);
            Assert.Equal(dummyIncludeSubdirectories, result.IncludeSubdirectories);
            Assert.Equal(NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName, result.NotifyFilter);
        }

        [Fact]
        public void InternalFileChangedHandler_CallsFileChangedEventHandlerIfPathIsWatched()
        {
            // Arrange
            const string dummyDirectory = "dummyDirectory";
            const string dummyName = "dummyName";
            string dummyFullPath = Path.Combine(dummyDirectory, dummyName);
            var dummyFileSystemEventArgs = new FileSystemEventArgs(WatcherChangeTypes.Changed, dummyDirectory, dummyName);
            Mock<FileWatcherService> mockFileWatcherService = CreateMockFileWatcherService();
            mockFileWatcherService.CallBase = true;
            mockFileWatcherService.Setup(f => f.IsPathWatched(dummyFullPath)).Returns(true);
            mockFileWatcherService.Setup(f => f.InternalFileHandlerCoreAsync(dummyFullPath));

            // Act
            mockFileWatcherService.Object.InternalFileChangedHandler(new object(), dummyFileSystemEventArgs);

            // Assert
            _mockRepository.VerifyAll();
        }

        [Fact]
        public void InternalFileChangedHandler_DoesNotCallFileChangedEventHandlerIfPathIsNotWatched()
        {
            // Arrange
            const string dummyDirectory = "dummyDirectory";
            const string dummyName = "dummyName";
            var dummyFileSystemEventArgs = new FileSystemEventArgs(WatcherChangeTypes.Changed, dummyDirectory, dummyName);
            Mock<FileWatcherService> mockFileWatcherService = CreateMockFileWatcherService();
            mockFileWatcherService.CallBase = true;
            mockFileWatcherService.Setup(f => f.IsPathWatched(Path.Combine(dummyDirectory, dummyName))).Returns(false);

            // Act
            mockFileWatcherService.Object.InternalFileChangedHandler(new object(), dummyFileSystemEventArgs);

            // Assert
            _mockRepository.VerifyAll();
        }

        [Fact]
        public void InternalFileRenamedHandler_CallsFileChangedEventHandlerPassingNewPathToItIfNewPathIsWatched()
        {
            // Arrange
            const string dummyDirectory = "dummyDirectory";
            const string dummyNewName = "dummyNewName";
            string dummyNewPath = Path.Combine(dummyDirectory, dummyNewName);
            var dummyFileSystemEventArgs = new RenamedEventArgs(WatcherChangeTypes.Changed, dummyDirectory, dummyNewName, "dummyOldName");
            Mock<FileWatcherService> mockFileWatcherService = CreateMockFileWatcherService();
            mockFileWatcherService.CallBase = true;
            mockFileWatcherService.Setup(f => f.IsPathWatched(dummyNewPath)).Returns(true);
            mockFileWatcherService.Setup(f => f.InternalFileHandlerCoreAsync(dummyNewPath));

            // Act
            mockFileWatcherService.Object.InternalFileRenamedHandler(new object(), dummyFileSystemEventArgs);

            // Assert
            _mockRepository.VerifyAll();
        }

        [Fact]
        public void InternalFileRenamedHandler_CallsFileChangedEventHandlerPassingOldPathToItIfOnlyOldPathIsWatched()
        {
            // Arrange
            const string dummyDirectory = "dummyDirectory";
            const string dummyOldName = "dummyOldName";
            const string dummyNewName = "dummyNewName";
            string dummyOldPath = Path.Combine(dummyDirectory, dummyOldName);
            var dummyFileSystemEventArgs = new RenamedEventArgs(WatcherChangeTypes.Changed, dummyDirectory, dummyNewName, dummyOldName);
            Mock<FileWatcherService> mockFileWatcherService = CreateMockFileWatcherService();
            mockFileWatcherService.CallBase = true;
            mockFileWatcherService.Setup(f => f.IsPathWatched(dummyOldPath)).Returns(true);
            mockFileWatcherService.Setup(f => f.IsPathWatched(Path.Combine(dummyDirectory, dummyNewName))).Returns(false); // New path not watched
            mockFileWatcherService.Setup(f => f.InternalFileHandlerCoreAsync(dummyOldPath));

            // Act
            mockFileWatcherService.Object.InternalFileRenamedHandler(new object(), dummyFileSystemEventArgs);

            // Assert
            _mockRepository.VerifyAll();
        }

        [Fact]
        public void InternalFileRenamedHandler_DoesNotCallFileChangedEventHandlerIfNeitherOldNorNewPathIsWatched()
        {
            // Arrange
            const string dummyDirectory = "dummyDirectory";
            const string dummyOldName = "dummyOldName";
            const string dummyNewName = "dummyNewName";
            var dummyFileSystemEventArgs = new RenamedEventArgs(WatcherChangeTypes.Changed, dummyDirectory, dummyNewName, dummyOldName);
            Mock<FileWatcherService> mockFileWatcherService = CreateMockFileWatcherService();
            mockFileWatcherService.CallBase = true;
            mockFileWatcherService.Setup(f => f.IsPathWatched(Path.Combine(dummyDirectory, dummyOldName))).Returns(false);
            mockFileWatcherService.Setup(f => f.IsPathWatched(Path.Combine(dummyDirectory, dummyNewName))).Returns(false);

            // Act
            mockFileWatcherService.Object.InternalFileRenamedHandler(new object(), dummyFileSystemEventArgs);

            // Assert
            _mockRepository.VerifyAll();
            mockFileWatcherService.Verify(f => f.InternalFileHandlerCoreAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task InternalFileHandlerCoreAsync_DoesNothingIfNoFileChangedListenersSet()
        {
            // Arrange
            FileWatcherService testSubject = CreateFileWatcherService();

            // Act
            await testSubject.InternalFileHandlerCoreAsync("dummyPath").ConfigureAwait(false); // If the method attempts to invoke _fileChanged, this throws and the test fails
        }

        [Fact]
        public async Task InternalFileHandlerCoreAsync_DoesNothingIfCancellationTokenSourceIsCancelled()
        {
            // Arrange
            var dummyCancellationTokenSource = new CancellationTokenSource();
            dummyCancellationTokenSource.Cancel();
            Mock<FileWatcherService> mockTestSubject = CreateMockFileWatcherService();
            mockTestSubject.CallBase = true;
            mockTestSubject.Setup(f => f.CancelExistingAndGetNewCancellationTokenSource()).Returns(dummyCancellationTokenSource);
            mockTestSubject.Setup(f => f.CreateFileSystemWatcher()).Returns((FileSystemWatcher)null!);
            FileWatcherService testSubject = mockTestSubject.Object;
            void dummyAction() => throw new Exception("Should not be called");
            await testSubject.AddFileChangedListenerAsync(dummyAction).ConfigureAwait(false);

            // Act
            await mockTestSubject.Object.InternalFileHandlerCoreAsync("dummyPath").ConfigureAwait(false); // If the method attempts to invoke _fileChanged, this throws and the test fails

            // Assert
            _mockRepository.VerifyAll();
        }

        [Fact]
        public async Task InternalFileHandlerCoreAsync_CallsFileChangedEventHandler()
        {
            // Arrange
            Mock<FileWatcherService> mockTestSubject = CreateMockFileWatcherService();
            mockTestSubject.CallBase = true;
            mockTestSubject.Setup(f => f.CreateFileSystemWatcher()).Returns((FileSystemWatcher)null!);
            FileWatcherService testSubject = mockTestSubject.Object;
            bool resultFileChangedListenerCalled = false;
            void dummyAction() { resultFileChangedListenerCalled = true; }
            await testSubject.AddFileChangedListenerAsync(dummyAction).ConfigureAwait(false);

            // Act
            await mockTestSubject.Object.InternalFileHandlerCoreAsync("dummyPath").ConfigureAwait(false);

            // Assert
            _mockRepository.VerifyAll();
            Assert.True(resultFileChangedListenerCalled);
        }

        [Fact]
        public void CancelExistingAndGetNewCancellationTokenSource_CancelsExistingCancellationTokenSourceAndReturnsNewCancellationTokenSource()
        {
            // Arrange
            FileWatcherService testSubject = CreateFileWatcherService();

            // Act
            CancellationTokenSource resultCancellationTokenSource1 = testSubject.CancelExistingAndGetNewCancellationTokenSource();
            CancellationTokenSource resultCancellationTokenSource2 = testSubject.CancelExistingAndGetNewCancellationTokenSource();

            // Assert
            Assert.NotNull(resultCancellationTokenSource1);
            Assert.True(resultCancellationTokenSource1.IsCancellationRequested);
            Assert.NotNull(resultCancellationTokenSource2);
            Assert.False(resultCancellationTokenSource2.IsCancellationRequested);
        }

        [Fact]
        public void DisposeAndRemoveCancellationTokenSource_DisposesOfCancellationTokenSource()
        {
            // Arrange
            var dummyCancellationTokenSource = new CancellationTokenSource();
            FileWatcherService testSubject = CreateFileWatcherService();

            // Act
            testSubject.DisposeAndRemoveCancellationTokenSource(dummyCancellationTokenSource);

            // Assert
            Assert.Throws<ObjectDisposedException>(() => dummyCancellationTokenSource.Token);
        }

        [Theory]
        [MemberData(nameof(IsPathWatched_ReturnsFalseIfPathIsNullWhitespaceOrAnEmptyString_Data))]
        public void IsPathWatched_ReturnsFalseIfPathIsNullWhitespaceOrAnEmptyString(string dummyPath)
        {
            // Arrange
            FileWatcherService testSubject = CreateFileWatcherService();

            // Act
            bool result = testSubject.IsPathWatched(dummyPath);

            // Assert
            Assert.False(result);
        }

        public static IEnumerable<object?[]> IsPathWatched_ReturnsFalseIfPathIsNullWhitespaceOrAnEmptyString_Data()
        {
            return new object?[][]
            {
                new object?[]{ null },
                new object?[]{ " " },
                new object?[]{ string.Empty }
            };
        }

        [Theory]
        [MemberData(nameof(IsPathWatched_ReturnsTrueIfFileNameMatchesAFilterFalseOtherwise_Data))]
        public async Task IsPathWatched_ReturnsTrueIfFileNameMatchesAFilterFalseOtherwise(Regex[] dummyFilters, string dummyPath, bool expectedResult)
        {
            // Arrange
            OutOfProcessNodeJSServiceOptions dummyOutOfProcessNodeJSOptions = new();
            Mock<IOptions<OutOfProcessNodeJSServiceOptions>> mockOutOfProcessNodeJSOptions = _mockRepository.Create<IOptions<OutOfProcessNodeJSServiceOptions>>();
            mockOutOfProcessNodeJSOptions.Setup(o => o.Value).Returns(dummyOutOfProcessNodeJSOptions);
            NodeJSProcessOptions dummyNodeJSProcessOptions = new();
            Mock<IOptions<NodeJSProcessOptions>> mockNodeJSProcessOptions = _mockRepository.Create<IOptions<NodeJSProcessOptions>>();
            mockNodeJSProcessOptions.Setup(o => o.Value).Returns(dummyNodeJSProcessOptions);
            Mock<FileWatcherService> mockTestSubject = CreateMockFileWatcherService(outOfProcessNodeHostOptionsAccessor: mockOutOfProcessNodeJSOptions.Object,
                nodeJSProcessOptionsAccessor: mockNodeJSProcessOptions.Object);
            mockTestSubject.CallBase = true;
            mockTestSubject.Setup(t => t.ResolveFilters(dummyOutOfProcessNodeJSOptions.WatchFileNamePatterns)).Returns(new ReadOnlyCollection<Regex>(dummyFilters));
            mockTestSubject.Setup(t => t.ResolveDirectoryPath(dummyOutOfProcessNodeJSOptions.WatchPath, dummyNodeJSProcessOptions.ProjectPath)).Returns(Directory.GetCurrentDirectory());
            FileWatcherService testSubject = mockTestSubject.Object;
            await testSubject.AddFileChangedListenerAsync(() => { }).ConfigureAwait(false);

            // Act
            bool result = testSubject.IsPathWatched(dummyPath);

            // Assert
            Assert.Equal(expectedResult, result);
        }

        public static IEnumerable<object[]> IsPathWatched_ReturnsTrueIfFileNameMatchesAFilterFalseOtherwise_Data()
        {
            Regex[] dummyFilters = new[] { new Regex("^.*\\.ts$"), new Regex("^.*\\.js$"), new Regex("^.*\\.html$") };

            return new object[][]
            {
                // True if file name matches a filter
                new object[]{
                    dummyFilters,
                    "dummy/path/dummy.js",
                    true
                },
                // False if file name doesn't match any filter
                new object[]{
                    dummyFilters,
                    "dummy/path/dummy.jsx",
                    false
                }
            };
        }

        [Theory]
        [MemberData(nameof(ResolveDirectoryPath_ResolvesDirectoryPath_Data))]
        public void ResolveDirectoryPath_ResolvesDirectoryPath(string dummyDirectoryPath, string dummyProjectPath, string expectedResult)
        {
            // Arrange
            FileWatcherService testSubject = CreateFileWatcherService();

            // Act
            string result = testSubject.ResolveDirectoryPath(dummyDirectoryPath, dummyProjectPath);

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
            Mock<FileWatcherService> mockTestSubject = CreateMockFileWatcherService();
            mockTestSubject.CallBase = true;
            mockTestSubject.Setup(t => t.CreateRegex(dummyFileNamePattern1)).Returns(dummyRegex1);
            mockTestSubject.Setup(t => t.CreateRegex(dummyFileNamePattern2)).Returns(dummyRegex2);

            // Act
            ReadOnlyCollection<Regex> result1 = mockTestSubject.Object.ResolveFilters(dummyFileNamePatterns);

            // Assert
            _mockRepository.VerifyAll();
            Assert.Same(dummyRegex1, result1[0]);
            Assert.Same(dummyRegex2, result1[1]);
        }

        [Theory]
        [MemberData(nameof(CreateRegex_CreatesRegex_Data))]
        public void CreateRegex_CreatesRegex(string dummyFileNamePattern, string expectedRegexPattern, string[] expectedMatches, string[] expectedNonMatches)
        {
            // Arrange
            FileWatcherService testSubject = CreateFileWatcherService();

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

        private Mock<FileWatcherService> CreateMockFileWatcherService(IOptions<OutOfProcessNodeJSServiceOptions>? outOfProcessNodeHostOptionsAccessor = null,
            IOptions<NodeJSProcessOptions>? nodeJSProcessOptionsAccessor = null,
            ILogger<FileWatcherService>? logger = null)
        {
            return _mockRepository.Create<FileWatcherService>(nodeJSProcessOptionsAccessor ?? _mockRepository.Create<IOptions<NodeJSProcessOptions>>().Object,
                outOfProcessNodeHostOptionsAccessor ?? _mockRepository.Create<IOptions<OutOfProcessNodeJSServiceOptions>>().Object,
                logger ?? _mockRepository.Create<ILogger<FileWatcherService>>().Object);
        }

        private FileWatcherService CreateFileWatcherService(IOptions<OutOfProcessNodeJSServiceOptions>? outOfProcessNodeHostOptionsAccessor = null,
            IOptions<NodeJSProcessOptions>? nodeJSProcessOptionsAccessor = null,
            ILogger<FileWatcherService>? logger = null)
        {
            return new FileWatcherService(nodeJSProcessOptionsAccessor ?? _mockRepository.Create<IOptions<NodeJSProcessOptions>>().Object,
                outOfProcessNodeHostOptionsAccessor ?? _mockRepository.Create<IOptions<OutOfProcessNodeJSServiceOptions>>().Object,
                logger ?? _mockRepository.Create<ILogger<FileWatcherService>>().Object);
        }
    }
}
