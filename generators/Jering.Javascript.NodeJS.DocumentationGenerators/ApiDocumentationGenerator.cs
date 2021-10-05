using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web;
using System.Xml;
using System.Xml.Linq;

#nullable enable

namespace Jering.Javascript.NodeJS.DocumentationGenerators
{
    /// <summary>
    /// <para>Generates API documenation and inserts it into ReadMe.</para>
    /// <para>To add API documentation for a type to ReadMe, simply add a "&lt;!-- typeName generated docs --&gt;&lt;!-- typeName generated docs --&gt;" pair.</para>
    /// <para>The generator locates all "&lt;!-- typeName generated docs --&gt;&lt;!-- typeName generated docs --&gt;" pairs in ReadMe.
    /// It extracts typeNames, using them to retrieve type metadata from the compilation's public types. The metadata is used to generate API documentation for the types,
    /// which is then inserted within "&lt;!-- typeName generated docs --&gt;&lt;!-- typeName generated docs --&gt;" pairs.</para>
    /// </summary>
    [Generator]
    public class ApiDocumentationGenerator : SourceGenerator<ApiDocumentationGeneratorSyntaxReceiver>
    {
        private static readonly DiagnosticDescriptor _missingClassDeclaration = new("G0013",
            "Missing class declaration",
            "Missing class declaration for: \"{0}\"",
            "Code generation",
            DiagnosticSeverity.Error,
            true);
        private static readonly DiagnosticDescriptor _missingInterfaceDeclaration = new("G0014",
            "Missing interface declaration",
            "Missing interface declaration for: \"{0}\"",
            "Code generation",
            DiagnosticSeverity.Error,
            true);
        private const string RELATIVE_README_FILE_PATH = "../../ReadMe.md";
        private static string _readMeFilePath = string.Empty;


        protected override void InitializeCore()
        {
            // https://docs.microsoft.com/en-sg/visualstudio/releases/2019/release-notes-preview#--visual-studio-2019-version-1610-preview-2-
            //Debugger.Launch();
        }

        protected override void ExecuteCore(ref GeneratorExecutionContext context)
        {
            CancellationToken cancellationToken = context.CancellationToken;
            if (cancellationToken.IsCancellationRequested) return;

            // Get syntax receiver
            if (context.SyntaxReceiver is not ApiDocumentationGeneratorSyntaxReceiver apiDocumentationGeneratorSyntaxReceiver)
            {
                return;
            }

            // Parse readme
            if (_readMeFilePath == string.Empty)
            {
                _readMeFilePath = Path.Combine(_projectDirectory, RELATIVE_README_FILE_PATH);
            }

            string readMeContents;
            lock (this)
            {
                if (!File.Exists(_readMeFilePath))
                {
                    return; // Project has no readme
                }

                readMeContents = File.ReadAllText(_readMeFilePath);
            }
            MatchCollection matches = Regex.Matches(readMeContents, @"<!--\s+(.*?)\s+generated\s+docs\s+--(>).*?(<)!--\s+\1\s+generated\s+docs\s+-->", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            if (matches.Count == 0)
            {
                return; // No types to generate docs for
            }
            List<(int indexBeforeDocs, int indexAfterDocs, string typeName)> generatedDocsTypes = new();
            foreach (Match match in matches)
            {
                generatedDocsTypes.Add((match.Groups[2].Index, match.Groups[3].Index, match.Groups[1].Value));
            }

            // Generate docs
            StringBuilder stringBuilder = new();
            int nextStartIndex = 0;
            foreach ((int indexBeforeDocs, int indexAfterDocs, string typeName) in generatedDocsTypes)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                stringBuilder.
                    Append(readMeContents, nextStartIndex, indexBeforeDocs - nextStartIndex + 1).
                    Append("\n\n");

                if (typeName.StartsWith("I"))
                {
                    if (!apiDocumentationGeneratorSyntaxReceiver.PublicInterfaceDeclarations.TryGetValue(typeName, out InterfaceDeclarationSyntax interfaceDeclarationSyntax))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(_missingInterfaceDeclaration, null, typeName));
                        continue;
                    }

                    stringBuilder.AppendInterfaceDocumentation(interfaceDeclarationSyntax, ref context);
                }
                else
                {
                    if (!apiDocumentationGeneratorSyntaxReceiver.PublicClassDeclarations.TryGetValue(typeName, out ClassDeclarationSyntax classDeclarationSyntax))
                    {
                        context.ReportDiagnostic(Diagnostic.Create(_missingClassDeclaration, null, typeName));
                        continue;
                    }

                    stringBuilder.AppendClassDocumentation(classDeclarationSyntax, ref context);
                }

