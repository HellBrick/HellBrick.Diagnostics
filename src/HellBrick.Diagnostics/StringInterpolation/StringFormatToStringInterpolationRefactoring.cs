using System;
using System.Composition;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;
using HellBrick.Diagnostics.Utils;

namespace HellBrick.Diagnostics.StringInterpolation
{
	[ExportCodeRefactoringProvider( LanguageNames.CSharp, Name = nameof( StringFormatToStringInterpolationRefactoring ) ), Shared]
	public class StringFormatToStringInterpolationRefactoring : CodeRefactoringProvider
	{
		public sealed override async Task ComputeRefactoringsAsync( CodeRefactoringContext context )
		{
			var root = await context.Document.GetSyntaxRootAsync().ConfigureAwait( false );
			var semanticModel = await context.Document.GetSemanticModelAsync().ConfigureAwait( false );

			var conversions = root
				.EnumerateSelectedNodes<InvocationExpressionSyntax>( context.Span )
				.Where( i => IsInterpolatableStringFormatCall( i, semanticModel ) )
				.Select( i => StringFormatConversion.TryCreateConversion( i ) )
				.Where( i => i.IsSuccess )
				.ToDictionary( c => c.FormatCall, c => c.InterpolatedString );

			if ( conversions.Count > 0 )
			{
				CodeAction refactoring = CodeAction.Create( "Convert to an interpolated string", cancelToken => ConvertDocumentAsync( conversions, context.Document, root ) );
				context.RegisterRefactoring( refactoring );
			}
		}

		private Task<Document> ConvertDocumentAsync( Dictionary<InvocationExpressionSyntax, InterpolatedStringExpressionSyntax> conversionMap, Document document, SyntaxNode root )
		{
			var newRoot = root.ReplaceNodes( conversionMap.Keys, ( original, second ) => conversionMap[ original ] );
			var newDocument = document.WithSyntaxRoot( newRoot );
			return Task.FromResult( newDocument );
		}

		private bool IsInterpolatableStringFormatCall( InvocationExpressionSyntax invocation, SemanticModel semanticModel )
		{
			var symbol = semanticModel.GetSymbolInfo( invocation.Expression ).Symbol as IMethodSymbol;
			return
				symbol?.ContainingType.Name == "String" &&
				symbol?.Name == "Format" &&
				invocation.ArgumentList.Arguments.FirstOrDefault()?.Expression.IsKind( SyntaxKind.StringLiteralExpression ) == true;
		}
	}
}