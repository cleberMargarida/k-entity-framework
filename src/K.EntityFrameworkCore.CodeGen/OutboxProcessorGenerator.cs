using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

namespace K.EntityFrameworkCore.CodeGen
{
    [Generator]
    public class OutboxProcessorGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            IncrementalValuesProvider<ITypeSymbol?> dbContexts = context.SyntaxProvider.CreateSyntaxProvider(
                predicate: static (node, _) => node is ClassDeclarationSyntax,
                transform: static (ctx, ct) => GetDbContextSymbol(ctx, ct))
                .Where(static m => m is not null);

            IncrementalValueProvider<(Compilation, ImmutableArray<ITypeSymbol?>)> compilationAndDbContexts =
                context.CompilationProvider.Combine(dbContexts.Collect());

            context.RegisterSourceOutput(compilationAndDbContexts,
                static (spc, source) => Execute(source.Item1, source.Item2, spc));
        }

        private static ITypeSymbol? GetDbContextSymbol(GeneratorSyntaxContext context, CancellationToken cancellationToken)
        {
            var classDeclaration = (ClassDeclarationSyntax)context.Node;
            if (context.SemanticModel.GetDeclaredSymbol(classDeclaration, cancellationToken) is ITypeSymbol typeSymbol && IsDbContext(typeSymbol))
            {
                return typeSymbol;
            }

            return null;
        }

        private static void Execute(Compilation compilation, ImmutableArray<ITypeSymbol?> dbContexts, SourceProductionContext context)
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
                    var source = GenerateOutboxProcessor(dbContext, messageTypes);
                    context.AddSource($"{dbContext.Name}.OutboxProcessor.g.cs", SourceText.From(source, Encoding.UTF8));
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

        private static string GenerateOutboxProcessor(ITypeSymbol dbContext, IEnumerable<ITypeSymbol> messageTypes)
        {
            var sb = new StringBuilder();
            var namespaceName = dbContext.ContainingNamespace.ToDisplayString();
            var distinctMessageTypes = messageTypes.Distinct(SymbolEqualityComparer.Default);

            sb.AppendLine($"// <auto-generated/>");
            sb.AppendLine($"#nullable enable");
            sb.AppendLine($"using System;");
            sb.AppendLine($"using System.Threading.Tasks;");
            sb.AppendLine($"using K.EntityFrameworkCore;");
            sb.AppendLine($"using K.EntityFrameworkCore.Middlewares.Producer;");
            sb.AppendLine();
            sb.AppendLine($"namespace {namespaceName}");
            sb.AppendLine($"{{");
            sb.AppendLine($"    internal static class {dbContext.Name}OutboxProcessor");
            sb.AppendLine($"    {{");
            sb.AppendLine($"        internal static Task ProcessOutboxMessageAsync(this OutboxPollingWorker<{dbContext.Name}> worker, OutboxMessage message)");
            sb.AppendLine($"        {{");
            sb.AppendLine($"            return message.EventType switch");
            sb.AppendLine($"            {{");

            foreach (var type in distinctMessageTypes)
            {
                var fullTypeName = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                sb.AppendLine($"                \"{fullTypeName}\" => worker.DeferedExecution<{fullTypeName}>(message),");
            }

            sb.AppendLine($"                _ => throw new NotSupportedException($\"Message type '{{message.EventType}}' not supported for processing.\")");
            sb.AppendLine($"            }};");
            sb.AppendLine($"        }}");
            sb.AppendLine($"    }}");
            sb.AppendLine($"}}");

            return sb.ToString();
        }
    }
}
