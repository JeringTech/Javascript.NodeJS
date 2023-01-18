using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Text;
using System.Threading;

namespace Jering.Javascript.NodeJS.CodeGenerators
{
    /// <summary>
    /// <para>Generates StaticNodeJSService methods.</para>
    /// <para>StaticNodeJSService wraps an INodeJSService instance, exposing the instance's members statically.
    /// This generator creates the static methods from INodeJSService metadata, avoiding the need for manual creation.</para>
    /// </summary>
    [Generator]
    public class CodeIncrementalGenerator : IIncrementalGenerator
    {
        #region Diagnostics
        protected static readonly DiagnosticDescriptor _invalidInterfaceDeclarationOrSymbol = new("G0007",
            "Invalid interface declaration or symbol",
            "Invalid INodeJSService interface declaration or symbol. Unable to generate StaticNodeJSService.",
            "Code generation",
            DiagnosticSeverity.Error,
            true);

        protected static readonly DiagnosticDescriptor _invalidMemberDeclaration = new("G0009",
            "Invalid member declaration",
            "Invalid member declaration: {0}",
            "Code generation",
            DiagnosticSeverity.Error,
            true);
        #endregion

        #region Symbol Display Formats
        private static readonly SymbolDisplayFormat _declarationSymbolDisplayFormat = new(genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
            memberOptions: SymbolDisplayMemberOptions.IncludeParameters | SymbolDisplayMemberOptions.IncludeType | SymbolDisplayMemberOptions.IncludeModifiers,
            parameterOptions: SymbolDisplayParameterOptions.IncludeType | SymbolDisplayParameterOptions.IncludeName | SymbolDisplayParameterOptions.IncludeDefaultValue,
            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes | SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

        private static readonly SymbolDisplayFormat _invocationSymbolDisplayFormat = new(genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters,
            memberOptions: SymbolDisplayMemberOptions.IncludeParameters,
            parameterOptions: SymbolDisplayParameterOptions.IncludeName);
        #endregion

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // Generate log method names from syntax nodes
            IncrementalValueProvider<SortedList<MethodData>?> methodDatas = context.SyntaxProvider.
                CreateSyntaxProvider(SyntaxNodeSelector, MethodDataListGenerator).
                Single();

            // Generate StaticNodeJSService methods
            context.RegisterSourceOutput(methodDatas, OutputGenerator);
        }

        #region Output Generation - StaticNodeJSService
        private static void OutputGenerator(SourceProductionContext context, SortedList<MethodData>? methodDatas)
        {
            // Invalid
            if (methodDatas == null)
            {
                context.ReportDiagnostic(Diagnostic.Create(_invalidInterfaceDeclarationOrSymbol, null));
                return;
            }

            // Create string builder
            StringBuilder stringBuilder = new();

            // Generate StaticNodeJSService
            GenerateStaticNodeJSService(ref context, methodDatas, stringBuilder);

            // Generate HttpNodeJSPoolService
            GenerateHttpNodeJSPoolService(ref context, methodDatas, stringBuilder);
        }

