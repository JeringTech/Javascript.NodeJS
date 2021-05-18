using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Xunit;

namespace Jering.Javascript.NodeJS.Tests
{
    public sealed class FileWatcherUnitTests : IDisposable
    {
        private readonly MockRepository _mockRepository = new(MockBehavior.Default);
        private string? _tempWatchDirectory; // Only set if we actually create a directory so Dispose knows when to delete

        [Theory]
        [MemberData(nameof(Constructor_ThrowsArgumentExceptionIfDirectoryPathIsNullWhitespaceOrAnEmptyString_Data))]
        public void Constructor_ThrowsArgumentExceptionIfDirectoryPathIsNullWhitespaceOrAnEmptyString(string dummyDirectoryPath)
        {
            // Act and assert
            Assert.Throws<ArgumentException>(() => new FileWatcher(dummyDirectoryPath, false, new[] { new Regex(".") }, DummyFileChangedHandler));
        }

        public static IEnumerable<object?[]> Constructor_ThrowsArgumentExceptionIfDirectoryPathIsNullWhitespaceOrAnEmptyString_Data()
        {
            return new object?[][]
            {
                new object?[]{null},
                new object?[]{" "},
                new object?[]{string.Empty}
            };
        }

        [Fact]
        public void Constructor_ThrowsArgumentNullExceptionIfFiltersIsNull()
        {
            // Act and assert
            Assert.Throws<ArgumentNullException>(() => new FileWatcher("dummyDirectoryPath", false, null!, DummyFileChangedHandler)); // Testing situation where user ignores nullable reference type warnings
        }

        [Fact]
        public void Constructor_ThrowsArgumentExceptionIfFiltersIsEmpty()
        {
            // Act and assert
            Assert.Throws<ArgumentException>(() => new FileWatcher("dummyDirectoryPath", false, Array.Empty<Regex>(), DummyFileChangedHandler));
        }

        [Fact]
        public void Constructor_ThrowsArgumentNullExceptionIfFileChangedEventHandlerIsNull()
        {
            // Act and assert
            Assert.Throws<ArgumentNullException>(() => new FileWatcher("dummyDirectoryPath", false, new[] { new Regex(".") }, null!)); // Testing situation where user ignores nullable reference type warnings
        }

        // TODO verify that events are registered
        [Fact]
        public void Start_CreatesNewFileSystemWatcher()
        {
            // Arrange
            using var dummyFileSystemWatcher = new FileSystemWatcher(CreateWatchDirectory());
            Mock<FileWatcher> mockTestSubject = CreateMockFileWatcher();
            mockTestSubject.CallBase = true;
            mockTestSubject.Setup(t => t.CreateFileSystemWatcher()).Returns(dummyFileSystemWatcher);

            // Act
            mockTestSubject.Object.Start();

            // Assert
            _mockRepository.VerifyAll();
        }

        [Fact]
        public void InternalFileChangedHandler_CallsFileChangedEventHandlerIfPathIsWatched()
        {
            // Arrange
            const string dummyDirectory = "dummyDirectory";
            const string dummyName = "dummyName";
            string dummyFullPath = Path.Combine(dummyDirectory, dummyName);
            string? result = null;
            var dummyFileSystemEventArgs = new FileSystemEventArgs(WatcherChangeTypes.Changed, dummyDirectory, dummyName);
            Mock<FileWatcher> mockFileWatcher = CreateMockFileWatcher(fileChangedEventHandler: (path) => result = path);
            mockFileWatcher.CallBase = true;
            mockFileWatcher.Setup(f => f.IsPathWatched(dummyFullPath)).Returns(true);

            // Act
            mockFileWatcher.Object.InternalFileChangedHandler(new object(), dummyFileSystemEventArgs);

            // Assert
            _mockRepository.VerifyAll();
            Assert.Equal(dummyFullPath, result);
        }

        [Fact]
        public void InternalFileChangedHandler_DoesNotCallFileChangedEventHandlerIfPathIsNotWatched()
        {
            // Arrange
            const string dummyDirectory = "dummyDirectory";
            const string dummyName = "dummyName";
            string? result = null;
            var dummyFileSystemEventArgs = new FileSystemEventArgs(WatcherChangeTypes.Changed, dummyDirectory, dummyName);
            Mock<FileWatcher> mockFileWatcher = CreateMockFileWatcher(fileChangedEventHandler: (path) => result = path);
            mockFileWatcher.CallBase = true;
            mockFileWatcher.Setup(f => f.IsPathWatched(Path.Combine(dummyDirectory, dummyName))).Returns(false);

            // Act
            mockFileWatcher.Object.InternalFileChangedHandler(new object(), dummyFileSystemEventArgs);

            // Assert
            _mockRepository.VerifyAll();
            Assert.Null(result);
        }

        [Fact]
        public void InternalFileRenamedHandler_CallsFileChangedEventHandlerPassingNewPathToItIfNewPathIsWatched()
        {
            // Arrange
            const string dummyDirectory = "dummyDirectory";
            const string dummyNewName = "dummyNewName";
            string dummyNewPath = Path.Combine(dummyDirectory, dummyNewName);
            string? result = null;
            var dummyFileSystemEventArgs = new RenamedEventArgs(WatcherChangeTypes.Changed, dummyDirectory, dummyNewName, "dummyOldName");
            Mock<FileWatcher> mockFileWatcher = CreateMockFileWatcher(fileChangedEventHandler: (path) => result = path);
            mockFileWatcher.CallBase = true;
            mockFileWatcher.Setup(f => f.IsPathWatched(dummyNewPath)).Returns(true);

            // Act
            mockFileWatcher.Object.InternalFileRenamedHandler(new object(), dummyFileSystemEventArgs);

            // Assert
            _mockRepository.VerifyAll();
            Assert.Equal(dummyNewPath, result);
        }

