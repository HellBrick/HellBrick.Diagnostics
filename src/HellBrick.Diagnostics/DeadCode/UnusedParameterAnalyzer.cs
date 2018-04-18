using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using HellBrick.Diagnostics.Utils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace HellBrick.Diagnostics.DeadCode
{
	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	public class UnusedParameterAnalyzer : DiagnosticAnalyzer
	{
		public const string ID = IDPrefix.Value + "UnusedParameter";
		private static readonly DiagnosticDescriptor _rule
			= new DiagnosticDescriptor
			(
				ID,
				"Unused parameter",
				"Parameter '{0}' can be removed",
				DiagnosticCategory.Design,
				DiagnosticSeverity.Hidden,
				isEnabledByDefault: true,
				customTags: WellKnownDiagnosticTags.Unnecessary
			);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create( _rule );

		public override void Initialize( AnalysisContext context )
		{
			context.ConfigureGeneratedCodeAnalysis( GeneratedCodeAnalysisFlags.None );
			context.RegisterCodeBlockAction( codeBlockContext => AnalyzeCodeBlock( codeBlockContext ) );
		}

		private void AnalyzeCodeBlock( CodeBlockAnalysisContext codeBlockContext )
		{
			if ( !codeBlockContext.CodeBlock.IsKind( SyntaxKind.MethodDeclaration ) && !codeBlockContext.CodeBlock.IsKind( SyntaxKind.ConstructorDeclaration ) )
				return;

			// If interface method declaration has a parameter with a default value, it's classified as a code block for some reason and needs to be ignored.
			if ( !codeBlockContext.CodeBlock.Parent.IsKind( SyntaxKind.ClassDeclaration ) )
				return;

			if ( !( codeBlockContext.OwningSymbol is IMethodSymbol methodSymbol ) )
				return;

			if ( methodSymbol.IsOverride || methodSymbol.IsVirtual || methodSymbol.IsEntryPoint() || methodSymbol.ImplementsInterface() )
				return;

			HashSet<IParameterSymbol> parametersToExamine
				=
				(
					methodSymbol.IsExtensionMethod
					? methodSymbol.Parameters.Skip( 1 )
					: methodSymbol.Parameters
				)
				.ToHashSet();

			if ( parametersToExamine.Count <= 0 )
				return;

			ParameterTracker tracker = new ParameterTracker( parametersToExamine, codeBlockContext.SemanticModel );
			tracker.Visit( codeBlockContext.CodeBlock );
			tracker.ReportUnusedParameters( codeBlockContext );
		}

		private class ParameterTracker : CSharpSyntaxWalker
		{
			private readonly HashSet<IParameterSymbol> _trackedParameters;
			private readonly SemanticModel _semanticModel;

			public ParameterTracker( HashSet<IParameterSymbol> parameters, SemanticModel semanticModel )
			{
				_trackedParameters = parameters;
				_semanticModel = semanticModel;
			}

			public override void Visit( SyntaxNode node )
			{
				if ( _trackedParameters.Count > 0 )
					base.Visit( node );
			}

			public override void VisitIdentifierName( IdentifierNameSyntax node )
			{
				IParameterSymbol symbol = _semanticModel.GetSymbolInfo( node ).Symbol as IParameterSymbol;
				_trackedParameters.Remove( symbol );
			}

			public void ReportUnusedParameters( CodeBlockAnalysisContext codeBlockContext )
			{
				foreach ( IParameterSymbol unusedParameter in _trackedParameters )
					ReportUnusedParameter( codeBlockContext, unusedParameter );
			}

			private void ReportUnusedParameter( CodeBlockAnalysisContext codeBlockContext, IParameterSymbol unusedParameter )
			{
				// I've seen it empty when the parameter list is being edited.
				if ( unusedParameter.DeclaringSyntaxReferences.Length == 0 )
					return;

				SyntaxReference syntaxReference = unusedParameter.DeclaringSyntaxReferences[ 0 ];
				Diagnostic diagnostic = Diagnostic.Create( _rule, syntaxReference.SyntaxTree.GetLocation( syntaxReference.Span ), unusedParameter.Name );
				codeBlockContext.ReportDiagnostic( diagnostic );
			}
		}
	}
}
