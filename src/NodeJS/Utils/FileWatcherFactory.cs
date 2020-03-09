using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;

namespace Jering.Javascript.NodeJS
{
    /// <summary>
    /// The default implementation of <see cref="IFileWatcherFactory"/>.
    /// </summary>
    public class FileWatcherFactory : IFileWatcherFactory
    {
        private readonly ConcurrentDictionary<string, Regex> _cachedRegices = new ConcurrentDictionary<string, Regex>();
        private readonly NodeJSProcessOptions _nodeJSProcessOptions;

        /// <summary>
        /// Creates a <see cref="FileWatcherFactory"/>.
        /// </summary>
        /// <param name="nodeJSProcessOptionsAccessor">The NodeJS process options.</param>
        public FileWatcherFactory(IOptions<NodeJSProcessOptions> nodeJSProcessOptionsAccessor)
        {
            _nodeJSProcessOptions = nodeJSProcessOptionsAccessor.Value;
        }

        /// <inheritdoc />
        public IFileWatcher Create(string directoryPath,
            bool includeSubdirectories,
            IEnumerable<string> fileNamePatterns,
            FileChangedEventHandler fileChangedEventHandler)
        {
            directoryPath = ResolveDirectoryPath(directoryPath, _nodeJSProcessOptions);
            ReadOnlyCollection<Regex> filters = ResolveFilters(fileNamePatterns);

            return new FileWatcher(directoryPath, includeSubdirectories, filters, fileChangedEventHandler);
        }

        // TODO validate options using https://docs.microsoft.com/en-us/aspnet/core/fundamentals/configuration/options?view=aspnetcore-3.1#options-validation
        // so directoryPath and NodeJSProcessOptions are never both null, whitespace or empty strings.
        internal virtual string ResolveDirectoryPath(string directoryPath, NodeJSProcessOptions nodeJSProcessOptions)
        {
            return string.IsNullOrWhiteSpace(directoryPath) ? nodeJSProcessOptions.ProjectPath : directoryPath;
        }

        internal virtual ReadOnlyCollection<Regex> ResolveFilters(IEnumerable<string> fileNamePatterns)
        {
            int count = fileNamePatterns.Count();
            var regices = new Regex[count];

            for (int i = 0; i < count; i++)
            {
                string fileNamePattern = fileNamePatterns.ElementAt(i);
                // Note that CreateRegex may get called multiple times for the same fileNamePattern - https://github.com/dotnet/runtime/issues/24293. 
                // This is fine for now since it doesn't do much.
                regices[i] = _cachedRegices.GetOrAdd(fileNamePattern, CreateRegex);
            }

            return new ReadOnlyCollection<Regex>(regices);
        }

        internal virtual Regex CreateRegex(string fileNamePattern)
        {
            string regexPattern = "^" + Regex.Escape(fileNamePattern).Replace("\\*", ".*").Replace("\\?", ".?") + "$";

            return new Regex(regexPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
        }
    }
}
