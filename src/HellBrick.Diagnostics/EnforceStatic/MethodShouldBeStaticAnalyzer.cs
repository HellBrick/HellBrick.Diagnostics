using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace HellBrick.Diagnostics.EnforceStatic
{
	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	public class MethodShouldBeStaticAnalyzer : DiagnosticAnalyzer
	{
		public const string DiagnosticId = IDPrefix.Value + "MethodShouldBeStatic";

		private static readonly DiagnosticDescriptor _rule = new DiagnosticDescriptor
		(
			DiagnosticId,
			"Method should be static",
			"'{0}' should be static",
			DiagnosticCategory.Design,
			DiagnosticSeverity.Warning,
			isEnabledByDefault: true
		);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create( _rule );

		public override void Initialize( AnalysisContext context )
		{
			context.EnableConcurrentExecution();
			context.ConfigureGeneratedCodeAnalysis( GeneratedCodeAnalysisFlags.None );

			context.RegisterOperationBlockAction( c => ReportNonStaticMethodThatShouldBeStatic( c ) );
		}

		private void ReportNonStaticMethodThatShouldBeStatic( OperationBlockAnalysisContext context )
		{
			if
			(
				context.OwningSymbol is IMethodSymbol methodSymbol
				&& !methodSymbol.IsStatic
				&& methodSymbol.ExplicitInterfaceImplementations.IsDefaultOrEmpty
				&& methodSymbol.DeclaredAccessibility == Accessibility.Private
				&& context.OperationBlocks[ 0 ] is var methodBlock
				&& methodBlock.Syntax.Parent is MethodDeclarationSyntax methodDeclaration
				&& !methodDeclaration.Modifiers.Any( SyntaxKind.PartialKeyword )
			)
			{
				Walker walker = new Walker( methodSymbol );
				walker.Visit( methodBlock );

				if ( !walker.ReferencesInstanceMembers )
				{
					Diagnostic diagnostic = Diagnostic.Create( _rule, methodDeclaration.Identifier.GetLocation(), methodSymbol.Name );
					context.ReportDiagnostic( diagnostic );
				}
			}
		}

		private class Walker : OperationWalker
		{
			private readonly IMethodSymbol _methodSymbol;

			public Walker( IMethodSymbol methodSymbol ) => _methodSymbol = methodSymbol;

			public bool ReferencesInstanceMembers { get; private set; } = false;

			public override void VisitInstanceReference( IInstanceReferenceOperation operation ) => ReferencesInstanceMembers = true;

			public override void VisitNameOf( INameOfOperation operation )
			{
				// Stop the walking: we can reference whatever we want in nameof().
			}
		}
	}
}
