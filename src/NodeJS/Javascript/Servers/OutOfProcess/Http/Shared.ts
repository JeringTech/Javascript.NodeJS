const path = require("path");
import * as stream from 'stream';
import * as http from 'http';
import * as http2 from 'http2';
import InvocationRequest from "../../../InvocationData/InvocationRequest";

export function setup(): [args: { [key: string]: string }, projectDir: string, moduleResolutionPaths: string[]] {
    // Parse arguments
    const args: { [key: string]: string } = parseArgs(process.argv);

    // Overwrite writing to output streams
    demarcateMessageEndings(process.stdout);
    demarcateMessageEndings(process.stderr);

    // Start auto-termination loop
    exitWhenParentExits(parseInt(args.parentPid), true, 1000);

    // Set by NodeJSProcessFactory
    const projectDir = process.cwd();
    const moduleResolutionPaths = generateModuleResolutionPaths(projectDir);

    // Patch lstat - issue explained in this comment: https://github.com/aspnet/JavaScriptServices/issues/1101#issue-241971678
    patchLStat(projectDir);

    return [args, projectDir, moduleResolutionPaths];
}

export function respondWithError(res: http.ServerResponse | http2.Http2ServerResponse, error: Error | string) {
    const errorIsString: boolean = typeof error === 'string';

    res.statusCode = 500;
    res.end(JSON.stringify({
        errorMessage: errorIsString ? error : (error as Error).message,
        errorStack: errorIsString ? null : (error as Error).stack
    }));
}

export function getTempIdentifier(invocationRequest: InvocationRequest): string {
    if (invocationRequest.cacheIdentifier == null) {
        return `"${invocationRequest.moduleSource.substring(0, 25)}..."`;
    } else {
        return invocationRequest.cacheIdentifier;
    }
}

function generateModuleResolutionPaths(projectDirectory: string): string[] {
    const result: string[] = [path.join(projectDirectory, 'node_modules')];
    let directory: string = projectDirectory;
    let lastDirectory: string;

    while ((directory = path.dirname(directory)) !== lastDirectory) { // Once we reach root directory, path.dirname(root directory) returns root directory. E.g. path.dirname('C:/') returns 'C:/'
        result.push(path.join(directory, 'node_modules'));
        lastDirectory = directory;
    }

    return result;
}

// https://github.com/aspnet/JavaScriptServices/blob/0dc570a0c8725e3031ce5a884d7df3cfb75545ba/src/Microsoft.AspNetCore.NodeServices/TypeScript/Util/ArgsUtil.ts
function parseArgs(args: string[]) {
    let currentKey = null;
    const result: any = {};
    args.forEach(arg => {
        if (arg.indexOf('--') === 0) {
            const argName = arg.substring(2);
            result[argName] = undefined;
            currentKey = argName;
        } else if (currentKey !== null) {
            result[currentKey] = arg;
            currentKey = null;
        }
    })

    return result;
}

// https://github.com/aspnet/JavaScriptServices/blob/0dc570a0c8725e3031ce5a884d7df3cfb75545ba/src/Microsoft.AspNetCore.NodeServices/TypeScript/Util/ExitWhenParentExits.ts
function exitWhenParentExits(parentPid: number, ignoreSigint: boolean, pollIntervalMS: number) {
    setInterval(() => {
        if (!processExists(parentPid)) {
            console.log(`[Node.js HTTP server] Parent process (pid: ${parentPid}) exited. Exiting this process...`);
            process.exit();
        }
    }, pollIntervalMS);

    if (ignoreSigint) {
        process.on('SIGINT', () => {
            console.log('[Node.js HTTP server] Received SIGINT. Waiting for .NET process to exit...');
        });
    }
}

// https://github.com/aspnet/JavaScriptServices/blob/0dc570a0c8725e3031ce5a884d7df3cfb75545ba/src/Microsoft.AspNetCore.NodeServices/TypeScript/Util/ExitWhenParentExits.ts
function processExists(pid: number) {
    try {
        process.kill(pid, 0);
        return true;
    } catch (ex) {
        if (ex.code === 'EPERM') {
            throw new Error(`Attempted to check whether process ${pid} was running, but got a permissions error.`);
        }
        return false;
    }
}

// https://github.com/aspnet/JavaScriptServices/blob/4763ad5b8c0575f030a3cac8518767f4bd192c9b/src/Microsoft.AspNetCore.NodeServices/TypeScript/Util/PatchModuleResolutionLStat.ts
function patchLStat(projectDir: string) {
    function patchedLStat(pathToStatLong: string, fsReqWrap?: any) {
        try {
            // If the lstat completes without errors, we don't modify its behavior at all
            return origLStat.apply(this, arguments);
        } catch (ex) {
            const shouldOverrideError =
                ex.message.startsWith('EPERM')                     // It's a permissions error
                && typeof appRootDirLong === 'string'
                && appRootDirLong.startsWith(pathToStatLong)       // ... for an ancestor directory
                && ex.stack.indexOf('Object.realpathSync ') >= 0;   // ... during symlink resolution
            if (shouldOverrideError) {
                // Fake the result to give the same result as an 'lstat' on the app root dir.
                // This stops Node failing to load modules just because it doesn't know whether
                // ancestor directories are symlinks or not. If there's a genuine file
                // permissions issue, it will still surface later when Node actually
                // tries to read the file.
                return origLStat.call(this, projectDir, fsReqWrap);
            } else {
                // In any other case, preserve the original error
                throw ex;
            }
        }
    }

    // It's only necessary to apply this workaround on Windows
    let appRootDirLong: string = null;
    let origLStat: Function = null;
    if (/^win/.test(process.platform)) {
        try {
            // Get the app's root dir in Node's internal "long" format (e.g., \\?\C:\dir\subdir)
            appRootDirLong = (path as any)._makeLong(projectDir);

            // Actually apply the patch, being as defensive as possible
            const bindingFs = (process as any).binding('fs');
            origLStat = bindingFs.lstat;
            if (typeof origLStat === 'function') {
                bindingFs.lstat = patchedLStat;
            }
        } catch (ex) {
            // If some future version of Node throws (e.g., to prevent use of process.binding()),
            // don't apply the patch, but still let the application run.
        }
    }
}

function demarcateMessageEndings(outputStream: NodeJS.WritableStream) {
    const origWriteFunction = outputStream.write;
    outputStream.write = <any>function (value: any) {
        // Only interfere with the write if it's definitely a string
        if (typeof value === 'string') {
            // Node appends a new line character at the end of the message. This facilitates reading of the stream: the process reading it reads messages line by line -
            // characters stay in its buffer until a new line character is received. This means that the null terminating character must appear before the last
            // new line character of the message. This approach is inefficent since it allocates 2 strings.
            //
            // TODO consider sending '\0\n' as a demarcator after sending value (profile). Also need to check if logging from worker threads might cause
            // concurrency issues.
            arguments[0] = value.slice(0, value.length - 1) + '\0\n';
        }

        origWriteFunction.apply(this, arguments);
    };
}