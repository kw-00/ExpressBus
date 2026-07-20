using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ExpressBus.Protocol.Sourcegen;
using Xunit;

namespace Test.ExpressBus.Protocol.Sourcegen;

public class DeclarationGenerationTests
{
    [Fact]
    public void GeneratePartial_SimpleClass_ReturnsNormalizedDeclaration()
    {
        // Arrange
        var code = "partial class MyClass { }";
        var tree = CSharpSyntaxTree.ParseText(code);
        var node = tree.GetCompilationUnitRoot().DescendantNodes().OfType<TypeDeclarationSyntax>().First();

        // Act
        var result = DeclarationGeneration.GeneratePartial(node);

        // Assert
        Assert.Equal("partial class MyClass", result);
    }

    [Fact]
    public void GeneratePartial_StructWithGenericsAndConstraints_ReturnsNormalizedDeclaration()
    {
        // Arrange
        var code = "partial struct MyStruct<T> where T : class { public int X; }";
        var tree = CSharpSyntaxTree.ParseText(code);
        var node = tree.GetCompilationUnitRoot().DescendantNodes().OfType<TypeDeclarationSyntax>().First();

        // Act
        var result = DeclarationGeneration.GeneratePartial(node);

        // Assert
        Assert.Equal("partial struct MyStruct<T> where T : class", result);
    }

    [Fact]
    public void GeneratePartial_RecordWithPrimaryConstructor_ReturnsNormalizedDeclaration()
    {
        // Arrange
        var code = "partial record MyRecord(int X);";
        var tree = CSharpSyntaxTree.ParseText(code);
        var node = tree.GetCompilationUnitRoot().DescendantNodes().OfType<TypeDeclarationSyntax>().First();

        // Act
        var result = DeclarationGeneration.GeneratePartial(node);

        // Assert
        Assert.Equal("partial record MyRecord", result);
    }
}
