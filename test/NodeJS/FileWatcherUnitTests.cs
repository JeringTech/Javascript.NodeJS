using Moq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Xunit;

namespace Jering.Javascript.NodeJS.Tests
{
    public class FileWatcherUnitTests : IDisposable
    {
        private readonly MockRepository _mockRepository = new MockRepository(MockBehavior.Default);
        private string _tempWatchDirectory;

        [Fact]
        public void Constructor_ThrowsArgumentNullExceptionIfFileSystemWatcherIsNull()
        {
            // Act and assert
            Assert.Throws<ArgumentNullException>(() => new FileWatcher(null, new[] { new Regex(".") }, DummyFileChangedHandler));
        }

        [Fact]
        public void Constructor_ThrowsArgumentNullExceptionIfFiltersIsNull()
        {
            // Act and assert
            Assert.Throws<ArgumentNullException>(() => new FileWatcher(new FileSystemWatcher(), null, DummyFileChangedHandler));
        }

        [Fact]
        public void Constructor_ThrowsArgumentExceptionIfFiltersIsEmpty()
        {
            // Act and assert
            Assert.Throws<ArgumentException>(() => new FileWatcher(new FileSystemWatcher(), new Regex[0], DummyFileChangedHandler));
        }

        [Fact]
        public void Constructor_ThrowsArgumentNullExceptionIfFileChangedEventHandlerIsNull()
        {
            // Act and assert
            Assert.Throws<ArgumentNullException>(() => new FileWatcher(new FileSystemWatcher(), new[] { new Regex(".") }, null));
        }

        [Fact]
        public void Start_StartsFileWatching()
        {
            // Arrange
            RecreateWatchDirectory();
            using (var dummyFileSystemWatcher = new FileSystemWatcher(_tempWatchDirectory)) // Must provide an existing directory or it throws
            {
                FileWatcher testSubject = CreateFileWatcher(dummyFileSystemWatcher);

                // Act
                testSubject.Start(); // Stopped initially

                // Assert
                Assert.True(dummyFileSystemWatcher.EnableRaisingEvents);
            }
        }

        [Fact]
        public void Stop_StopsFileWatching()
        {
            // Arrange
            RecreateWatchDirectory();
            using (var dummyFileSystemWatcher = new FileSystemWatcher(_tempWatchDirectory)) // Must provide an existing directory or it throws
            {
                FileWatcher testSubject = CreateFileWatcher(dummyFileSystemWatcher);
                testSubject.Start();

                // Act
                testSubject.Stop();

                // Assert
                Assert.False(dummyFileSystemWatcher.EnableRaisingEvents);
            }
        }

