using System.Collections.Generic;
using System.IO;

namespace Jering.Javascript.NodeJS
{
    /// <summary>
    /// Options for an <see cref="OutOfProcessNodeJSService"/>.
    /// </summary>
    public class OutOfProcessNodeJSServiceOptions
    {
        /// <summary>The maximum duration to wait for the NodeJS process to connect and to wait for responses to invocations.</summary>
        /// <remarks>
        /// <para>If this value is negative, the maximum duration is infinite.</para>
        /// <para>Defaults to 60000.</para>
        /// </remarks>
        public int TimeoutMS { get; set; } = 60000;

        /// <summary>The number of times a NodeJS process retries an invocation.</summary>
        /// <remarks>
        /// <para>If this value is negative, invocations are retried indefinitely.</para>
        /// <para>If an invocation's module source is an unseekable stream, the invocation is not retried.
        /// If you require retries for such streams, copy their contents to a <see cref="MemoryStream"/>.</para>
        /// <para>Defaults to 1.</para>
        /// </remarks>
        public int NumRetries { get; set; } = 1;

        /// <summary>The number of new NodeJS processes created to retry an invocation.</summary>
        /// <remarks>
        /// <para>A NodeJS process retries invocations <see cref="NumRetries"/> times. Once a process's retries are exhausted,
        /// if any <b>process retries</b> remain, the library creates a new process that then retries invocations <see cref="NumRetries"/> times.</para>
        /// <para>For example, consider the situation where <see cref="NumRetries"/> and this value are both 1. The existing process first attempts the invocation.
        /// If it fails, it retries the invocation once. If it fails again, the library creates a new process that retries the invocation once. In total, the library
        /// attempt the invocation 3 times.</para>
        /// <para>If this value is negative, the library creates new NodeJS processes indefinitely.</para>
        /// <para>If the module source of an invocation is an unseekable stream, the invocation is not retried.
        /// If you require retries for such streams, copy their contents to a <see cref="MemoryStream"/>.</para>
        /// <para>Defaults to 1.</para>
        /// </remarks>
        public int NumProcessRetries { get; set; } = 1;

        /// <summary>Number of times the library retries NodeJS connection attempts.</summary>
        /// <remarks>
        /// <para>If this value is negative, connection attempts are retried indefinitely.</para>
        /// <para>Defaults to 1.</para>
        /// </remarks>
        public int NumConnectionRetries { get; set; } = 1;

        /// <summary>The concurrency mode for invocations.</summary>
        /// <remarks>
        /// <para>By default, this value is <see cref="Concurrency.None"/>. In this mode, a single NodeJS process executes invocations synchronously. 
        /// This mode has the benefit of lower memory overhead and it supports all modules. However, it is less performant.</para>
        /// <para>If this value is <see cref="Concurrency.MultiProcess"/>, <see cref="ConcurrencyDegree"/> NodeJS processes are created and invocations are
        /// distributed among them using round robin load balancing. This mode is more performant. However, it has higher memory overhead and doesn't work with modules that 
        /// have persistent state.</para>
        /// <para>Defaults to <see cref="Concurrency.None"/>.</para>
        /// </remarks>
        public Concurrency Concurrency { get; set; }

        /// <summary>The concurrency degree.</summary>
        /// <remarks>
        /// <para>If <see cref="Concurrency"/> is <see cref="Concurrency.MultiProcess"/>, this value is the number of NodeJS processes.</para>
        /// <para>If this value is less than or equal to 0, concurrency degree is the number of logical processors the current machine has.</para>
        /// <para>This value does nothing if <see cref="Concurrency"/> is <see cref="Concurrency.None"/>.</para>
        /// <para>Defaults to 0.</para>
        /// </remarks>
        public int ConcurrencyDegree { get; set; }

        /// <summary>The value specifying whether file watching is enabled.</summary>
        /// <remarks>
        /// <para>If file watching is enabled, the library watches files in <see cref="WatchPath"/> with file name matching a pattern in <see cref="WatchFileNamePatterns"/>. 
        /// The library restarts NodeJS when a watched file changes.</para>
        /// <para>Works with all <see cref="Concurrency"/> modes.</para>
        /// <para>Defaults to <c>false</c>.</para>
        /// </remarks>
        public bool EnableFileWatching { get; set; }

        /// <summary>The directory to watch for file changes.</summary>
        /// <remarks>
        /// <para>If this value is <c>null</c>, the path <see cref="NodeJSProcessOptions.ProjectPath"/> is watched.</para>
        /// <para>This value does nothing if <see cref="EnableFileWatching"/> is <c>false</c>.</para>
        /// <para>Defaults to <c>null</c></para>
        /// </remarks>
        public string? WatchPath { get; set; }

        /// <summary>The value specifying whether subdirectories of <see cref="WatchPath"/> are watched.</summary>
        /// <remarks>
        /// <para>This value does nothing if <see cref="EnableFileWatching"/> is <c>false</c>.</para>
        /// <para>Defaults to <c>true</c>.</para>
        /// </remarks>
        public bool WatchSubdirectories { get; set; } = true;

        /// <summary>The file name patterns to watch.</summary>
        /// <remarks>
        /// <para>In a pattern, "*" represents 0 or more of any character and "?" represents 0 or 1 of any character. For example,
        /// "TestFile1.js" matches the pattern "*File?.js".</para>
        /// <para>This value does nothing if <see cref="EnableFileWatching"/> is <c>false</c>.</para>
        /// <para>Defaults to "*.js", "*.jsx", "*.ts", "*.tsx", "*.json" and "*.html".</para>
        /// </remarks>
        public IEnumerable<string> WatchFileNamePatterns { get; set; } = new[] { "*.js", "*.jsx", "*.ts", "*.tsx", "*.json", "*.html" };

        /// <summary>The value specifying whether NodeJS processes shutdown gracefully when moving to a new process.</summary>
        /// <remarks>
        /// <para>If this value is true, NodeJS processes shutdown gracefully. Otherwise they're killed immediately.</para>
        /// <para>What's a graceful shutdown? When the library creates a new NodeJS process, the old NodeJS process
        /// might still be handling earlier invocations. If graceful shutdown is enabled, the old NodeJS process is killed <b>after</b> its
        /// invocations complete. If graceful shutdown is disabled, the old NodeJS process is killed immediately and existing
        /// invocations are retried in the new NodeJS process (assuming they have remaining retries, see <see cref="NumRetries"/>).</para>
        /// <para>Should I use graceful shutdown? Shutting down gracefully is safer: chances of an invocation exhausting retries and failing is lower, also,
        /// you won't face issues from an invocation terminating midway. However, graceful shutdown does incur a small performance cost.
        /// Also, invocations complete using the outdated version of your script. Weigh these factors for your script and use-case to decide whether to use graceful shutdown.</para>
        /// <para>This value does nothing if <see cref="EnableFileWatching"/> is <c>false</c> and <see cref="NumProcessRetries"/> is 0.</para>
        /// <para>Defaults to <c>true</c>.</para>
        /// </remarks>
        public bool GracefulProcessShutdown { get; set; } = true;
    }
}
