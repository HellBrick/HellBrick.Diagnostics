using System;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HellBrick.Diagnostics.AccessModifiers
{
	internal static class DeclarationHandlers
	{
		public static ImmutableArray<SyntaxKind> SupportedSyntaxKinds { get; }
		public static ImmutableDictionary<SyntaxKind, IDeclarationHandler> HandlerLookup { get; }

		static DeclarationHandlers()
		{
			HandlerLookup = InitializeHandlerLookup();
			SupportedSyntaxKinds = HandlerLookup.Keys.ToImmutableArray();
		}

		private static ImmutableDictionary<SyntaxKind, IDeclarationHandler> InitializeHandlerLookup()
		{
			System.Reflection.TypeInfo handlerTypeInfo = typeof( IDeclarationHandler ).GetTypeInfo();
			System.Reflection.TypeInfo containerTypeInfo = typeof( DeclarationHandlers ).GetTypeInfo();

			System.Reflection.TypeInfo[] handlerTypes = containerTypeInfo.DeclaredNestedTypes
				.Where( t => handlerTypeInfo.IsAssignableFrom( t ) )
				.ToArray();

			return handlerTypes
				.Select( t => Activator.CreateInstance( t.AsType() ) as IDeclarationHandler )
				.ToImmutableDictionary( handler => handler.Kind );
		}

		private class ClassDeclarationHandler : DeclarationHandler<ClassDeclarationSyntax>
		{
			public override SyntaxKind Kind => SyntaxKind.ClassDeclaration;
			protected override SyntaxTokenList GetModifiers( ClassDeclarationSyntax node ) => node.Modifiers;
			protected override SyntaxNode WithModifiers( ClassDeclarationSyntax node, SyntaxTokenList newModifiers ) => node.WithModifiers( newModifiers );
		}

		private class EnumDeclarationHandler : DeclarationHandler<EnumDeclarationSyntax>
		{
			public override SyntaxKind Kind => SyntaxKind.EnumDeclaration;
			protected override SyntaxTokenList GetModifiers( EnumDeclarationSyntax node ) => node.Modifiers;
			protected override SyntaxNode WithModifiers( EnumDeclarationSyntax node, SyntaxTokenList newModifiers ) => node.WithModifiers( newModifiers );
		}

		private class InterfaceDeclarationHandler : DeclarationHandler<InterfaceDeclarationSyntax>
		{
			public override SyntaxKind Kind => SyntaxKind.InterfaceDeclaration;
			protected override SyntaxTokenList GetModifiers( InterfaceDeclarationSyntax node ) => node.Modifiers;
			protected override SyntaxNode WithModifiers( InterfaceDeclarationSyntax node, SyntaxTokenList newModifiers ) => node.WithModifiers( newModifiers );
		}

		private class StructDeclarationHandler : DeclarationHandler<StructDeclarationSyntax>
		{
			public override SyntaxKind Kind => SyntaxKind.StructDeclaration;
			protected override SyntaxTokenList GetModifiers( StructDeclarationSyntax node ) => node.Modifiers;
			protected override SyntaxNode WithModifiers( StructDeclarationSyntax node, SyntaxTokenList newModifiers ) => node.WithModifiers( newModifiers );
		}

		private class ConstructorDeclarationHandler : DeclarationHandler<ConstructorDeclarationSyntax>
		{
			public override SyntaxKind Kind => SyntaxKind.ConstructorDeclaration;
			protected override SyntaxTokenList GetModifiers( ConstructorDeclarationSyntax node ) => node.Modifiers;
			protected override SyntaxNode WithModifiers( ConstructorDeclarationSyntax node, SyntaxTokenList newModifiers ) => node.WithModifiers( newModifiers );
		}

		private class ConversionOperatorDeclarationHandler : DeclarationHandler<ConversionOperatorDeclarationSyntax>
		{
			public override SyntaxKind Kind => SyntaxKind.ConversionOperatorDeclaration;
			protected override SyntaxTokenList GetModifiers( ConversionOperatorDeclarationSyntax node ) => node.Modifiers;
			protected override SyntaxNode WithModifiers( ConversionOperatorDeclarationSyntax node, SyntaxTokenList newModifiers ) => node.WithModifiers( newModifiers );
		}

		private class EventDeclarationHandler : DeclarationHandler<EventDeclarationSyntax>
		{
			public override SyntaxKind Kind => SyntaxKind.EventDeclaration;
			protected override SyntaxTokenList GetModifiers( EventDeclarationSyntax node ) => node.Modifiers;
			protected override SyntaxNode WithModifiers( EventDeclarationSyntax node, SyntaxTokenList newModifiers ) => node.WithModifiers( newModifiers );
		}

		private class EventFieldDeclarationHandler : DeclarationHandler<EventFieldDeclarationSyntax>
		{
			public override SyntaxKind Kind => SyntaxKind.EventFieldDeclaration;
			protected override SyntaxTokenList GetModifiers( EventFieldDeclarationSyntax node ) => node.Modifiers;
			protected override SyntaxNode WithModifiers( EventFieldDeclarationSyntax node, SyntaxTokenList newModifiers ) => node.WithModifiers( newModifiers );
		}

		private class FieldDeclarationHandler : DeclarationHandler<FieldDeclarationSyntax>
		{
			public override SyntaxKind Kind => SyntaxKind.FieldDeclaration;
			protected override SyntaxTokenList GetModifiers( FieldDeclarationSyntax node ) => node.Modifiers;
			protected override SyntaxNode WithModifiers( FieldDeclarationSyntax node, SyntaxTokenList newModifiers ) => node.WithModifiers( newModifiers );
		}

		private class IndexerDeclarationHandler : DeclarationHandler<IndexerDeclarationSyntax>
		{
			public override SyntaxKind Kind => SyntaxKind.IndexerDeclaration;
			protected override SyntaxTokenList GetModifiers( IndexerDeclarationSyntax node ) => node.Modifiers;
			protected override SyntaxNode WithModifiers( IndexerDeclarationSyntax node, SyntaxTokenList newModifiers ) => node.WithModifiers( newModifiers );
		}

		private class MethodDeclarationHandler : DeclarationHandler<MethodDeclarationSyntax>
		{
			public override SyntaxKind Kind => SyntaxKind.MethodDeclaration;
			protected override SyntaxTokenList GetModifiers( MethodDeclarationSyntax node ) => node.Modifiers;
			protected override SyntaxNode WithModifiers( MethodDeclarationSyntax node, SyntaxTokenList newModifiers ) => node.WithModifiers( newModifiers );
		}

		private class OperatorDeclarationHandler : DeclarationHandler<OperatorDeclarationSyntax>
		{
			public override SyntaxKind Kind => SyntaxKind.OperatorDeclaration;
			protected override SyntaxTokenList GetModifiers( OperatorDeclarationSyntax node ) => node.Modifiers;
			protected override SyntaxNode WithModifiers( OperatorDeclarationSyntax node, SyntaxTokenList newModifiers ) => node.WithModifiers( newModifiers );
		}

		private class PropertyDeclarationHandler : DeclarationHandler<PropertyDeclarationSyntax>
		{
			public override SyntaxKind Kind => SyntaxKind.PropertyDeclaration;
			protected override SyntaxTokenList GetModifiers( PropertyDeclarationSyntax node ) => node.Modifiers;
			protected override SyntaxNode WithModifiers( PropertyDeclarationSyntax node, SyntaxTokenList newModifiers ) => node.WithModifiers( newModifiers );
		}
	}
}
