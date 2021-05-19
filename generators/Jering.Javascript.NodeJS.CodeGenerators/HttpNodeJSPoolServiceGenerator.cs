using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Text;
using System.Threading;

#nullable enable

namespace Jering.Javascript.NodeJS.Generators
{
    /// <summary>
    /// <para>Generates HttpNodeJSPoolService methods.</para>
    /// <para>HttpNodeJSPoolService wraps several INodeJSService instances, exposing their members.
    /// This generator creates the methods from INodeJSService metadata, avoiding the need for manual creation.</para>
    /// </summary>
    [Generator]
    public class HttpNodeJSPoolServiceGenerator : SourceGenerator<HttpNodeJSPoolServiceReceiver>
    {
        protected static readonly DiagnosticDescriptor _missingInterfaceDeclaration = new("G0010",
            "Missing interface declaration",
            "Missing INodeJSService interface declaration. Unable to generate HttpNodeJSPoolService.",
            "Code generation",
            DiagnosticSeverity.Error,
            true);

        protected static readonly DiagnosticDescriptor _missingInterfaceSymbol = new("G0011",
            "Missing interface symbol",
            "Missing INodeJSService interface symbol. Unable to generate HttpNodeJSPoolService.",
            "Code generation",
            DiagnosticSeverity.Error,
            true);

        protected static readonly DiagnosticDescriptor _missingMemberDeclaration = new("G0012",
            "Missing member declaration",
            "Missing member declaration: {0}",
            "Code generation",
            DiagnosticSeverity.Error,
            true);

        private readonly SymbolDisplayFormat _declarationSymbolDisplayFormat = new(genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
            memberOptions: SymbolDisplayMemberOptions.IncludeParameters | SymbolDisplayMemberOptions.IncludeType | SymbolDisplayMemberOptions.IncludeModifiers,
            parameterOptions: SymbolDisplayParameterOptions.IncludeType | SymbolDisplayParameterOptions.IncludeName | SymbolDisplayParameterOptions.IncludeDefaultValue,
            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes | SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

        private static readonly SymbolDisplayFormat _invocationSymbolDisplayFormat = new(genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
            memberOptions: SymbolDisplayMemberOptions.IncludeParameters,
            parameterOptions: SymbolDisplayParameterOptions.IncludeName);

        // Files
        private const string CS_FILE_NAME = "HttpNodeJSPoolService.Generated.cs";

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
            if (context.SyntaxReceiver is not HttpNodeJSPoolServiceReceiver HttpNodeJSPoolServiceReceiver)
            {
                return;
            }

            // Generate
            StringBuilder classBuilder = new();

            // Generate - class start
            classBuilder.Append(@"using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

#nullable enable

namespace Jering.Javascript.NodeJS
{
    public partial class HttpNodeJSPoolService
    {");

            // Semantic model
            InterfaceDeclarationSyntax? iNodeJSServiceInterfaceDeclarationSyntax = HttpNodeJSPoolServiceReceiver.INodeJSServiceInterfaceDeclarationSyntax;
            if (iNodeJSServiceInterfaceDeclarationSyntax == null)
            {
                context.ReportDiagnostic(Diagnostic.Create(_missingInterfaceDeclaration, null));
                return;
            }
            SemanticModel semanticModel = context.Compilation.GetSemanticModel(iNodeJSServiceInterfaceDeclarationSyntax.SyntaxTree);
            INamedTypeSymbol? interfaceSymbol = semanticModel.GetDeclaredSymbol(iNodeJSServiceInterfaceDeclarationSyntax);
            if (interfaceSymbol == null)
            {
                context.ReportDiagnostic(Diagnostic.Create(_missingInterfaceSymbol, null));
                return;
            }

            // Generate - members
            ImmutableArray<ISymbol> memberSymbols = interfaceSymbol.GetMembers();
            foreach (ISymbol memberSymbol in memberSymbols)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                // Create method
                classBuilder.
                    AppendLine().
                    AppendLine("        /// <inheritdoc />").
                    Append("        public ").
                    AppendLine(memberSymbol.ToDisplayString(_declarationSymbolDisplayFormat)).
                    Append(@"        {
            return GetHttpNodeJSService().").
                    Append(memberSymbol.ToDisplayString(_invocationSymbolDisplayFormat)).
                    AppendLine(@";
        }");
            }

            // Generate - class end
            classBuilder.Append(@"    }
}");

            // Output
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            context.AddSource(CS_FILE_NAME, classBuilder.ToString());
        }
    }

    public class HttpNodeJSPoolServiceReceiver : ISyntaxReceiver
    {
        public InterfaceDeclarationSyntax? INodeJSServiceInterfaceDeclarationSyntax;

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            if (INodeJSServiceInterfaceDeclarationSyntax != null ||
                syntaxNode is not InterfaceDeclarationSyntax interfaceDeclarationSyntax ||
                interfaceDeclarationSyntax.Identifier.ValueText != "INodeJSService")
            {
                return;
            }

            INodeJSServiceInterfaceDeclarationSyntax = interfaceDeclarationSyntax;
        }
    }
}
