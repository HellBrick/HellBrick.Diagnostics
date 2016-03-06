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
		private static readonly ImmutableArray<SymbolKind> _symbolKindsToTrack =
			ImmutableArray.Create
			(
				SymbolKind.Event,
				SymbolKind.Field,
				SymbolKind.Method,
				SymbolKind.Property
			);

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
			context.RegisterSymbolAction( symbolContext => analysisContext.TrackSymbol( symbolContext.Symbol ), _symbolKindsToTrack );
			context.RegisterSemanticModelAction( semanticContext => analysisContext.DiscardReferencedSymbols( semanticContext ) );
			context.RegisterCompilationEndAction( compilationContext => analysisContext.ReportDiagnosticsForUnusedSymbols( compilationContext ) );
		}

		private class UnusedSymbolAnalysisContext
		{
			private HashSet<ISymbol> _symbolsToReportOnCompilationEnd = new HashSet<ISymbol>();

			public void TrackSymbol( ISymbol symbol )
			{
				if ( IsCandidate( symbol ) )
						_symbolsToReportOnCompilationEnd.Add( symbol );
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
						methodSymbol.MetadataName == ".cctor"
					);
			}

			public void DiscardReferencedSymbols( SemanticModelAnalysisContext semanticContext )
			{
				ReferencedSymbolFinder referenceFinder = new ReferencedSymbolFinder( semanticContext.SemanticModel );
				referenceFinder.Visit( semanticContext.SemanticModel.SyntaxTree.GetRoot() );
				_symbolsToReportOnCompilationEnd.ExceptWith( referenceFinder.ReferencedSymbols );
			}

			public void ReportDiagnosticsForUnusedSymbols( CompilationAnalysisContext context )
			{
				foreach ( ISymbol unusedSymbol in _symbolsToReportOnCompilationEnd )
				{
					ISymbol definition = unusedSymbol.OriginalDefinition;
					foreach ( SyntaxReference declarationReference in definition.DeclaringSyntaxReferences )
					{
						Location diagnosticLocation = GetDiagnosticLocation( declarationReference );
						Diagnostic diagnostic = Diagnostic.Create( _rule, diagnosticLocation, unusedSymbol.ToString() );
						context.ReportDiagnostic( diagnostic );
					}
				}
			}

			private Location GetDiagnosticLocation( SyntaxReference declarationReference )
			{
				SyntaxNode definitionNode = declarationReference.GetSyntax();
				if ( definitionNode.IsKind( SyntaxKind.VariableDeclarator ) )
				{
					FieldDeclarationSyntax fieldDeclarationNode = definitionNode.FirstAncestorOrSelf<FieldDeclarationSyntax>();
					if ( fieldDeclarationNode != null )
						definitionNode = fieldDeclarationNode;
				}

				if ( definitionNode.HasStructuredTrivia && definitionNode.HasLeadingTrivia )
				{
					SyntaxTrivia leadingTrivia = definitionNode.GetLeadingTrivia().FirstOrDefault( t => t.IsKind( SyntaxKind.SingleLineDocumentationCommentTrivia ) );
					if ( leadingTrivia != default( SyntaxTrivia ) )
						return Location.Create( declarationReference.SyntaxTree, TextSpan.FromBounds( leadingTrivia.FullSpan.Start, definitionNode.Span.End ) );
				}

				return definitionNode.GetLocation();
			}
		}
	}
}