using System.Collections.Immutable;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace HellBrick.Diagnostics.ConfigureAwait
{
	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	public class TaskAwaiterAnalyzer : DiagnosticAnalyzer
	{
		private static readonly SimpleNameSyntax _configureAwaitName = (SimpleNameSyntax) ParseName( nameof( Task.ConfigureAwait ) );
		private static readonly ArgumentListSyntax _configureAwaitCandidateArgumentList = ArgumentList
		(
			SeparatedList<ArgumentSyntax>().Add
			(
				Argument( LiteralExpression( SyntaxKind.FalseLiteralExpression, Token( SyntaxKind.FalseKeyword ) ) )
			)
		);

		public const string DiagnosticID = IDPrefix.Value + "ConfigureAwait";

		private const string _title = "'ConfigureAwait()' is missing";
		private const string _category = DiagnosticCategory.Async;

		private static readonly DiagnosticDescriptor _rule = new DiagnosticDescriptor( DiagnosticID, _title, _title, _category, DiagnosticSeverity.Warning, isEnabledByDefault: true );
		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create( _rule );

		public override void Initialize( AnalysisContext context )
		{
			context.EnableConcurrentExecution();
			context.ConfigureGeneratedCodeAnalysis( GeneratedCodeAnalysisFlags.None );
			context.RegisterSyntaxNodeAction( EnsureConfigureAwait, SyntaxKind.AwaitExpression );
		}

		private void EnsureConfigureAwait( SyntaxNodeAnalysisContext context )
		{
			AwaitExpressionSyntax awaitExpression = context.Node as AwaitExpressionSyntax;

			if ( CanResolveConfigureAwaitCall() )
			{
				Diagnostic diagnostic = Diagnostic.Create( _rule, awaitExpression.AwaitKeyword.GetLocation() );
				context.ReportDiagnostic( diagnostic );
			}

			bool CanResolveConfigureAwaitCall()
			{
				SyntaxNode configureAwaitCandidate
					= InvocationExpression
					(
						MemberAccessExpression( SyntaxKind.SimpleMemberAccessExpression, awaitExpression.Expression, _configureAwaitName ),
						_configureAwaitCandidateArgumentList
					);

				SymbolInfo configureAwaitSymbolInfo = context.SemanticModel.GetSpeculativeSymbolInfo
				(
					awaitExpression.Expression.Span.Start,
					configureAwaitCandidate,
					SpeculativeBindingOption.BindAsExpression
				);

				return configureAwaitSymbolInfo.Symbol != null;
			}
		}
	}
}
