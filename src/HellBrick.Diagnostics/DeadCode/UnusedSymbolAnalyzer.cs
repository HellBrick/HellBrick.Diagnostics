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
			UnusedSymbolAnalysisContext analysisContext = new UnusedSymbolAnalysisContext();
			context.RegisterSymbolAction( symbolContext => analysisContext.TrackSymbol( symbolContext.Symbol ), ImmutableArray.Create( SymbolKind.Event, SymbolKind.Field, SymbolKind.Method, SymbolKind.Property ) );
			context.RegisterCompilationEndAction( compilationContext => analysisContext.DiscardUsedSymbolsAndReportDiagnostics( compilationContext ) );
		}

		private class UnusedSymbolAnalysisContext
		{
			private HashSet<ISymbol> _symbols = new HashSet<ISymbol>();

			public void TrackSymbol( ISymbol symbol )
			{
				if ( IsCandidate( symbol ) )
					_symbols.Add( symbol );
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

			public void DiscardUsedSymbolsAndReportDiagnostics( CompilationAnalysisContext context )
			{
				foreach ( SyntaxTree syntaxTree in context.Compilation.SyntaxTrees )
				{
					ReferencedSymbolDiscarder discarder = new ReferencedSymbolDiscarder( context.Compilation.GetSemanticModel( syntaxTree ), _symbols );
					discarder.Visit( syntaxTree.GetRoot() );
				}

				foreach ( ISymbol unusedSymbol in _symbols )
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
}