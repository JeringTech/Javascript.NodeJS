using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading;

#nullable enable

namespace Jering.Javascript.NodeJS.Generators
{
    /// <summary>
    /// <para>Generates StaticNodeJSService methods.</para>
    /// <para>StaticNodeJSService wraps an INodeJSService instance, exposing the instance's members statically.
    /// This generator creates the static methods from INodeJSService metadata, avoiding the need for manual creation.</para>
    /// </summary>
    [Generator]
    public class StaticNodeJSServiceGenerator : SourceGenerator<StaticNodeJSServiceReceiver>
    {
        protected static readonly DiagnosticDescriptor _missingInterfaceDeclaration = new("G0007",
            "Missing interface declaration",
            "Missing INodeJSService interface declaration. Unable to generate StaticNodeJSService.",
            "Code generation",
            DiagnosticSeverity.Error,
            true);

        protected static readonly DiagnosticDescriptor _missingInterfaceSymbol = new("G0008",
            "Missing interface symbol",
            "Missing INodeJSService interface symbol. Unable to generate StaticNodeJSService.",
            "Code generation",
            DiagnosticSeverity.Error,
            true);

        protected static readonly DiagnosticDescriptor _missingMemberDeclaration = new("G0009",
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
        private const string CS_FILE_NAME = "StaticNodeJSService.Generated.cs";

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
            if (context.SyntaxReceiver is not StaticNodeJSServiceReceiver staticNodeJSServiceReceiver)
            {
                return;
            }

            // Generate
            StringBuilder classBuilder = new();

            // Generate - class start
            classBuilder.AppendLine(@"using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

#nullable enable

namespace Jering.Javascript.NodeJS
{
    public static partial class StaticNodeJSService
    {");

            // Semantic model
            InterfaceDeclarationSyntax? iNodeJSServiceInterfaceDeclarationSyntax = staticNodeJSServiceReceiver.INodeJSServiceInterfaceDeclarationSyntax;
            if(iNodeJSServiceInterfaceDeclarationSyntax == null)
            {
                context.ReportDiagnostic(Diagnostic.Create(_missingInterfaceDeclaration, null));
                return;
            }
            SemanticModel semanticModel = context.Compilation.GetSemanticModel(iNodeJSServiceInterfaceDeclarationSyntax.SyntaxTree);
            INamedTypeSymbol? interfaceSymbol = semanticModel.GetDeclaredSymbol(iNodeJSServiceInterfaceDeclarationSyntax);
            if(interfaceSymbol == null)
            {
                context.ReportDiagnostic(Diagnostic.Create(_missingInterfaceSymbol, null));
                return;    
            }

            // Generate - members
            ImmutableArray<ISymbol> memberSymbols = interfaceSymbol.GetMembers();
            foreach(ISymbol memberSymbol in memberSymbols)
            {
                if (cancellationToken.IsCancellationRequested || memberSymbol is not IMethodSymbol methodSymbol)
                {
                    return;
                }

                // Get leading trivia
                SyntaxReference? syntaxReference = methodSymbol.DeclaringSyntaxReferences.FirstOrDefault();
                if(syntaxReference == null)
                {
                    context.ReportDiagnostic(Diagnostic.Create(_missingMemberDeclaration, null, methodSymbol.Name));
                    continue;
                }
                string leadingTrivia = syntaxReference.GetSyntax().GetLeadingTrivia().ToFullString();

                // Create method
                classBuilder.
                    Append(leadingTrivia).
                    Append("public static ").
                    AppendLine(methodSymbol.ToDisplayString(_declarationSymbolDisplayFormat)).
                    Append(@"        {
            ");

                if (methodSymbol.ReturnType.SpecialType != SpecialType.System_Void)
                {
                    classBuilder.Append("return ");
                }

                classBuilder.
                    Append("GetOrCreateNodeJSService().").
                    Append(methodSymbol.ToDisplayString(_invocationSymbolDisplayFormat)).
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

    public class StaticNodeJSServiceReceiver : ISyntaxReceiver
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
