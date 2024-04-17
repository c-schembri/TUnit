﻿using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using TUnit.Engine.SourceGenerator.CodeGenerators.Helpers;
using TUnit.Engine.SourceGenerator.CodeGenerators.Writers;
using TUnit.Engine.SourceGenerator.Enums;
using TUnit.Engine.SourceGenerator.Models;

namespace TUnit.Engine.SourceGenerator.CodeGenerators;

[Generator]
internal class TestsGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var basicTests = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                WellKnownFullyQualifiedClassNames.TestAttribute.WithoutGlobalPrefix,
                predicate: static (s, _) => IsSyntaxTargetForGeneration(s),
                transform: static (ctx, _) => GetSemanticTargetForGeneration(ctx, TestType.Basic))
            .Where(static m => m is not null);
        
        var dataDrivenTests = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                WellKnownFullyQualifiedClassNames.DataDrivenTestAttribute.WithoutGlobalPrefix,
                predicate: static (s, _) => IsSyntaxTargetForGeneration(s),
                transform: static (ctx, _) => GetSemanticTargetForGeneration(ctx, TestType.DataDriven))
            .Where(static m => m is not null);
        
        var dataSourceDrivenTests = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                WellKnownFullyQualifiedClassNames.DataSourceDrivenTestAttribute.WithoutGlobalPrefix,
                predicate: static (s, _) => IsSyntaxTargetForGeneration(s),
                transform: static (ctx, _) => GetSemanticTargetForGeneration(ctx, TestType.DataSourceDriven))
            .Where(static m => m is not null);
        
        var combinativeTests = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                WellKnownFullyQualifiedClassNames.CombinativeTestAttribute.WithoutGlobalPrefix,
                predicate: static (s, _) => IsSyntaxTargetForGeneration(s),
                transform: static (ctx, _) => GetSemanticTargetForGeneration(ctx, TestType.Combinative))
            .Where(static m => m is not null);
        
        context.RegisterSourceOutput(basicTests, Execute);
        context.RegisterSourceOutput(dataDrivenTests, Execute);
        context.RegisterSourceOutput(dataSourceDrivenTests, Execute);
        context.RegisterSourceOutput(combinativeTests, Execute);
    }

    static bool IsSyntaxTargetForGeneration(SyntaxNode node)
    {
        return node is MethodDeclarationSyntax;
    }

    static IEnumerable<TestSourceDataModel> GetSemanticTargetForGeneration(GeneratorAttributeSyntaxContext context, TestType testType)
    {
        if (context.TargetSymbol is not IMethodSymbol methodSymbol)
        {
            yield break;
        }

        if (methodSymbol.ContainingType.IsAbstract)
        {
            yield break;
        }

        if (methodSymbol.IsStatic)
        {
            yield break;
        }

        if (methodSymbol.DeclaredAccessibility != Accessibility.Public)
        {
            yield break;
        }

        foreach (var testSourceDataModel in methodSymbol.ParseTestDatas(methodSymbol.ContainingType, testType))
        {
            yield return testSourceDataModel;
        }
    }

    private void Execute(SourceProductionContext context, IEnumerable<TestSourceDataModel> models)
    {
        foreach (var model in models)
        {
            var className = $"{model.MethodName}_{model.MinimalTypeName}_{Guid.NewGuid():N}";

            using var sourceBuilder = new SourceCodeWriter();

            sourceBuilder.WriteLine("// <auto-generated/>");
            sourceBuilder.WriteLine("using System.Linq;");
            sourceBuilder.WriteLine("using System.Reflection;");
            sourceBuilder.WriteLine("using System.Runtime.CompilerServices;");
            sourceBuilder.WriteLine();
            sourceBuilder.WriteLine("namespace TUnit.Engine;");
            sourceBuilder.WriteLine();
            sourceBuilder.WriteLine($"file class {className}");
            sourceBuilder.WriteLine("{");
            sourceBuilder.WriteLine("[ModuleInitializer]");
            sourceBuilder.WriteLine("public static void Initialise()");
            sourceBuilder.WriteLine("{");

            GenericTestInvocationWriter.GenerateTestInvocationCode(sourceBuilder, model);

            sourceBuilder.WriteLine("}");
            sourceBuilder.WriteLine("}");

            context.AddSource($"{className}.g.cs", sourceBuilder.ToString());
        }
    }
}