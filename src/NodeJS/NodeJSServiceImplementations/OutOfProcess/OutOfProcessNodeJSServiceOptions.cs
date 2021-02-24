using System.Collections.Generic;
using System.IO;

namespace Jering.Javascript.NodeJS
{
    /// <summary>
    /// Options for an <see cref="OutOfProcessNodeJSService"/>.
    /// </summary>
    public class OutOfProcessNodeJSServiceOptions
    {
        /// <summary>
        /// <para>The maximum duration to wait for the NodeJS process to connect and to wait for responses to invocations.</para>
        /// <para>If this value is negative, the maximum duration is infinite.</para>
        /// <para>Defaults to 60000.</para>
        /// </summary>
        public int TimeoutMS { get; set; } = 60000;

        /// <summary>
        /// <para>The number of times an invocation is retried in the existing NodeJS process.</para>
        /// <para>If set to a negative value, invocations are retried indefinitely.</para>
        /// <para>If the module source of an invocation is an unseekable stream, the invocation is not retried.
        /// If you require retries for such streams, copy their contents to a <see cref="MemoryStream"/>.</para>
        /// <para>Defaults to 1.</para>
        /// </summary>
        public int NumRetries { get; set; } = 1;

        /// <summary>
        /// <para>The number of times we create a new NodeJS process to retry an invocation.</para>
        /// <para>Invocations are retried <see cref="NumRetries"/> times in the existing NodeJS process. Once <b>existing process retries</b> are exhausted,
        /// if any <b>process retries</b> remain, a new NodeJS process is created and invocations are retried <see cref="NumRetries"/> times in the new process.</para>
        /// <para>For example, consider the situation where <see cref="NumRetries"/> and this value are both 1. An invocation is first tried in the existing process.
        /// If it fails it is retried in the same process. If it fails again, a new process is created and the invocation is retried there once. In total, the
        /// invocation is attempted 3 times.</para>
        /// <para>If set to a negative value, new NodeJS processes are recreated for retries indefinitely.</para>
        /// <para>If the module source of an invocation is an unseekable stream, the invocation is not retried.
        /// If you require retries for such streams, copy their contents to a <see cref="MemoryStream"/>.</para>
        /// <para>Defaults to 1.</para>
        /// </summary>
        public int NumProcessRetries { get; set; } = 1;

        /// <summary>
        /// <para>The number of times a NodeJS connection attempt is retried.</para>
        /// <para>If set to a negative value, connection attempts are retried indefinitely.</para>
        /// <para>Defaults to 1.</para>
        /// </summary>
        public int NumConnectionRetries { get; set; } = 1;

        /// <summary>
        /// <para>The concurrency mode for invocations.</para>
        /// <para>By default, this value is <see cref="Concurrency.None"/> and invocations are executed synchronously by a single NodeJS process; 
        /// mode pros: lower memory overhead and supports all modules, cons: less performant.</para>
        /// <para>If this value is <see cref="Concurrency.MultiProcess"/>, <see cref="ConcurrencyDegree"/> NodeJS processes are created and invocations are
        /// distributed among them using round robin load balancing; mode pros: more performant, cons: higher memory overhead and doesn't work with modules that 
        /// have persistent state.</para>
        /// <para>Defaults to <see cref="Concurrency.None"/>.</para>
        /// </summary>
        public Concurrency Concurrency { get; set; }

        /// <summary>
        /// <para>The concurrency degree.</para>
        /// <para>If <see cref="Concurrency"/> is <see cref="Concurrency.MultiProcess"/>, this value is the number of NodeJS processes.</para>
        /// <para>If this value is less than or equal to 0, concurrency degree is the number of logical processors the current machine has.</para>
        /// <para>This value does nothing if <see cref="Concurrency"/> is <see cref="Concurrency.None"/>.</para>
        /// <para>Defaults to 0.</para>
        /// </summary>
        public int ConcurrencyDegree { get; set; }

        /// <summary>
        /// <para>The value specifying whether file watching is enabled.</para>
        /// <para>If file watching is enabled, when a file in <see cref="WatchPath"/> with name matching a pattern in <see cref="WatchFileNamePatterns"/> changes,
        /// NodeJS is restarted.</para>
        /// <para>Defaults to <c>false</c>.</para>
        /// </summary>
        public bool EnableFileWatching { get; set; }

        /// <summary>
        /// <para>The path of the directory to watch for file changes.</para>
        /// <para>If this value is <c>null</c>, the path <see cref="NodeJSProcessOptions.ProjectPath"/> is watched.</para>
        /// <para>This value does nothing if <see cref="EnableFileWatching"/> is <c>false</c>.</para>
        /// <para>Defaults to <c>null</c></para>
        /// </summary>
        public string WatchPath { get; set; }

        /// <summary>
        /// <para>The value specifying whether to watch subdirectories of <see cref="WatchPath"/>.</para>
        /// <para>This value does nothing if <see cref="EnableFileWatching"/> is <c>false</c>.</para>
        /// <para>Defaults to <c>true</c>.</para>
        /// </summary>
        public bool WatchSubdirectories { get; set; } = true;

        /// <summary>
        /// <para>The file name patterns to watch.</para>
        /// <para>In a pattern, "*" represents 0 or more of any character and "?" represents 0 or 1 of any character. For example,
        /// "TestFile1.js" matches the pattern "*File?.js".</para>
        /// <para>This value does nothing if <see cref="EnableFileWatching"/> is <c>false</c>.</para>
        /// <para>Defaults to "*.js", "*.jsx", "*.ts", "*.tsx", "*.json" and "*.html".</para>
        /// </summary>
        public IEnumerable<string> WatchFileNamePatterns { get; set; } = new[] { "*.js", "*.jsx", "*.ts", "*.tsx", "*.json", "*.html" };

        /// <summary>
        /// <para>The value specifying whether NodeJS processes shutdown gracefully when a file changes or an invocation is retried in a new process.</para>
        /// <para>If this value is true, NodeJS processes shutdown gracefully. Otherwise they're killed immediately.</para>
        /// <para>What's a graceful shutdown? When we create a new NodeJS process, the old NodeJS process
        /// might still be handling earlier invocations. If graceful shutdown is enabled, the old NodeJS process is killed <b>after</b> its
        /// invocations complete. If graceful shutdown is disabled, the old NodeJS process is killed immediately and existing
        /// invocations are retried in the new NodeJS process if they have remaining retries (see <see cref="NumRetries"/>).</para>
        /// <para>Should I use graceful shutdown? Shutting down gracefully is safer: chances of an invocation exhausting retries and failing is lower, also,
        /// you won't face issues from an invocation terminating midway. However, graceful shutdown does incur a small performance cost
        /// and invocations complete using the outdated version of your script. Weigh these factors for your script and use-case to decide whether to use graceful shutdown.</para>
        /// <para>This value does nothing if <see cref="EnableFileWatching"/> is <c>false</c> and <see cref="NumProcessRetries"/> is 0.</para>
        /// <para>Defaults to <c>true</c>.</para>
        /// </summary>
        public bool GracefulProcessShutdown { get; set; } = true;
    }
}
