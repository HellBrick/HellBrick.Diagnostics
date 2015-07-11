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
	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	public class EnforceReadOnlyAnalyzer : DiagnosticAnalyzer
	{
		public const string DiagnosticID = Common.RulePrefix + "EnforceReadOnly";
		private const string _title = "Field can be made read-only";
		private const string _messageFormat = "Field '{0}' can be made read-only";
		private const string _category = "Design";

		private static DiagnosticDescriptor _rule = new DiagnosticDescriptor( DiagnosticID, _title, _messageFormat, _category, DiagnosticSeverity.Warning, isEnabledByDefault: true );

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create( _rule );

		public override void Initialize( AnalysisContext context )
		{
			context.RegisterSyntaxNodeAction( EnforceReadOnlyOnClassFields, SyntaxKind.ClassDeclaration );
		}

		private void EnforceReadOnlyOnClassFields( SyntaxNodeAnalysisContext context )
		{
			var classNode = context.Node as ClassDeclarationSyntax;
			if ( classNode.Modifiers.Any( mod => mod.IsKind( SyntaxKind.PartialKeyword ) ) )
				return;

			HashSet<ISymbol> fieldSymbols = FindFieldSymbols( context, classNode );
			if ( fieldSymbols.Count == 0 )
				return;

			var assignees = EnumerateAssignees( classNode );

			foreach ( var assignee in assignees )
			{
				if ( !IsAssignedInsideConstructor( classNode, assignee ) )
				{
					//	If this is an indexer assignment, the symbol located to the left of the [] should be looked up.
					var underlyingAssignee = ( assignee as ElementAccessExpressionSyntax )?.Expression ?? assignee;
					var symbol = context.SemanticModel.GetSymbolInfo( underlyingAssignee ).Symbol?.OriginalDefinition;

					//	 The only chance for the assignment not to break the read-only limitations outside the ctor body is to make an indexer assignment to a reference type
					bool isAssignmentAllowed = underlyingAssignee != assignee && ( symbol as IFieldSymbol )?.Type?.IsReferenceType == true;

					if ( !isAssignmentAllowed )
						fieldSymbols.Remove( symbol );
				}

				if ( fieldSymbols.Count == 0 )
					return;
			}

			foreach ( var fieldSymbol in fieldSymbols )
				context.ReportDiagnostic( Diagnostic.Create( _rule, fieldSymbol.Locations[ 0 ], fieldSymbol.Name ) );
		}

		private HashSet<ISymbol> FindFieldSymbols( SyntaxNodeAnalysisContext context, ClassDeclarationSyntax classNode )
		{
			var fieldNodes = classNode.Members
				.OfType<FieldDeclarationSyntax>()
				.Where( f => IsReadOnlyCandidate( f ) )
				.ToList();

			var fieldSymbols = fieldNodes
				.SelectMany( field => field.DescendantNodes().OfType<VariableDeclaratorSyntax>() )
				.Select( declarator => context.SemanticModel.GetDeclaredSymbol( declarator, context.CancellationToken ) );

			var fieldSymbolMap = new HashSet<ISymbol>( fieldSymbols );
			return fieldSymbolMap;
		}

		private bool IsReadOnlyCandidate( FieldDeclarationSyntax field )
		{
			if ( field.DescendantNodes().OfType<VariableDeclaratorSyntax>().Count() > 1 )
				return false;

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

		private static IEnumerable<ExpressionSyntax> EnumerateAssignees( SyntaxNode method )
		{
			var assigned = method.DescendantNodes()
				.OfType<AssignmentExpressionSyntax>()
				.Select( ass => ass.Left );

			var passedByRef = method.DescendantNodes()
				.OfType<ArgumentSyntax>()
				.Where( arg => !arg.RefOrOutKeyword.IsKind( SyntaxKind.None ) )
				.Select( arg => arg.Expression );

			var preIncremented = method.DescendantNodes()
				.OfType<PrefixUnaryExpressionSyntax>()
				.Select( ex => ex.Operand );

			var postIncremented = method.DescendantNodes()
				.OfType<PostfixUnaryExpressionSyntax>()
				.Select( ex => ex.Operand );

			var assigneeExpressions = assigned.Concat( passedByRef ).Concat( preIncremented ).Concat( postIncremented );
			return assigneeExpressions;
		}

		private static bool IsAssignmentAllowed( ClassDeclarationSyntax classNode, ExpressionSyntax assignee )
		{
			return
				IsAssignedInsideConstructor( classNode, assignee ) ||
				IsIndexerAssignmentToReferenceType( assignee );
		}

		private static bool IsAssignedInsideConstructor( ClassDeclarationSyntax classNode, ExpressionSyntax assignee )
		{
			var ownerNode = assignee.FirstAncestorOrSelf<CSharpSyntaxNode>( n =>
				n is MethodDeclarationSyntax ||
				n is ConstructorDeclarationSyntax ||
				n is ParenthesizedLambdaExpressionSyntax ||
				n is SimpleLambdaExpressionSyntax );

			return
				ownerNode is ConstructorDeclarationSyntax &&
				ownerNode.FirstAncestorOrSelf<ClassDeclarationSyntax>() == classNode;	//	this check ensures that we're not dealing with a nested class
		}

		private static bool IsIndexerAssignmentToReferenceType( ExpressionSyntax assignee )
		{
			return
				assignee is ElementAccessExpressionSyntax;
		}
	}
}