        private static void GenerateStaticNodeJSService(ref SourceProductionContext context, SortedList<MethodData> MethodDatas, StringBuilder stringBuilder)
        {
            // Generate - class start
            stringBuilder.
                Clear().
                AppendLine(@"using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

#nullable enable

namespace Jering.Javascript.NodeJS
{
    public static partial class StaticNodeJSService
    {");

            // Generate - members
            foreach (MethodData methodData in MethodDatas)
            {
                if (!methodData.Initialized)
                {
                    context.ReportDiagnostic(Diagnostic.Create(_invalidMemberDeclaration, null, methodData.InterfaceMethodDeclaration));
                    continue;
                }

                // Create method
                stringBuilder.
                    Append(methodData.LeadingTrivia).
                    Append("public static ").
                    AppendLine(methodData.InterfaceMethodDeclaration).
                    Append(@"        {
            ");

                if (!methodData.ReturnsVoid)
                {
                    stringBuilder.Append("return ");
                }

                stringBuilder.
                    Append("GetOrCreateNodeJSService().").
                    Append(methodData.MethodInvocation).
                    AppendLine(@";
        }");
            }

            // Generate - class end
            stringBuilder.Append(@"    }
}");

            // Create file
            string content = stringBuilder.ToString();
            context.AddSource("StaticNodeJSService.Generated.cs", content);
        }

        private static void GenerateHttpNodeJSPoolService(ref SourceProductionContext context, SortedList<MethodData> MethodDatas, StringBuilder stringBuilder)
        {
            // Generate - class start
            stringBuilder.
                Clear().
                AppendLine(@"using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

#nullable enable

namespace Jering.Javascript.NodeJS
{
    public partial class HttpNodeJSPoolService
    {");

            // Generate - members
            foreach (MethodData methodData in MethodDatas)
            {
                if (!methodData.Initialized)
                {
                    context.ReportDiagnostic(Diagnostic.Create(_invalidMemberDeclaration, null, methodData.InterfaceMethodDeclaration));
                    continue;
                }

                // Create method
                stringBuilder.
                    AppendLine().
                    AppendLine("        /// <inheritdoc />").
                    Append("        public ").
                    AppendLine(methodData.InterfaceMethodDeclaration).
                    Append(@"        {
            ");

                if (!methodData.ReturnsVoid)
                {
                    stringBuilder.Append("return ");
                }

                stringBuilder.
                    Append("GetHttpNodeJSService().").
                    Append(methodData.MethodInvocation).
                    AppendLine(@";
        }");
            }

            // Generate - class end
            stringBuilder.Append(@"    }
}");

            // Create file
            string content = stringBuilder.ToString();
            context.AddSource("HttpNodeJSPoolService.Generated.cs", content);
        }
        #endregion

        #region Node Selection
        private static bool SyntaxNodeSelector(SyntaxNode syntaxNode, CancellationToken _)
        {
            return syntaxNode is InterfaceDeclarationSyntax interfaceDeclarationSyntax &&
                interfaceDeclarationSyntax.Identifier.ValueText == "INodeJSService";
        }
        #endregion

        #region Data Generation
        private static SortedList<MethodData>? MethodDataListGenerator(GeneratorSyntaxContext context, CancellationToken _)
        {
            // Interface semantic model
            InterfaceDeclarationSyntax? iNodeJSServiceInterfaceDeclarationSyntax = context.Node as InterfaceDeclarationSyntax;
            if (iNodeJSServiceInterfaceDeclarationSyntax == null)
            {
                return null;
            }
            INamedTypeSymbol? interfaceSymbol = context.SemanticModel.GetDeclaredSymbol(iNodeJSServiceInterfaceDeclarationSyntax) as INamedTypeSymbol;
            if (interfaceSymbol == null)
            {
                return null;
            }

            // Members
            var result = new SortedList<MethodData>();
            ImmutableArray<ISymbol> memberSymbols = interfaceSymbol.GetMembers();
            foreach (ISymbol memberSymbol in memberSymbols)
            {
                if (memberSymbol is not IMethodSymbol methodSymbol)
                {
                    continue;
                }

                // Get declaration
                string interfaceMethodDeclaration = methodSymbol.ToDisplayString(_declarationSymbolDisplayFormat);

                // Get leading trivia
                SyntaxReference? syntaxReference = methodSymbol.DeclaringSyntaxReferences.FirstOrDefault();
                if (syntaxReference == null)
                {
                    result.Add(new MethodData(interfaceMethodDeclaration));
                    continue;
                }
                string leadingTrivia = syntaxReference.GetSyntax().GetLeadingTrivia().ToFullString();

                // Get invocation
                string methodInvocation = methodSymbol.ToDisplayString(_invocationSymbolDisplayFormat);

                // Get returns boid
                bool returnsVoid = methodSymbol.ReturnType.SpecialType == SpecialType.System_Void;

                // Add
                result.Add(new MethodData(leadingTrivia, interfaceMethodDeclaration, methodInvocation, returnsVoid));
            }

            return result;
        }
        #endregion

        #region Data
        private struct MethodData : IEquatable<MethodData>, IComparable<MethodData>
        {
            public readonly string LeadingTrivia;
            public readonly string InterfaceMethodDeclaration;
            public readonly string MethodInvocation;
            public readonly bool ReturnsVoid;
            public readonly bool Initialized { get; } = false;

            public MethodData(string leadingTrivia, string interfaceMethodDeclaration, string methodInvocation, bool returnsVoid)
            {
                LeadingTrivia = leadingTrivia;
                InterfaceMethodDeclaration = interfaceMethodDeclaration;
                MethodInvocation = methodInvocation;
                ReturnsVoid = returnsVoid;
                Initialized = true;
            }

            public MethodData(string interfaceMethodDeclaration)
            {
                InterfaceMethodDeclaration = interfaceMethodDeclaration;
                LeadingTrivia = string.Empty;
                MethodInvocation = string.Empty;
                ReturnsVoid = false;
                Initialized = false;
            }

            public bool Equals(MethodData other)
            {
                return LeadingTrivia == other.LeadingTrivia &&
                       InterfaceMethodDeclaration == other.InterfaceMethodDeclaration &&
                       MethodInvocation == other.MethodInvocation &&
                       ReturnsVoid == other.ReturnsVoid &&
                        Initialized == other.Initialized;
            }

            public override int GetHashCode()
            {
                int hashCode = -2028132620;
                hashCode = hashCode * -1521134295 + EqualityComparer<string?>.Default.GetHashCode(LeadingTrivia);
                hashCode = hashCode * -1521134295 + EqualityComparer<string?>.Default.GetHashCode(InterfaceMethodDeclaration);
                hashCode = hashCode * -1521134295 + EqualityComparer<string?>.Default.GetHashCode(MethodInvocation);
                hashCode = hashCode * -1521134295 + ReturnsVoid.GetHashCode();
                hashCode = hashCode * -1521134295 + Initialized.GetHashCode();
                return hashCode;
            }

            public int CompareTo(MethodData other)
            {
                return InterfaceMethodDeclaration.CompareTo(other.InterfaceMethodDeclaration);
            }
        }
        #endregion
    }
}
