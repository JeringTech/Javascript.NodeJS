import * as path from 'path';
import { fileURLToPath } from 'url';

const __dirname = path.dirname(fileURLToPath(import.meta.url));

export default env => {

    let mode = env.mode.toLowerCase() === 'release' ? 'production' : 'development'; // Default to development, production mode minifies scripts
    console.log(`Mode: ${mode}.`);

    return {
        mode: mode,
        target: 'node14',
        resolve: {
            extensions: ['.ts', '.js']
        },
        module: {
            rules: [
                { test: /\.ts$/, loader: 'ts-loader' }
            ]
        },
        entry: env.entry,
        output: {
            hashFunction: 'xxhash64',
            library: {
                type: 'module'
            },
            path: path.join(__dirname, 'bin', env.mode),
            filename: path.basename(env.entry, path.extname(env.entry)) + '.js'
        },
        experiments: {
            outputModule: true
        }
    };
}; 
