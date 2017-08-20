using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace HellBrick.Diagnostics.ValueTypeToNullComparing
{
	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	public class ValueTypeToNullComparingAnalyzer : DiagnosticAnalyzer
	{
		public const string DiagnosticId = IDPrefix.Value + "ValueTypeToNullComparing";
		private const string _title = "Value type to null comparing";

		private static readonly ImmutableArray<SyntaxKind> _analysisTarget = ImmutableArray.Create( SyntaxKind.EqualsExpression, SyntaxKind.NotEqualsExpression );

		private static readonly DiagnosticDescriptor _rule = new DiagnosticDescriptor
		(
			id: DiagnosticId,
			title: _title,
			messageFormat: _title,
			category: DiagnosticCategory.Design,
			defaultSeverity: DiagnosticSeverity.Warning,
			isEnabledByDefault: true
		);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create( _rule );

		public override void Initialize( AnalysisContext context )
		{
			context.ConfigureGeneratedCodeAnalysis( GeneratedCodeAnalysisFlags.None );
			context.RegisterSyntaxNodeAction( FindFlagrantViolationOfCodeStyle, _analysisTarget );
		}

		private void FindFlagrantViolationOfCodeStyle( SyntaxNodeAnalysisContext context )
		{
			BinaryExpressionSyntax typedNode = (BinaryExpressionSyntax) context.Node;

			bool shouldReportDiagnostic =
				NodesAreNullAndValueType( typedNode.Right, typedNode.Left, context.SemanticModel ) ||
				NodesAreNullAndValueType( typedNode.Left, typedNode.Right, context.SemanticModel );

			if ( shouldReportDiagnostic )
			{
				Diagnostic diagnostic = Diagnostic.Create( _rule, context.Node.GetLocation() );
				context.ReportDiagnostic( diagnostic );
			}
		}

		private static bool NodesAreNullAndValueType( ExpressionSyntax left, ExpressionSyntax right, SemanticModel semanticModel )
		{
			ITypeSymbol rightType = semanticModel.GetTypeInfo( right ).Type;
			return
				left.IsKind( SyntaxKind.NullLiteralExpression )
				&& rightType != null
				&& rightType.TypeKind == TypeKind.Struct
				&& !( rightType.ContainingNamespace.Name == "System" && rightType.Name == "Nullable" );
		}

	}
}