        [Fact]
        public void InternalFileRenamedHandler_CallsFileChangedEventHandlerPassingOldPathToItIfOnlyOldPathIsWatched()
        {
            // Arrange
            const string dummyDirectory = "dummyDirectory";
            const string dummyOldName = "dummyOldName";
            const string dummyNewName = "dummyNewName";
            string dummyOldPath = Path.Combine(dummyDirectory, dummyOldName);
            string? result = null;
            var dummyFileSystemEventArgs = new RenamedEventArgs(WatcherChangeTypes.Changed, dummyDirectory, dummyNewName, dummyOldName);
            Mock<FileWatcher> mockFileWatcher = CreateMockFileWatcher(fileChangedEventHandler: (path) => result = path);
            mockFileWatcher.CallBase = true;
            mockFileWatcher.Setup(f => f.IsPathWatched(dummyOldPath)).Returns(true);
            mockFileWatcher.Setup(f => f.IsPathWatched(Path.Combine(dummyDirectory, dummyNewName))).Returns(false); // New path not watched

            // Act
            mockFileWatcher.Object.InternalFileRenamedHandler(new object(), dummyFileSystemEventArgs);

            // Assert
            _mockRepository.VerifyAll();
            Assert.Equal(dummyOldPath, result);
        }

        [Fact]
        public void InternalFileRenamedHandler_DoesNotCallFileChangedEventHandlerIfNeitherOldNorNewPathIsWatched()
        {
            // Arrange
            const string dummyDirectory = "dummyDirectory";
            const string dummyOldName = "dummyOldName";
            const string dummyNewName = "dummyNewName";
            string? result = null;
            var dummyFileSystemEventArgs = new RenamedEventArgs(WatcherChangeTypes.Changed, dummyDirectory, dummyNewName, dummyOldName);
            Mock<FileWatcher> mockFileWatcher = CreateMockFileWatcher(fileChangedEventHandler: (path) => result = path);
            mockFileWatcher.CallBase = true;
            mockFileWatcher.Setup(f => f.IsPathWatched(Path.Combine(dummyDirectory, dummyOldName))).Returns(false);
            mockFileWatcher.Setup(f => f.IsPathWatched(Path.Combine(dummyDirectory, dummyNewName))).Returns(false);

            // Act
            mockFileWatcher.Object.InternalFileRenamedHandler(new object(), dummyFileSystemEventArgs);

            // Assert
            _mockRepository.VerifyAll();
            Assert.Null(result);
        }

        [Theory]
        [MemberData(nameof(IsPathWatched_ReturnsFalseIfPathIsNullWhitespaceOrAnEmptyString_Data))]
        public void IsPathWatched_ReturnsFalseIfPathIsNullWhitespaceOrAnEmptyString(string dummyPath)
        {
            // Arrange
            FileWatcher testSubject = CreateFileWatcher();

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
        public void IsPathWatched_ReturnsTrueIfFileNameMatchesAFilterFalseOtherwise(Regex[] dummyFilters, string dummyPath, bool expectedResult)
        {
            // Arrange
            FileWatcher testSubject = CreateFileWatcher(filters: dummyFilters);

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

        [Fact]
        public void CreateFileSystemWatcher_CreatesFileSystemWatcher()
        {
            // Arrange
            string dummyDirectoryPath = Directory.GetCurrentDirectory(); // FileSystemWatcher constructor requires an existing path
            const bool dummyIncludeSubdirectories = true;
            FileWatcher testSubject = CreateFileWatcher(dummyDirectoryPath, dummyIncludeSubdirectories);

            // Act
            FileSystemWatcher result = testSubject.CreateFileSystemWatcher();

            // Assert
            Assert.Equal(dummyDirectoryPath, result.Path);
            Assert.Equal(dummyIncludeSubdirectories, result.IncludeSubdirectories);
            Assert.Equal(NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName, result.NotifyFilter);
        }

        private Mock<FileWatcher> CreateMockFileWatcher(string? directoryPath = null,
            bool includeSubdirectories = false,
            IEnumerable<Regex>? filters = null,
            FileChangedEventHandler? fileChangedEventHandler = null)
        {
            return _mockRepository.Create<FileWatcher>(directoryPath ?? "dummyDirectoryPath",
                includeSubdirectories,
                filters ?? new[] { new Regex(".") },
                fileChangedEventHandler ?? DummyFileChangedHandler);
        }

        private FileWatcher CreateFileWatcher(string? directoryPath = null,
            bool includeSubdirectories = false,
            IEnumerable<Regex>? filters = null,
            FileChangedEventHandler? fileChangedEventHandler = null)
        {
            return new FileWatcher(directoryPath ?? "dummyDirectoryPath",
                includeSubdirectories,
                filters ?? new[] { new Regex(".") },
                fileChangedEventHandler ?? DummyFileChangedHandler);
        }

        private void DummyFileChangedHandler(string _)
        {
        }

        private string CreateWatchDirectory()
        {
            if(_tempWatchDirectory != null)
            {
                return _tempWatchDirectory;
            }

            _tempWatchDirectory = Path.Combine(Path.GetTempPath(), nameof(FileWatcherUnitTests));
            Directory.CreateDirectory(_tempWatchDirectory);

            return _tempWatchDirectory;
        }

        public void Dispose()
        {
            TryDeleteWatchDirectory();
        }

        private void TryDeleteWatchDirectory()
        {
            try
            {
                if (_tempWatchDirectory != null)
                {
                    Directory.Delete(_tempWatchDirectory, true);
                }
            }
            catch
            {
                // Do nothing
            }
        }
    }
}