                nextStartIndex = indexAfterDocs;
            }
            stringBuilder.Append(readMeContents, nextStartIndex, readMeContents.Length - nextStartIndex);

            // Update file
            string newReadMeContents = stringBuilder.ToString();
            if (cancellationToken.IsCancellationRequested || newReadMeContents == readMeContents)
            {
                return;
            }

            File.WriteAllText(_readMeFilePath, newReadMeContents);
        }
    }

    public static class StringBuilderExtensions
    {
        private static readonly DiagnosticDescriptor _missingTypeSymbol = new("G0015",
            "Missing type symbol",
            "Missing type symbol for: \"{0}\"",
            "Code generation",
            DiagnosticSeverity.Error,
            true);
        private static readonly DiagnosticDescriptor _missingMemberSymbol = new("G0016",
            "Missing member symbol",
            "Missing member symbol for: \"{0}\"",
            "Code generation",
            DiagnosticSeverity.Error,
            true);
        private static readonly DiagnosticDescriptor _crefValueWithUnexpectedPrefix = new("G0017",
            "Cref value with unexpected prefix",
            "Cref value with unexpected prefix: \"{0}\"",
            "Code generation",
            DiagnosticSeverity.Error,
            true);

        public static StringBuilder AppendInterfaceDocumentation(this StringBuilder stringBuilder, InterfaceDeclarationSyntax interfaceDeclarationSyntax, ref GeneratorExecutionContext context)
        {
            Compilation compilation = context.Compilation;
            SemanticModel semanticModel = compilation.GetSemanticModel(interfaceDeclarationSyntax.SyntaxTree);

            // Interface title
            INamedTypeSymbol? interfaceSymbol = semanticModel.GetDeclaredSymbol(interfaceDeclarationSyntax);
            if (interfaceSymbol == null)
            {
                return stringBuilder;
            }
            stringBuilder.
                Append("### ").
                Append(ToHtmlEncodedDisplayString(interfaceSymbol, DisplayFormats.TypeTitleDisplayFormat)).
                AppendLine(" Interface");

            // Members
            IEnumerable<ISymbol> publicMemberSymbols = interfaceSymbol.GetMembers().Where(memberSymbol => memberSymbol.DeclaredAccessibility == Accessibility.Public);
            if (publicMemberSymbols.Count() == 0)
            {
                return stringBuilder;
            }

            return stringBuilder.
                AppendProperties(publicMemberSymbols, compilation, ref context).
                AppendOrdinaryMethods(publicMemberSymbols, compilation, ref context);
        }

        public static StringBuilder AppendClassDocumentation(this StringBuilder stringBuilder, ClassDeclarationSyntax classDeclarationSyntax, ref GeneratorExecutionContext context)
        {
            Compilation compilation = context.Compilation;
            SemanticModel semanticModel = compilation.GetSemanticModel(classDeclarationSyntax.SyntaxTree);

            // Class title
            INamedTypeSymbol? classSymbol = semanticModel.GetDeclaredSymbol(classDeclarationSyntax);
            if (classSymbol == null)
            {
                return stringBuilder;
            }
            stringBuilder.
                Append("### ").
                Append(ToHtmlEncodedDisplayString(classSymbol, DisplayFormats.TypeTitleDisplayFormat)).
                AppendLine(" Class");

            // Members
            IEnumerable<ISymbol> publicMemberSymbols = classSymbol.GetMembers().Where(memberSymbol => memberSymbol.DeclaredAccessibility == Accessibility.Public);
            if (publicMemberSymbols.Count() == 0)
            {
                return stringBuilder;
            }

            // Members - Constructors
            IEnumerable<IMethodSymbol> publicMethodSymbols = publicMemberSymbols.OfType<IMethodSymbol>();
            IEnumerable<IMethodSymbol> publicConstructorSymbols = publicMethodSymbols.Where(methodSymbol => methodSymbol.MethodKind == MethodKind.Constructor);
            if (publicConstructorSymbols.Count() > 0)
            {
                stringBuilder.AppendLine(@"#### Constructors");

                foreach (IMethodSymbol constructorSymbol in publicConstructorSymbols)
                {
                    XElement? rootXmlElement = TryGetXmlDocumentationRootElement(constructorSymbol);

                    stringBuilder.
                        AppendMemberTitle(constructorSymbol, DisplayFormats.ConstructorTitleDisplayFormat).
                        AppendSummary(rootXmlElement, compilation, ref context).
                        AppendSignature(constructorSymbol).
                        AppendParameters(constructorSymbol, rootXmlElement, compilation, ref context).
                        AppendRemarks(rootXmlElement, compilation, ref context);
                }
            }

            return stringBuilder.
                AppendProperties(publicMemberSymbols, compilation, ref context).
                AppendOrdinaryMethods(publicMethodSymbols, compilation, ref context);
        }

        public static StringBuilder AppendProperties(this StringBuilder stringBuilder, IEnumerable<ISymbol> symbols, Compilation compilation, ref GeneratorExecutionContext context)
        {
            IEnumerable<IPropertySymbol> propertySymbols = symbols.OfType<IPropertySymbol>();
            if (propertySymbols.Count() == 0)
            {
                return stringBuilder;
            }

            stringBuilder.AppendLine(@"#### Properties");

            foreach (IPropertySymbol propertySymbol in propertySymbols)
            {
                XElement? rootXmlElement = TryGetXmlDocumentationRootElement(propertySymbol);

                stringBuilder.
                    AppendMemberTitle(propertySymbol, DisplayFormats.propertyTitleDisplayFormat).
                    AppendSummary(rootXmlElement, compilation, ref context).
                    AppendSignature(propertySymbol).
                    AppendRemarks(rootXmlElement, compilation, ref context);
            }

            return stringBuilder;
        }

        public static StringBuilder AppendOrdinaryMethods(this StringBuilder stringBuilder, IEnumerable<ISymbol> symbols, Compilation compilation, ref GeneratorExecutionContext context)
        {
            IEnumerable<IMethodSymbol> ordinaryMethodSymbols = symbols.OfType<IMethodSymbol>().Where(methodSymbol => methodSymbol.MethodKind == MethodKind.Ordinary);
            if (ordinaryMethodSymbols.Count() == 0)
            {
                return stringBuilder;
            }

            stringBuilder.AppendLine(@"#### Methods");

            foreach (IMethodSymbol ordinaryMethodSymbol in ordinaryMethodSymbols)
            {
                XElement? rootXmlElement = TryGetXmlDocumentationRootElement(ordinaryMethodSymbol);

                stringBuilder.
                    AppendMemberTitle(ordinaryMethodSymbol, DisplayFormats.ordinaryMethodTitleDisplayFormat).
                    AppendSummary(rootXmlElement, compilation, ref context).
                    AppendSignature(ordinaryMethodSymbol).
                    AppendTypeParameters(ordinaryMethodSymbol, rootXmlElement, compilation, ref context).
                    AppendParameters(ordinaryMethodSymbol, rootXmlElement, compilation, ref context).
                    AppendReturns(rootXmlElement, compilation, ref context).
                    AppendExceptions(rootXmlElement, compilation, ref context).
                    AppendRemarks(rootXmlElement, compilation, ref context).
                    AppendExample(rootXmlElement, compilation, ref context);
            }

            return stringBuilder;
        }

        public static StringBuilder AppendMemberTitle(this StringBuilder stringBuilder, ISymbol symbol, SymbolDisplayFormat displayFormat)
        {
            return stringBuilder.
                Append("##### ").
                AppendLine(ToHtmlEncodedDisplayString(symbol, displayFormat));
        }

        public static StringBuilder AppendSummary(this StringBuilder stringBuilder, XElement? rootXmlElement, Compilation compilation, ref GeneratorExecutionContext context)
        {
            if (rootXmlElement == null)
            {
                return stringBuilder;
            }

            return stringBuilder.AppendXmlDocumentation(rootXmlElement.Element("summary"), compilation, ref context);
        }

        public static StringBuilder AppendExceptions(this StringBuilder stringBuilder, XElement? rootXmlElement, Compilation compilation, ref GeneratorExecutionContext context)
        {
            if (rootXmlElement == null)
            {
                return stringBuilder;
            }

            IEnumerable<XElement> exceptionXmlElements = rootXmlElement.Elements("exception");

            if (exceptionXmlElements.Count() == 0)
            {
                return stringBuilder;
            }
            stringBuilder.AppendLine("###### Exceptions");

            foreach (XElement exceptionXmlElement in exceptionXmlElements)
            {
                string? crefValue = exceptionXmlElement.Attribute("cref")?.Value;
                if (crefValue == null)
                {
                    continue;
                }

                int indexOfLastSeparator = crefValue.LastIndexOf('.');
                string exceptionName = crefValue.Substring(indexOfLastSeparator + 1);

                stringBuilder.
                    Append('`').
                    Append(exceptionName).
                    AppendLine("`  ").
                    AppendXmlDocumentation(exceptionXmlElement, compilation, ref context).
                    AppendLine();
            }

            return stringBuilder;
        }

        public static StringBuilder AppendReturns(this StringBuilder stringBuilder, XElement? rootXmlElement, Compilation compilation, ref GeneratorExecutionContext context)
        {
            if (rootXmlElement == null)
            {
                return stringBuilder;
            }

            XElement returnsXmlElement = rootXmlElement.Element("returns");

            if (returnsXmlElement == null)
            {
                return stringBuilder;
            }

            return stringBuilder.
                AppendLine("###### Returns").
                AppendXmlDocumentation(returnsXmlElement, compilation, ref context);
        }

        public static StringBuilder AppendRemarks(this StringBuilder stringBuilder, XElement? rootXmlElement, Compilation compilation, ref GeneratorExecutionContext context)
        {
            if (rootXmlElement == null)
            {
                return stringBuilder;
            }

            XElement remarksXmlElement = rootXmlElement.Element("remarks");

            if (remarksXmlElement == null)
            {
                return stringBuilder;
            }

            return stringBuilder.
                AppendLine("###### Remarks").
                AppendXmlDocumentation(remarksXmlElement, compilation, ref context);
        }

        public static StringBuilder AppendExample(this StringBuilder stringBuilder, XElement? rootXmlElement, Compilation compilation, ref GeneratorExecutionContext context)
        {
            if (rootXmlElement == null)
            {
                return stringBuilder;
            }

            XElement exampleXmlElement = rootXmlElement.Element("example");

            if (exampleXmlElement == null)
            {
                return stringBuilder;
            }

            return stringBuilder.
                AppendLine("###### Example").
                AppendXmlDocumentation(exampleXmlElement, compilation, ref context);
        }

        public static StringBuilder AppendTypeParameters(this StringBuilder stringBuilder, IMethodSymbol methodSymbol, XElement? rootXmlElement, Compilation compilation, ref GeneratorExecutionContext context)
        {
            ImmutableArray<ITypeParameterSymbol> typeParameterSymbols = methodSymbol.TypeParameters;
            if (typeParameterSymbols.Length == 0)
            {
                return stringBuilder;
            }

            IEnumerable<XElement>? typeparamXmlElements = null;
            if (rootXmlElement != null)
            {
                typeparamXmlElements = rootXmlElement.Elements("typeparam");
            }

            stringBuilder.AppendLine("###### Type Parameters");
            foreach (ITypeParameterSymbol typeParameterSymbol in typeParameterSymbols)
            {
                string typeParameterName = typeParameterSymbol.Name;

                stringBuilder.
                    Append('`').
                    Append(typeParameterName).
                    AppendLine("`  ");

                if (typeparamXmlElements != null)
                {
                    XElement? paramXmlElement = typeparamXmlElements.FirstOrDefault(paramXmlElement => paramXmlElement.Attribute("name").Value == typeParameterName);

                    if (paramXmlElement != null)
                    {
                        stringBuilder.AppendXmlDocumentation(paramXmlElement, compilation, ref context);
                    }
                }

                stringBuilder.Append('\n'); // New paragraph for each parameter
            }

            stringBuilder.Length -= 1; // Remove last \n

            return stringBuilder;
        }

        public static StringBuilder AppendParameters(this StringBuilder stringBuilder, IMethodSymbol methodSymbol, XElement? rootXmlElement, Compilation compilation, ref GeneratorExecutionContext context)
        {
            ImmutableArray<IParameterSymbol> parameterSymbols = methodSymbol.Parameters;
            if (parameterSymbols.Length == 0)
            {
                return stringBuilder;
            }

            IEnumerable<XElement>? paramXmlElements = null;
            if (rootXmlElement != null)
            {
                paramXmlElements = rootXmlElement.Elements("param");
            }

            stringBuilder.AppendLine("###### Parameters");
            foreach (IParameterSymbol parameterSymbol in parameterSymbols)
            {
                string parameterName = parameterSymbol.Name;

                stringBuilder.
                    Append(parameterName).
                    Append(" `").
                    Append(parameterSymbol.Type.ToDisplayString(DisplayFormats.TypeInlineDisplayFormat)).
                    AppendLine("`  ");

                if (paramXmlElements != null)
                {
                    XElement? paramXmlElement = paramXmlElements.FirstOrDefault(paramXmlElement => paramXmlElement.Attribute("name").Value == parameterName);

                    if (paramXmlElement != null)
                    {
                        stringBuilder.AppendXmlDocumentation(paramXmlElement, compilation, ref context);
                    }
                }

                stringBuilder.Append('\n'); // New paragraph for each parameter
            }

            stringBuilder.Length -= 1; // Remove last \n

            return stringBuilder;
        }

        public static StringBuilder AppendSignature(this StringBuilder stringBuilder, ISymbol symbol)
        {
            return stringBuilder.
                AppendLine("```csharp").
                AppendLine(symbol.ToDisplayString(DisplayFormats.SignatureDisplayFormat)).
                AppendLine("```");
        }

        public static StringBuilder AppendXmlDocumentation(this StringBuilder stringBuilder, XNode xNode, Compilation compilation, ref GeneratorExecutionContext context)
        {
            if (xNode == null)
            {
                return stringBuilder;
            }

            stringBuilder.AppendXmlNodeContents(xNode, compilation, false, ref context);
            stringBuilder.TrimEnd();
            stringBuilder.AppendLine("  ");

            return stringBuilder;
        }

        public static void AppendXmlNodeContents(this StringBuilder stringBuilder, XNode xNode, Compilation compilation, bool decodeHtml, ref GeneratorExecutionContext context)
        {
            if (xNode.NodeType == XmlNodeType.Text)
            {
                string nodeText = xNode.ToString();

                if (decodeHtml)
                {
                    nodeText = HttpUtility.HtmlDecode(nodeText);
                }

                stringBuilder.Append(nodeText);
                return;
            }

            if (xNode.NodeType != XmlNodeType.Element)
            {
                return;
            }

            var xElement = (XElement)xNode;
            XName elementName = xElement.Name;

            if (elementName == "see")
            {
                string? crefValue = xElement.Attribute("cref")?.Value;

                if (crefValue == null)
                {
                    return;
                }

                ISymbol? seeSymbol;
                SymbolDisplayFormat? displayFormat;
                if (crefValue.StartsWith("T:"))
                {
                    string typeFullyQualifiedName = crefValue.Substring(2); // Drop "T:" prefix
                    seeSymbol = compilation.GetTypeByMetadataName(typeFullyQualifiedName);

                    if (seeSymbol == null)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(_missingTypeSymbol, null, typeFullyQualifiedName));
                        return;
                    }

                    displayFormat = DisplayFormats.TypeInlineDisplayFormat;
                }
                else if (crefValue.StartsWith("M:") || crefValue.StartsWith("P:") || crefValue.StartsWith("F:")) // Method, field or property
                {
                    int indexOfLastSeparator = GetLastIndexOfFullyQualifiedTypeName(crefValue) + 1;
                    string typeFullyQualifiedName = crefValue.Substring(2, indexOfLastSeparator - 2);
                    INamedTypeSymbol? typeSymbol = compilation.GetTypeByMetadataName(typeFullyQualifiedName);

                    if (typeSymbol == null)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(_missingTypeSymbol, null, typeFullyQualifiedName));
                        return;
                    }

                    string methodFullyQualifiedName = crefValue.Substring(indexOfLastSeparator + 1);
                    seeSymbol = typeSymbol.GetMembers().FirstOrDefault(memberSymbol => methodFullyQualifiedName.StartsWith(memberSymbol.Name)); // We can't know which overload, so just take first

                    if (seeSymbol == null)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(_missingMemberSymbol, null, methodFullyQualifiedName));
                        return;
                    }

                    displayFormat = DisplayFormats.MethodInlineDisplayFormat;
                }
                else
                {
                    context.ReportDiagnostic(Diagnostic.Create(_crefValueWithUnexpectedPrefix, null, crefValue));
                    return;
                }

                stringBuilder.
                    Append('`').
                    Append(seeSymbol.ToDisplayString(displayFormat)).
                    Append('`');

                return;
            }
            else if (elementName == "paramref")
            {
                string? name = xElement.Attribute("name")?.Value;

                if (name == null)
                {
                    return;
                }

                stringBuilder.
                    Append('`').
                    Append(name).
                    Append('`');

                return;
            }

            // Text in code blocks are displayed as is, so we have to pre-decode. Otherwise say angle brackets used for generics are rendered
            // as &gt; and &lt;. Note that this means we can't include HTML entities in code blocks cause they'll get decoded.
            // TODO consider adding attribute flags to work around the issue (decodeHTML flag)
            bool decodeHTML = false;
            if (elementName == "c")
            {
                stringBuilder.Append('`');
                decodeHTML = true;
            }
            else if (elementName == "a")
            {
                stringBuilder.Append('[');
            }
            else if (elementName == "code")
            {
                string? language = xElement.Attribute("language")?.Value;

                if (language != null)
                {
                    stringBuilder.
                        Append("```").
                        AppendLine(language);
                }
                else
                {
                    stringBuilder.AppendLine("```");
                }
                decodeHTML = true;
            }

            // Iterate over child nodes
            foreach (XNode descendantXNode in xElement.Nodes())
            {
                stringBuilder.AppendXmlNodeContents(descendantXNode, compilation, decodeHTML, ref context);
            }

            if (elementName == "c")
            {
                stringBuilder.Append('`');
            }
            else if (elementName == "a")
            {
                stringBuilder.
                    Append("](").
                    Append(xElement.Attribute("href")?.Value ?? string.Empty).
                    Append(')');
            }
            else if (elementName == "para")
            {
                stringBuilder.Append("  \n\n");
            }
            else if (elementName == "code")
            {
                stringBuilder.
                    AppendLine("\n```");
            }
        }

        // https://stackoverflow.com/questions/24769701/trim-whitespace-from-the-end-of-a-stringbuilder-without-calling-tostring-trim
        public static StringBuilder TrimEnd(this StringBuilder stringBuilder)
        {
            if (stringBuilder.Length == 0) return stringBuilder;

            int i = stringBuilder.Length - 1;

            for (; i >= 0; i--)
                if (!char.IsWhiteSpace(stringBuilder[i]))
                    break;

            if (i < stringBuilder.Length - 1)
                stringBuilder.Length = i + 1;

            return stringBuilder;
        }


        private static XElement? TryGetXmlDocumentationRootElement(ISymbol symbol)
        {
            string? xmlComment = symbol.GetDocumentationCommentXml(); // Note: this method indents our XML, can mess up text indentation

            if (string.IsNullOrWhiteSpace(xmlComment))
            {
                return null;
            }

            xmlComment = Regex.Replace(xmlComment, "^    ", "", RegexOptions.Multiline); // Get rid of indents

            XElement rootElement;
            try
            {
                rootElement = XDocument.Parse(xmlComment).Root;
            }
            catch
            {
                // Do nothing if xml is malformed
                return null;
            }

            XElement inheritDocElement = rootElement.Element("inheritdoc");

            if (inheritDocElement == null)
            {
                return rootElement;
            }

            ImmutableArray<INamedTypeSymbol> containingTypeInterfaceSymbols = symbol.ContainingType.AllInterfaces;
            foreach (INamedTypeSymbol containingTypeInterfaceSymbol in containingTypeInterfaceSymbols)
            {
                ImmutableArray<ISymbol> memberSymbols = containingTypeInterfaceSymbol.GetMembers();
                foreach (ISymbol memberSymbol in memberSymbols)
                {
                    // TODO
                    // - More stringent checks to determine whether symbol is the implementation of memberSymbol,
                    //   in particular, methods could be overloaded. Add checks when we have code to test it on.
                    if (symbol.Kind != memberSymbol.Kind ||
                        symbol.Name != memberSymbol.Name)
                    {
                        continue;
                    }

                    return TryGetXmlDocumentationRootElement(memberSymbol);
                }
            }

            return null;
        }

        private static string ToHtmlEncodedDisplayString(ISymbol symbol, SymbolDisplayFormat displayFormat)
        {
            return HttpUtility.HtmlEncode(symbol.ToDisplayString(displayFormat));
        }

        // We can't simply use the last . since fullyQualifiedMemberName could be something like M:Jering.Javascript.NodeJS.INodeJSService.TryInvokeFromCacheAsync``1(System.String,System.String,System.Object[],System.Threading.CancellationToken)
        private static int GetLastIndexOfFullyQualifiedTypeName(string fullyQualifiedMemberName)
        {
            int length = fullyQualifiedMemberName.Length;
            int lastNamespaceDotIndex = 0;
            for (int i = 0; i < length; i++)
            {
                char c = fullyQualifiedMemberName[i];
                if (c == '.')
                {
                    lastNamespaceDotIndex = i;
                }
                else if (c == '`' || c == '(')
                {
                    break;
                }
            }

            return lastNamespaceDotIndex - 1;
        }
    }

    public static class DisplayFormats
    {
        public static readonly SymbolDisplayFormat TypeTitleDisplayFormat = new(genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters);
        public static readonly SymbolDisplayFormat ConstructorTitleDisplayFormat = new(
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
            memberOptions: SymbolDisplayMemberOptions.IncludeParameters,
            parameterOptions: SymbolDisplayParameterOptions.IncludeParamsRefOut | SymbolDisplayParameterOptions.IncludeType,
            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes | SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers | SymbolDisplayMiscellaneousOptions.UseErrorTypeSymbolName);
        public static readonly SymbolDisplayFormat ordinaryMethodTitleDisplayFormat = new(genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
            memberOptions: SymbolDisplayMemberOptions.IncludeExplicitInterface | SymbolDisplayMemberOptions.IncludeParameters | SymbolDisplayMemberOptions.IncludeContainingType,
            parameterOptions: SymbolDisplayParameterOptions.IncludeParamsRefOut | SymbolDisplayParameterOptions.IncludeType,
            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes | SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers | SymbolDisplayMiscellaneousOptions.UseErrorTypeSymbolName);
        public static readonly SymbolDisplayFormat propertyTitleDisplayFormat = ordinaryMethodTitleDisplayFormat;
        public static readonly SymbolDisplayFormat SignatureDisplayFormat = new(genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
            memberOptions: SymbolDisplayMemberOptions.IncludeAccessibility | SymbolDisplayMemberOptions.IncludeModifiers | SymbolDisplayMemberOptions.IncludeParameters | SymbolDisplayMemberOptions.IncludeType,
            parameterOptions: SymbolDisplayParameterOptions.IncludeParamsRefOut | SymbolDisplayParameterOptions.IncludeType | SymbolDisplayParameterOptions.IncludeName | SymbolDisplayParameterOptions.IncludeDefaultValue | SymbolDisplayParameterOptions.IncludeExtensionThis | SymbolDisplayParameterOptions.IncludeOptionalBrackets,
            propertyStyle: SymbolDisplayPropertyStyle.ShowReadWriteDescriptor,
            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes | SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers | SymbolDisplayMiscellaneousOptions.UseErrorTypeSymbolName | SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);
        public static readonly SymbolDisplayFormat TypeInlineDisplayFormat = new(genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes | SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers | SymbolDisplayMiscellaneousOptions.UseErrorTypeSymbolName);
        public static readonly SymbolDisplayFormat MethodInlineDisplayFormat = new(genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
            memberOptions: SymbolDisplayMemberOptions.IncludeContainingType,
            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes | SymbolDisplayMiscellaneousOptions.EscapeKeywordIdentifiers | SymbolDisplayMiscellaneousOptions.UseErrorTypeSymbolName);
    }

    public class ApiDocumentationGeneratorSyntaxReceiver : ISyntaxReceiver
    {
        public Dictionary<string, ClassDeclarationSyntax> PublicClassDeclarations = new();
        public Dictionary<string, InterfaceDeclarationSyntax> PublicInterfaceDeclarations = new();

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            if (syntaxNode is ClassDeclarationSyntax classDeclarationSyntax)
            {
                if (classDeclarationSyntax.Modifiers.Any(SyntaxKind.PublicKeyword))
                {
                    PublicClassDeclarations.Add(classDeclarationSyntax.Identifier.ValueText, classDeclarationSyntax);
                }

                return;
            }

            if (syntaxNode is InterfaceDeclarationSyntax interfaceDeclarationSyntax &&
                interfaceDeclarationSyntax.Modifiers.Any(SyntaxKind.PublicKeyword))
            {
                PublicInterfaceDeclarations.Add(interfaceDeclarationSyntax.Identifier.ValueText, interfaceDeclarationSyntax);
            }
        }
    }
}
