using System.Collections.Generic;
using System.Collections.Immutable;
using HellBrick.Diagnostics.Utils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
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
			context.RegisterCodeBlockStartAction<SyntaxKind>( codeBlockStartcontext => AnalyzeCodeBlock( codeBlockStartcontext ) );
		}

		private void AnalyzeCodeBlock( CodeBlockStartAnalysisContext<SyntaxKind> codeBlockStartContext )
		{
			if ( !codeBlockStartContext.CodeBlock.IsKind( SyntaxKind.MethodDeclaration ) && !codeBlockStartContext.CodeBlock.IsKind( SyntaxKind.ConstructorDeclaration ) )
				return;

			IMethodSymbol methodSymbol = codeBlockStartContext.OwningSymbol as IMethodSymbol;
			if ( !( methodSymbol?.Parameters.Length > 0 ) || methodSymbol.IsOverride || methodSymbol.IsVirtual || methodSymbol.IsEntryPoint() || methodSymbol.ImplementsInterface() )
				return;

			ParameterTracker tracker = new ParameterTracker( methodSymbol.Parameters );
			codeBlockStartContext.RegisterCodeBlockEndAction( codeBlockContext => tracker.ReportUnusedParameters( codeBlockContext ) );
			codeBlockStartContext.RegisterSyntaxNodeAction( nodeContext => tracker.TryDiscardReferencedParameter( nodeContext ), SyntaxKind.IdentifierName );
		}

		private class ParameterTracker
		{
			private readonly HashSet<IParameterSymbol> _trackedParameters;

			public ParameterTracker( ImmutableArray<IParameterSymbol> parameters )
			{
				_trackedParameters = parameters.ToHashSet();
			}

			public void TryDiscardReferencedParameter( SyntaxNodeAnalysisContext nodeContext )
			{
				if ( _trackedParameters.Count > 0 )
				{
					IParameterSymbol symbol = nodeContext.SemanticModel.GetSymbolInfo( nodeContext.Node ).Symbol as IParameterSymbol;
					_trackedParameters.Remove( symbol );
				}
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
