using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace HellBrick.Diagnostics.ConfigureAwait
{
	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	public class TaskAwaiterAnalyzer : DiagnosticAnalyzer
	{
		public const string DiagnosticID = IDPrefix.Value + "ConfigureAwait";

		private const string _title = "'ConfigureAwait( false )' is missing";
		private const string _category = DiagnosticCategory.Async;

		private static readonly DiagnosticDescriptor _rule = new DiagnosticDescriptor( DiagnosticID, _title, _title, _category, DiagnosticSeverity.Warning, isEnabledByDefault: true );
		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create( _rule );

		public override void Initialize( AnalysisContext context )
		{
			context.ConfigureGeneratedCodeAnalysis( GeneratedCodeAnalysisFlags.None );
			context.RegisterSyntaxNodeAction( EnsureConfigureAwait, SyntaxKind.AwaitExpression );
		}

		private void EnsureConfigureAwait( SyntaxNodeAnalysisContext context )
		{
			AwaitExpressionSyntax awaitExpression = context.Node as AwaitExpressionSyntax;
			AwaitExpressionInfo awaitInfo = context.SemanticModel.GetAwaitExpressionInfo( awaitExpression );

			if ( IsTaskAwaiter( awaitInfo.GetAwaiterMethod?.ReturnType ) )
			{
				Diagnostic diagnostic = Diagnostic.Create( _rule, awaitExpression.Expression.GetLocation() );
				context.ReportDiagnostic( diagnostic );
			}
		}

		private static bool IsTaskAwaiter( ITypeSymbol awaiterType ) =>
			awaiterType?.Name == "TaskAwaiter" &&
			awaiterType?.ContainingNamespace.ToDisplayString() == "System.Runtime.CompilerServices";
	}
}
