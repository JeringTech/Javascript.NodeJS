// Performs a real-world operation (consider adding more)
const prismjs = require('prismjs');
require('prismjs/components/prism-csharp');

module.exports = (callback, code) => {
    var result = prismjs.highlight(code, prismjs.languages.csharp, 'csharp');

    callback(null, result);
};
