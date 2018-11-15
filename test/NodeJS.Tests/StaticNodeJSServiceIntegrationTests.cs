using System.IO;
using Xunit;

namespace Jering.Javascript.NodeJS.Tests
{
    public class StaticNodeJSServiceIntegrationTests
    {
        [Fact]
        public async void Configure_ConfiguresOptions()
        {
            // Arrange
            const string dummyTestVariableName1 = "TEST_VARIABLE_1";
            const string dummyTestVariableValue1 = "testVariableValue1";
            const string dummyTestVariableName2 = "TEST_VARIABLE_2";
            const string dummyTestVariableValue2 = "testVariableValue2";
            StaticNodeJSService.
                    Configure<NodeJSProcessOptions>(options => options.EnvironmentVariables.Add(dummyTestVariableName1, dummyTestVariableValue1));
            StaticNodeJSService.
                    Configure<NodeJSProcessOptions>(options => options.EnvironmentVariables.Add(dummyTestVariableName2, dummyTestVariableValue2));

            // Assert
            DummyResult result = await StaticNodeJSService.
                InvokeFromStringAsync<DummyResult>($"module.exports = (callback) => callback(null, {{result: process.env.{dummyTestVariableName1} + process.env.{dummyTestVariableName2}}});").
                ConfigureAwait(false);
            Assert.Equal(dummyTestVariableValue1 + dummyTestVariableValue2, result.Result);
        }

        [Fact]
        public async void TryInvokeFromCacheAsync_InvokesJavascriptIfModuleIsCached()
        {
            // Arrange
            const string dummyResultString = "success";
            const string dummyCacheIdentifier = "dummyCacheIdentifier";

            // Cache
            await StaticNodeJSService.
                InvokeFromStringAsync<DummyResult>("module.exports = (callback, resultString) => callback(null, {result: resultString});",
                    dummyCacheIdentifier,
                    args: new[] { dummyResultString }).
                ConfigureAwait(false);

            // Act
            (bool success, DummyResult value) = await StaticNodeJSService.TryInvokeFromCacheAsync<DummyResult>(dummyCacheIdentifier, args: new[] { dummyResultString }).ConfigureAwait(false);

            // Assert
            Assert.True(success);
            Assert.Equal(dummyResultString, value.Result);
        }

        [Fact]
        public async void TryInvokeFromCacheAsync_ReturnsFalseIfModuleIsNotCached()
        {
            // Arrange
            const string dummyResultString = "success";
            const string dummyCacheIdentifier = "dummyCacheIdentifier";

            // Act
            (bool success, DummyResult value) = await StaticNodeJSService.TryInvokeFromCacheAsync<DummyResult>(dummyCacheIdentifier, args: new[] { dummyResultString }).ConfigureAwait(false);

            // Assert
            Assert.False(success);
            Assert.Null(value);
        }

        [Fact]
        public async void InvokeFromStreamAsync_InvokesJavascript()
        {
            // Arrange
            const string dummyResultString = "success";

            DummyResult result;
            using (var memoryStream = new MemoryStream())
            using (var streamWriter = new StreamWriter(memoryStream))
            {
                streamWriter.Write("module.exports = (callback, resultString) => callback(null, {result: resultString});");
                streamWriter.Flush();
                memoryStream.Position = 0;

                // Act
                result = await StaticNodeJSService.InvokeFromStreamAsync<DummyResult>(memoryStream, args: new[] { dummyResultString }).ConfigureAwait(false);
            }

            // Assert
            Assert.Equal(dummyResultString, result.Result);
        }

        [Fact]
        public async void InvokeFromStringAsync_InvokesJavascript()
        {
            // Arrange
            const string dummyResultString = "success";

            // Act
            DummyResult result = await StaticNodeJSService.
                InvokeFromStringAsync<DummyResult>("module.exports = (callback, resultString) => callback(null, {result: resultString});", args: new[] { dummyResultString }).ConfigureAwait(false);

            // Assert
            Assert.Equal(dummyResultString, result.Result);
        }

        [Fact]
        public async void InvokeFromFileAsync_InvokesJavascript()
        {
            const string dummyResultString = "success";

            // Act
            DummyResult result = await StaticNodeJSService.
                InvokeFromFileAsync<DummyResult>("dummyModule.js", args: new[] { dummyResultString }).ConfigureAwait(false);

            // Assert
            Assert.Equal(dummyResultString, result.Result);
        }

        private class DummyResult
        {
            public string Result { get; set; }
        }
    }
}