        [Fact]
        public void InternalFileChangedHandler_CallsFileChangedEventHandlerIfPathIsWatched()
        {
            // Arrange
            const string dummyDirectory = "dummyDirectory";
            const string dummyName = "dummyName";
            string dummyFullPath = Path.Combine(dummyDirectory, dummyName);
            string result = null;
            var dummyFileSystemEventArgs = new FileSystemEventArgs(WatcherChangeTypes.Changed, dummyDirectory, dummyName);
            Mock<FileWatcher> mockFileWatcher = CreateMockFileWatcher(fileChangedEventHandler: (path) => result = path);
            mockFileWatcher.CallBase = true;
            mockFileWatcher.Setup(f => f.IsPathWatched(dummyFullPath)).Returns(true);

            // Act
            mockFileWatcher.Object.InternalFileChangedHandler(null, dummyFileSystemEventArgs);

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
            string result = null;
            var dummyFileSystemEventArgs = new FileSystemEventArgs(WatcherChangeTypes.Changed, dummyDirectory, dummyName);
            Mock<FileWatcher> mockFileWatcher = CreateMockFileWatcher(fileChangedEventHandler: (path) => result = path);
            mockFileWatcher.CallBase = true;
            mockFileWatcher.Setup(f => f.IsPathWatched(Path.Combine(dummyDirectory, dummyName))).Returns(false);

            // Act
            mockFileWatcher.Object.InternalFileChangedHandler(null, dummyFileSystemEventArgs);

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
            string result = null;
            var dummyFileSystemEventArgs = new RenamedEventArgs(WatcherChangeTypes.Changed, dummyDirectory, dummyNewName, "dummyOldName");
            Mock<FileWatcher> mockFileWatcher = CreateMockFileWatcher(fileChangedEventHandler: (path) => result = path);
            mockFileWatcher.CallBase = true;
            mockFileWatcher.Setup(f => f.IsPathWatched(dummyNewPath)).Returns(true);

            // Act
            mockFileWatcher.Object.InternalFileRenamedHandler(null, dummyFileSystemEventArgs);

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
            string result = null;
            var dummyFileSystemEventArgs = new RenamedEventArgs(WatcherChangeTypes.Changed, dummyDirectory, dummyNewName, dummyOldName);
            Mock<FileWatcher> mockFileWatcher = CreateMockFileWatcher(fileChangedEventHandler: (path) => result = path);
            mockFileWatcher.CallBase = true;
            mockFileWatcher.Setup(f => f.IsPathWatched(dummyOldPath)).Returns(true);
            mockFileWatcher.Setup(f => f.IsPathWatched(Path.Combine(dummyDirectory, dummyNewName))).Returns(false); // New path not watched

            // Act
            mockFileWatcher.Object.InternalFileRenamedHandler(null, dummyFileSystemEventArgs);

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
            string result = null;
            var dummyFileSystemEventArgs = new RenamedEventArgs(WatcherChangeTypes.Changed, dummyDirectory, dummyNewName, dummyOldName);
            Mock<FileWatcher> mockFileWatcher = CreateMockFileWatcher(fileChangedEventHandler: (path) => result = path);
            mockFileWatcher.CallBase = true;
            mockFileWatcher.Setup(f => f.IsPathWatched(Path.Combine(dummyDirectory, dummyOldName))).Returns(false);
            mockFileWatcher.Setup(f => f.IsPathWatched(Path.Combine(dummyDirectory, dummyNewName))).Returns(false);

            // Act
            mockFileWatcher.Object.InternalFileRenamedHandler(null, dummyFileSystemEventArgs);

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

        public static IEnumerable<object[]> IsPathWatched_ReturnsFalseIfPathIsNullWhitespaceOrAnEmptyString_Data()
        {
            return new object[][]
            {
                new object[]{ null },
                new object[]{ " " },
                new object[]{ string.Empty }
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

        private Mock<FileWatcher> CreateMockFileWatcher(FileSystemWatcher fileSystemWatcher = null,
            IEnumerable<Regex> filters = null,
            FileChangedEventHandler fileChangedEventHandler = null)
        {
            return _mockRepository.Create<FileWatcher>(fileSystemWatcher ?? new FileSystemWatcher(),
                filters ?? new[] { new Regex(".") },
                fileChangedEventHandler ?? DummyFileChangedHandler);
        }

        private FileWatcher CreateFileWatcher(FileSystemWatcher fileSystemWatcher = null,
            IEnumerable<Regex> filters = null,
            FileChangedEventHandler fileChangedEventHandler = null)
        {
            return new FileWatcher(fileSystemWatcher ?? new FileSystemWatcher(),
                filters ?? new[] { new Regex(".") },
                fileChangedEventHandler ?? DummyFileChangedHandler);
        }

        private void DummyFileChangedHandler(string _)
        {
        }

        private void RecreateWatchDirectory()
        {
            _tempWatchDirectory = Path.Combine(Path.GetTempPath(), nameof(FileWatcherUnitTests));
            Directory.CreateDirectory(_tempWatchDirectory);
        }

        public void Dispose()
        {
            if (_tempWatchDirectory != null)
            {
                TryDeleteWatchDirectory();
            }
        }

        private void TryDeleteWatchDirectory()
        {
            try
            {
                Directory.Delete(_tempWatchDirectory, true);
            }
            catch
            {
                // Do nothing
            }
        }
    }
}
