using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
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

		public override void Initialize( AnalysisContext context )
		{
			context.ConfigureGeneratedCodeAnalysis( GeneratedCodeAnalysisFlags.None );
			context.RegisterCompilationStartAction( StartAnalysis );
		}

		private void StartAnalysis( CompilationStartAnalysisContext context )
		{
			UnusedSymbolAnalysisContext analysisContext = new UnusedSymbolAnalysisContext();
			context.RegisterSyntaxNodeAction( nodeContext => analysisContext.DisableInternalTrackingIfInteralsVisibleToIsDeclared( nodeContext ), SyntaxKind.Attribute );
			context.RegisterSymbolAction( symbolContext => analysisContext.TrackSymbol( symbolContext.Symbol ), _symbolKindsToTrack );
			context.RegisterSemanticModelAction( semanticContext => analysisContext.TrackReferencedSymbols( semanticContext ) );
			context.RegisterCompilationEndAction( compilationContext => analysisContext.ReportDiagnosticsForUnusedSymbols( compilationContext ) );
		}

		private class UnusedSymbolAnalysisContext
		{
			private readonly Dictionary<SyntaxTree, HashSet<ISymbol>> _symbolsToReportOnSemanticModelBuilt = new Dictionary<SyntaxTree, HashSet<ISymbol>>();
			private readonly HashSet<ISymbol> _symbolsToReportOnCompilationEnd = new HashSet<ISymbol>();
			private readonly HashSet<ISymbol> _referencedSymbols = new HashSet<ISymbol>();

			private bool _hasInternalsVisibleTo = false;

			public void TrackSymbol( ISymbol symbol )
			{
				if ( !IsCandidate( symbol ) )
					return;

				if ( CanReportOnSemanticModelBuilt( symbol ) )
				{
					SyntaxTree declaringTree = symbol.DeclaringSyntaxReferences[ 0 ].SyntaxTree;
					if ( !_symbolsToReportOnSemanticModelBuilt.TryGetValue( declaringTree, out HashSet<ISymbol> currentTreeSymbols ) )
					{
						currentTreeSymbols = new HashSet<ISymbol>();
						_symbolsToReportOnSemanticModelBuilt.Add( declaringTree, currentTreeSymbols );
					}

					currentTreeSymbols.Add( symbol );
				}
				else
					_symbolsToReportOnCompilationEnd.Add( symbol );
			}

			private static bool IsCandidate( ISymbol symbol ) =>
				( symbol.DeclaredAccessibility == Accessibility.Private || symbol.DeclaredAccessibility == Accessibility.Internal ) &&
				!symbol.IsOverride &&
				!IsIgnoredMethod( symbol ) &&
				!symbol.ImplementsInterface();

			private static bool IsIgnoredMethod( ISymbol symbol )
			{
				IMethodSymbol methodSymbol = symbol as IMethodSymbol;
				return
					methodSymbol != null &&
					(
						methodSymbol.MethodKind == MethodKind.PropertyGet ||
						methodSymbol.MethodKind == MethodKind.PropertySet ||
						methodSymbol.MetadataName == ".cctor" ||
						methodSymbol.IsEntryPoint()
					);
			}

			private static bool CanReportOnSemanticModelBuilt( ISymbol symbol )
				=> symbol.DeclaredAccessibility == Accessibility.Private
				&& symbol.DeclaringSyntaxReferences.Length == 1
				&& symbol.ContainingSymbol?.DeclaringSyntaxReferences.Length == 1;

			public void TrackReferencedSymbols( SemanticModelAnalysisContext semanticContext )
			{
				ReferencedSymbolFinder referenceFinder = new ReferencedSymbolFinder( semanticContext.SemanticModel );
				referenceFinder.Visit( semanticContext.SemanticModel.SyntaxTree.GetRoot() );
				foreach ( ISymbol referencedSymbol in referenceFinder.ReferencedSymbols )
					_referencedSymbols.Add( referencedSymbol );

				if ( _symbolsToReportOnSemanticModelBuilt.TryGetValue( semanticContext.SemanticModel.SyntaxTree, out HashSet<ISymbol> currentTreeSymbols ) )
				{
					currentTreeSymbols.ExceptWith( referenceFinder.ReferencedSymbols );
					ReportDiagnostics( currentTreeSymbols, d => semanticContext.ReportDiagnostic( d ) );
				}
			}

			public void ReportDiagnosticsForUnusedSymbols( CompilationAnalysisContext context )
			{
				if ( _hasInternalsVisibleTo )
					_symbolsToReportOnCompilationEnd.RemoveWhere( s => s.DeclaredAccessibility == Accessibility.Internal );

				_symbolsToReportOnCompilationEnd.ExceptWith( _referencedSymbols );
				ReportDiagnostics( _symbolsToReportOnCompilationEnd, d => context.ReportDiagnostic( d ) );
			}

			private void ReportDiagnostics( IEnumerable<ISymbol> unusedSymbols, Action<Diagnostic> reportDiagnosticAction )
			{
				foreach ( ISymbol unusedSymbol in unusedSymbols )
				{
					ISymbol definition = unusedSymbol.OriginalDefinition;
					foreach ( SyntaxReference declarationReference in definition.DeclaringSyntaxReferences )
					{
						Location diagnosticLocation = GetDiagnosticLocation( declarationReference );
						Diagnostic diagnostic = Diagnostic.Create( _rule, diagnosticLocation, unusedSymbol.ToString() );
						reportDiagnosticAction( diagnostic );
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
					if ( leadingTrivia != default )
						return Location.Create( declarationReference.SyntaxTree, TextSpan.FromBounds( leadingTrivia.FullSpan.Start, definitionNode.Span.End ) );
				}

				return definitionNode.GetLocation();
			}

			public void DisableInternalTrackingIfInteralsVisibleToIsDeclared( SyntaxNodeAnalysisContext nodeContext )
				=> _hasInternalsVisibleTo = _hasInternalsVisibleTo || IsInternalsVisibleToAttribute( nodeContext );

			private static bool IsInternalsVisibleToAttribute( SyntaxNodeAnalysisContext nodeContext )
				=> IsInternalsVisibleToAttribute( nodeContext.Node as AttributeSyntax, nodeContext.SemanticModel );

			private static bool IsInternalsVisibleToAttribute( AttributeSyntax attribute, SemanticModel semanticModel )
				=> GetAttributeMetadataName( attribute, semanticModel ) == "InternalsVisibleToAttribute";

			private static string GetAttributeMetadataName( AttributeSyntax attribute, SemanticModel semanticModel )
				=> semanticModel
				.GetSymbolInfo( attribute )
				.Symbol
				?.ContainingType
				?.MetadataName;
		}
	}
}
