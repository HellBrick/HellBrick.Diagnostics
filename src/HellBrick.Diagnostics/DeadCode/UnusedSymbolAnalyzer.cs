using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace HellBrick.Diagnostics.DeadCode
{
	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	public class UnusedSymbolAnalyzer : DiagnosticAnalyzer
	{
		public const string DiagnosticID = IDPrefix.Value + "UnusedSymbol";
		private static ConcurrentDictionary<string, HashSet<ISymbol>> _compilationSymbols = new ConcurrentDictionary<string, HashSet<ISymbol>>();

		private static readonly DiagnosticDescriptor _rule = new DiagnosticDescriptor
		(
			DiagnosticID,
			"Unused member",
			"'{0}' can be removed",
			DiagnosticCategory.Design,
			DiagnosticSeverity.Hidden,
			true,
			customTags: WellKnownDiagnosticTags.Unnecessary
		);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create( _rule );
		public override void Initialize( AnalysisContext context ) => context.RegisterCompilationStartAction( StartAnalysis );

		private void StartAnalysis( CompilationStartAnalysisContext context )
		{
			if ( context.Compilation.AssemblyName == null )
				return;

			_compilationSymbols.AddOrUpdate( context.Compilation.AssemblyName, new HashSet<ISymbol>(), ( key, old ) => new HashSet<ISymbol>() );
			context.RegisterSymbolAction( MarkDeclaredSymbols, ImmutableArray.Create( SymbolKind.Event, SymbolKind.Field, SymbolKind.Method, SymbolKind.Property ) );
			context.RegisterCompilationEndAction( AnalyzeUnusedSymbols );
		}

		private void MarkDeclaredSymbols( SymbolAnalysisContext context )
		{
			if ( context.Compilation.AssemblyName == null )
				return;

			HashSet<ISymbol> symbols;
			if ( !_compilationSymbols.TryGetValue( context.Compilation.AssemblyName, out symbols ) )
				throw new InvalidOperationException( $"There's no symbol storage for {context.Compilation.AssemblyName}" );

			if ( IsCandidate( context.Symbol ) )
				symbols.Add( context.Symbol );
		}

		private static bool IsCandidate( ISymbol symbol ) =>
			( symbol.DeclaredAccessibility == Accessibility.Private || symbol.DeclaredAccessibility == Accessibility.Internal ) &&
			!symbol.IsOverride &&
			!IsIgnoredMethod( symbol ) &&
			!ImplementsInterface( symbol );

		private static bool ImplementsInterface( ISymbol symbol ) =>
			(
				from @interface in symbol.ContainingType.AllInterfaces
				from interfaceMember in @interface.GetMembers()
				let implementation = symbol.ContainingType.FindImplementationForInterfaceMember( interfaceMember )
				where symbol == implementation
				select 0
			)
			.Any();

		private static bool IsIgnoredMethod( ISymbol symbol )
		{
			IMethodSymbol methodSymbol = symbol as IMethodSymbol;
			return
				methodSymbol != null &&
				(
					methodSymbol.MethodKind == MethodKind.PropertyGet ||
					methodSymbol.MethodKind == MethodKind.PropertySet ||
					methodSymbol.IsOverride ||
					methodSymbol.MetadataName == ".cctor"
				);
		}

		private void AnalyzeUnusedSymbols( CompilationAnalysisContext context )
		{
			if ( context.Compilation.AssemblyName == null )
				return;

			HashSet<ISymbol> candidates;
			if ( !_compilationSymbols.TryRemove( context.Compilation.AssemblyName, out candidates ) )
				return;

			foreach ( SyntaxTree syntaxTree in context.Compilation.SyntaxTrees )
			{
				ReferencedSymbolDiscarder discarder = new ReferencedSymbolDiscarder( context.Compilation.GetSemanticModel( syntaxTree ), candidates );
				discarder.Visit( syntaxTree.GetRoot() );
			}

			foreach ( ISymbol unusedSymbol in candidates )
			{
				ISymbol definition = unusedSymbol.OriginalDefinition;
				foreach ( Location symbolLocation in definition.Locations )
				{
					Location diagnosticLocation = GetDiagnosticLocation( symbolLocation );
					Diagnostic diagnostic = Diagnostic.Create( _rule, diagnosticLocation, unusedSymbol.ToString() );
					context.ReportDiagnostic( diagnostic );
				}
			}
		}

		private static Location GetDiagnosticLocation( Location symbolLocation )
		{
			SyntaxNode root = symbolLocation.SourceTree.GetRoot();
			SyntaxNode definitionNode = root.FindNode( symbolLocation.SourceSpan );
			Location diagnosticLocation = definitionNode.GetLocation();

			if ( definitionNode.HasStructuredTrivia && definitionNode.HasLeadingTrivia )
			{
				SyntaxTrivia leadingTrivia = definitionNode.GetLeadingTrivia().FirstOrDefault( t => t.IsKind( SyntaxKind.SingleLineDocumentationCommentTrivia ) );
				if ( leadingTrivia != default( SyntaxTrivia ) )
					diagnosticLocation = Location.Create( symbolLocation.SourceTree, TextSpan.FromBounds( leadingTrivia.FullSpan.Start, diagnosticLocation.SourceSpan.End ) );
			}

			return diagnosticLocation;
		}
	}
}