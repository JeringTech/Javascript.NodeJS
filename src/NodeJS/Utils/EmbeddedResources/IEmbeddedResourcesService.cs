using System;
using System.IO;
using System.Reflection;

namespace Jering.Javascript.NodeJS
{
    /// <summary>
    /// An abstraction for reading of embedded resources.
    /// </summary>
    public interface IEmbeddedResourcesService
    {
        /// <summary>
        /// <para>Reads the embedded resource with the specified name from a given assembly, as a string.</para>
        /// <para>When building a managed dll,
        /// the name of an embedded resource can be specified using the LogicalName attribute of the EmbeddedResource element.</para>
        /// </summary>
        /// <param name="embeddedResourceAssembly">The assembly that contains the embedded resource.</param>
        /// <param name="name">The name of the embedded resource to be read.</param>
        /// <returns>The contents of the resource as a string.</returns>
        /// <exception cref="InvalidOperationException">Thrown if there is no embedded resource with the specified name.</exception>
        string ReadAsString(Assembly embeddedResourceAssembly, string name);

        /// <summary>
        /// <para>Reads the embedded resource with the specified name from a given assembly as a string.</para>
        /// <para>When building a managed dll,
        /// the name of an embedded resource can be specified using the LogicalName attribute of the EmbeddedResource element.</para>
        /// </summary>
        /// <param name="typeFromEmbeddedResourceAssembly">Any <see cref="Type"/> in the assembly that contains the embedded resource.</param>
        /// <param name="name">The name of the embedded resource to be read.</param>
        /// <returns>The contents of the resource as a string.</returns>
        /// <exception cref="InvalidOperationException">Thrown if there is no embedded resource with the specified name.</exception>
        string ReadAsString(Type typeFromEmbeddedResourceAssembly, string name);

        /// <summary>
        /// <para>Reads the embedded resource with the specified name from a given assembly as a stream.</para>
        /// <para>When building a managed dll,
        /// the name of an embedded resource can be specified using the LogicalName attribute of the EmbeddedResource element.</para>
        /// </summary>
        /// <param name="embeddedResourceAssembly">The assembly that contains the embedded resource.</param>
        /// <param name="name">The name of the embedded resource to be read.</param>
        /// <returns>The contents of the resource as a stream.</returns>
        /// <exception cref="InvalidOperationException">Thrown if there is no embedded resource with the specified name.</exception>
        Stream ReadAsStream(Assembly embeddedResourceAssembly, string name);

        /// <summary>
        /// <para>Reads the embedded resource with the specified name from a given assembly as a stream.</para> 
        /// <para>When building a managed dll,
        /// the name of an embedded resource can be specified using the LogicalName attribute of the EmbeddedResource element.</para>
        /// </summary>
        /// <param name="typeFromEmbeddedResourceAssembly">Any <see cref="Type"/> in the assembly that contains the embedded resource.</param>
        /// <param name="name">The name of the embedded resource to be read.</param>
        /// <returns>The contents of the resource as a stream.</returns>
        /// <exception cref="InvalidOperationException">Thrown if there is no embedded resource with the specified name.</exception>
        Stream ReadAsStream(Type typeFromEmbeddedResourceAssembly, string name);
    }
}
