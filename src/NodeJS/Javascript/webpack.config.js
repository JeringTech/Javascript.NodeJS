const path = require('path');

module.exports = env => {

    let mode = env.mode.toLowerCase() === 'release' ? 'production' : 'development'; // Default to development, production mode minifies scripts
    console.log(`Mode: ${mode}.`);

    return {
        mode: mode,
        target: 'node',
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
            libraryTarget: 'commonjs2',
            path: path.join(__dirname, 'bin', env.mode),
            filename: path.basename(env.entry, path.extname(env.entry)) + '.js'
        }
    };
}; 