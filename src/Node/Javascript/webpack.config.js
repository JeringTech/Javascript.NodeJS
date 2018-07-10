const Path = require('path');

module.exports = env => {

    let mode = env.mode.toLowerCase() === 'release' ? 'production' : 'development'; // Default to development, production mode minifies scripts
    console.log(`Mode: ${mode}.`);

    return {
        mode: mode,
        target: 'node',
        resolve: {
            extensions: ['.ts']
        },
        module: {
            rules: [
                { test: /\.ts$/, loader: 'ts-loader' }
            ]
        },
        entry: './Http/HttpServer.ts',
        output: {
            libraryTarget: 'commonjs2',
            path: Path.join(__dirname, 'bin', env.mode),
            filename: '[name].js'
        }
    };
};