using System.Net.Http;

namespace Jering.Javascript.NodeJS
{
    /// <summary>
    /// The default implementation of <see cref="IHttpContentFactory"/>.
    /// </summary>
    public class InvocationContentFactory : IHttpContentFactory
    {
        private readonly IJsonService _jsonService;

        /// <summary>
        /// Creates an <see cref="InvocationContentFactory"/> instance.
        /// </summary>
        /// <param name="jsonService"></param>
        public InvocationContentFactory(IJsonService jsonService)
        {
            _jsonService = jsonService;
        }

        /// <inheritdoc />
        public HttpContent Create(InvocationRequest invocationRequest)
        {
            return new InvocationContent(_jsonService, invocationRequest);
        }
    }
}
