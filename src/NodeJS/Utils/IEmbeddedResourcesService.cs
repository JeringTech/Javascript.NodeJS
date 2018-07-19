using System;
using System.IO;
using System.Reflection;

namespace Jering.JavascriptUtils.NodeJS
{
    public interface IEmbeddedResourcesService
    {
        /// <summary>
        /// Reads the embedded resource with the specified name from a given assembly as a string. When building a managed dll,
        /// the name of an embedded resource can be specified using the LogicalName attribute of the EmbeddedResource element.
        /// </summary>
        /// <param name="embeddedResourceAssembly">Assembly whose resource is to be read.</param>
        /// <param name="name">The name of the embedded resource to be read.</param>
        /// <returns>The contents of the resource as a string.</returns>
        string ReadAsString(Assembly embeddedResourceAssembly, string name);

        /// <summary>
        /// Reads the embedded resource with the specified name from a given assembly as a string. When building a managed dll,
        /// the name of an embedded resource can be specified using the LogicalName attribute of the EmbeddedResource element.
        /// </summary>
        /// <param name="typeFromEmbeddedResourceAssembly">Any <see cref="Type"/> in the assembly whose resource is to be read.</param>
        /// <param name="name">The name of the embedded resource to be read.</param>
        /// <returns>The contents of the resource as a string.</returns>
        string ReadAsString(Type typeFromEmbeddedResourceAssembly, string name);

        /// <summary>
        /// Reads the embedded resource with the specified name from a given assembly as a stream. When building a managed dll,
        /// the name of an embedded resource can be specified using the LogicalName attribute of the EmbeddedResource element.
        /// </summary>
        /// <param name="embeddedResourceAssembly">Assembly whose resource is to be read.</param>
        /// <param name="name">The name of the embedded resource to be read.</param>
        /// <returns>The contents of the resource as a stream.</returns>
        Stream ReadAsStream(Assembly embeddedResourceAssembly, string name);

        /// <summary>
        /// Reads the embedded resource with the specified name from a given assembly as a stream. When building a managed dll,
        /// the name of an embedded resource can be specified using the LogicalName attribute of the EmbeddedResource element.
        /// </summary>
        /// <param name="typeFromEmbeddedResourceAssembly">Any <see cref="Type"/> in the assembly whose resource is to be read.</param>
        /// <param name="name">The name of the embedded resource to be read.</param>
        /// <returns>The contents of the resource as a stream.</returns>
        Stream ReadAsStream(Type typeFromEmbeddedResourceAssembly, string name);
    }
}
