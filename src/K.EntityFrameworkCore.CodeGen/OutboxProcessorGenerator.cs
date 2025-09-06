using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;

#nullable disable
namespace K.EntityFrameworkCore.CodeGen
{
    /// <summary>
    /// Source generator that creates middleware specifier implementations for DbContext types.
    /// Automatically generates code to handle type-specific outbox message processing.
    /// </summary>
    [Generator, ExcludeFromCodeCoverage]
    public class OutboxProcessorGenerator : IIncrementalGenerator
    {
        /// <summary>
        /// Initializes the incremental generator by setting up syntax providers and registering source output.
        /// </summary>
        /// <param name="context">The generator initialization context.</param>
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            IncrementalValuesProvider<ITypeSymbol> dbContexts = context.SyntaxProvider.CreateSyntaxProvider(
                predicate: static (node, _) => node is ClassDeclarationSyntax,
                transform: static (ctx, ct) => GetDbContextSymbol(ctx, ct))
                .Where(static m => m is not null);

            IncrementalValueProvider<(Compilation, ImmutableArray<ITypeSymbol>)> compilationAndDbContexts =
                context.CompilationProvider.Combine(dbContexts.Collect());

            context.RegisterSourceOutput(compilationAndDbContexts,
                static (spc, source) => Execute(source.Item1, source.Item2, spc));
        }

        private static ITypeSymbol GetDbContextSymbol(GeneratorSyntaxContext context, CancellationToken cancellationToken)
        {
            var classDeclaration = (ClassDeclarationSyntax)context.Node;
            if (context.SemanticModel.GetDeclaredSymbol(classDeclaration, cancellationToken) is ITypeSymbol typeSymbol && IsDbContext(typeSymbol))
            {
                return typeSymbol;
            }

            return null;
        }

        private static void Execute(Compilation compilation, ImmutableArray<ITypeSymbol> dbContexts, SourceProductionContext context)
        {
            if (dbContexts.IsDefaultOrEmpty)
            {
                return;
            }

            foreach (var item in dbContexts.Distinct(SymbolEqualityComparer.Default))
            {
                if (item is not ITypeSymbol dbContext) continue;

                var messageTypes = GetMessageTypes(dbContext, compilation);
                if (messageTypes.Any())
                {
                    var source = GenerateMiddlewareSpecifier(dbContext, messageTypes);
                    context.AddSource($"{dbContext.Name}MiddlewareSpecifier.g.cs", SourceText.From(source, Encoding.UTF8));
                }
            }
        }

        private static bool IsDbContext(ITypeSymbol typeSymbol)
        {
            //Debugger.Launch();

            var current = typeSymbol.BaseType;
            while (current != null)
            {
                if (current.ToDisplayString() == "Microsoft.EntityFrameworkCore.DbContext")
                {
                    return true;
                }
                current = current.BaseType;
            }
            return false;
        }

        private static IEnumerable<ITypeSymbol> GetMessageTypes(ITypeSymbol dbContext, Compilation compilation)
        {
            var topicInterface = compilation.GetTypeByMetadataName("K.EntityFrameworkCore.Topic`1");
            var outboxMessageInterface = compilation.GetTypeByMetadataName("K.EntityFrameworkCore.IOutboxMessage");

            if (topicInterface != null)
            {
                foreach (var member in dbContext.GetMembers())
                {
                    if (member is IPropertySymbol { Type: INamedTypeSymbol propertyType } &&
                        propertyType.IsGenericType &&
                        SymbolEqualityComparer.Default.Equals(propertyType.ConstructedFrom, topicInterface))
                    {
                        var messageType = propertyType.TypeArguments.FirstOrDefault();
                        if (messageType != null)
                        {
                            yield return messageType;
                        }
                    }
                }
            }

            if (outboxMessageInterface != null)
            {
                var onModelCreating = dbContext.GetMembers("OnModelCreating")
                                               .OfType<IMethodSymbol>()
                                               .FirstOrDefault(m => m.Parameters.Length == 1 && m.Parameters[0].Type.Name == "ModelBuilder");

                if (onModelCreating?.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax() is MethodDeclarationSyntax onModelCreatingSyntax)
                {
                    var semanticModel = compilation.GetSemanticModel(onModelCreatingSyntax.SyntaxTree);
                    foreach (var invocation in onModelCreatingSyntax.DescendantNodes().OfType<InvocationExpressionSyntax>())
                    {
                        if (invocation.Expression is MemberAccessExpressionSyntax { Name: GenericNameSyntax { Identifier.ValueText: "Entity" } genericName } &&
                            genericName.TypeArgumentList.Arguments.Count == 1)
                        {
                            var typeArgumentSyntax = genericName.TypeArgumentList.Arguments[0];
                            if (semanticModel.GetTypeInfo(typeArgumentSyntax).Type is ITypeSymbol entityType &&
                                entityType.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, outboxMessageInterface)))
                            {
                                yield return entityType;
                            }
                        }
                    }
                }
            }
        }

        private static string GenerateMiddlewareSpecifier(ITypeSymbol dbContext, IEnumerable<ITypeSymbol> messageTypes)
        {
            var sb = new StringBuilder();
            var namespaceName = dbContext.ContainingNamespace.ToDisplayString();
            var distinctMessageTypes = messageTypes.Distinct(SymbolEqualityComparer.Default);

            sb.AppendLine($"// <auto-generated/>");
            sb.AppendLine($"#nullable disable");
            sb.AppendLine($"using K.EntityFrameworkCore;");
            sb.AppendLine($"using K.EntityFrameworkCore.Extensions;");
            sb.AppendLine($"using K.EntityFrameworkCore.Middlewares.Outbox;");
            sb.AppendLine();
            sb.AppendLine($"namespace {namespaceName}");
            sb.AppendLine($"{{");
            sb.AppendLine($"    public class {dbContext.Name}MiddlewareSpecifier : IMiddlewareSpecifier<{dbContext.Name}>");
            sb.AppendLine($"    {{");
            sb.AppendLine($"        public ScopedCommand DeferedExecution(OutboxMessage outboxMessage) => outboxMessage.Type switch");
            sb.AppendLine($"        {{");

            foreach (var type in distinctMessageTypes)
            {
                var fullTypeName = type.GetAssemblyQualifiedName();
                var simpleTypeName = type.Name;
                sb.AppendLine($"            \"{fullTypeName}\" => OutboxPollingWorker<{dbContext.Name}>.DeferedExecution<{simpleTypeName}>(outboxMessage),");
            }

            sb.AppendLine($"            _ => null");
            sb.AppendLine($"        }};");
            sb.AppendLine($"    }}");
            sb.AppendLine($"}}");

            return sb.ToString();
        }
    }
}

[ExcludeFromCodeCoverage]
internal static class SymbolExtensions
{
    /// <summary>
    /// Gets the assembly-qualified name for a symbol.
    /// </summary>
    /// <param name="symbol">The symbol to get the qualified name for.</param>
    /// <returns>The assembly-qualified name of the symbol.</returns>
    /// <exception cref="System.NotImplementedException">Thrown when the symbol type is not supported.</exception>
    public static string GetAssemblyQualifiedName(this ISymbol symbol)
    {
        var typeSymbol = symbol switch
        {
            INamedTypeSymbol named => named,
            _ => throw new System.NotImplementedException(),
        };

        return GetAssemblyQualifiedNameInternal(typeSymbol);
    }

    private static string GetAssemblyQualifiedNameInternal(INamedTypeSymbol symbol)
    {
        return $"{symbol}, {symbol.ContainingAssembly.Identity}";
    }
}
