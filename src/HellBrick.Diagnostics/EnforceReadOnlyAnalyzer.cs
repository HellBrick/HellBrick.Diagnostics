using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace HellBrick.Diagnostics
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class EnforceReadOnlyAnalyzer: DiagnosticAnalyzer
	{
		public const string DiagnosticID = Common.RulePrefix + "EnforceReadOnly";
		private const string _title = "Field can be made read-only";
		private const string _messageFormat = "Field '{0}' can be made read-only";
		private const string _category = "Design";

		private static DiagnosticDescriptor _rule = new DiagnosticDescriptor( DiagnosticID, _title, _messageFormat, _category, DiagnosticSeverity.Warning, isEnabledByDefault: true );

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get ; } = ImmutableArray.Create( _rule );

		public override void Initialize( AnalysisContext context )
		{
			context.RegisterSyntaxNodeAction( EnforceReadOnlyOnClassFields, SyntaxKind.ClassDeclaration );
		}

		private void EnforceReadOnlyOnClassFields( SyntaxNodeAnalysisContext context )
		{
			var classNode = context.Node as ClassDeclarationSyntax;
			var fieldNodes = classNode.Members
				.OfType<FieldDeclarationSyntax>()
				.Where( f => IsReadOnlyCandidate( f ) )
				.ToList();

			if ( fieldNodes.Count == 0 )
				return;

			var fieldSymbols = fieldNodes
				.SelectMany( field => field.DescendantNodes().OfType<VariableDeclaratorSyntax>() )
				.Select( declarator => context.SemanticModel.GetDeclaredSymbol( declarator, context.CancellationToken ) );

			var fieldSymbolMap = new HashSet<ISymbol>( fieldSymbols );

			var methods = classNode.Members
				.OfType<MethodDeclarationSyntax>()
				.ToList();

			foreach ( var method in methods )
			{
				var assignments = EnumerateMethodAssignements( method, context ).ToList();
				foreach ( var targetSymbol in assignments )
				{
					fieldSymbolMap.Remove( targetSymbol );

					if ( fieldSymbolMap.Count == 0 )
						return;
				}
			}

			foreach ( var fieldSymbol in fieldSymbolMap )
				context.ReportDiagnostic( Diagnostic.Create( _rule, fieldSymbol.Locations[ 0 ], fieldSymbol.Name ) );
		}

		private bool IsReadOnlyCandidate( FieldDeclarationSyntax field )
		{
			foreach ( var modifier in field.Modifiers )
			{
				//	Is already const or read-only.
				if ( modifier.IsKind( SyntaxKind.ConstKeyword ) || modifier.IsKind( SyntaxKind.ReadOnlyKeyword ) )
					return false;

				//	The field is not private => its value can be set outside the class.
				if ( modifier.IsKind( SyntaxKind.PublicKeyword ) || modifier.IsKind( SyntaxKind.InternalKeyword ) || modifier.IsKind( SyntaxKind.ProtectedKeyword ) )
					return false;
			}

			return true;
		}

		private IEnumerable<ISymbol> EnumerateMethodAssignements( BaseMethodDeclarationSyntax method, SyntaxNodeAnalysisContext context )
		{
			return method.DescendantNodes()
				.OfType<AssignmentExpressionSyntax>()
				.Select( ass => context.SemanticModel.GetSymbolInfo( ass.Left ).Symbol?.OriginalDefinition )
				.Where( s => s != null );
		}
	}
}
