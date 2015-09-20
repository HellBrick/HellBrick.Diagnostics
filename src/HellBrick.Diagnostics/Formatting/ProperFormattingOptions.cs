using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;

namespace HellBrick.Diagnostics.Formatting
{
	internal static class ProperFormattingOptions
	{
		public static OptionSet Instance { get; } = new AdhocWorkspace().Options
			.WithChangedOption( FormattingOptions.UseTabs, LanguageNames.CSharp, true )

			.WithChangedOption( CSharpFormattingOptions.IndentBlock, true )
			.WithChangedOption( CSharpFormattingOptions.IndentBraces, false )
			.WithChangedOption( CSharpFormattingOptions.IndentSwitchCaseSection, true )
			.WithChangedOption( CSharpFormattingOptions.IndentSwitchSection, true )

			.WithChangedOption( CSharpFormattingOptions.LabelPositioning, LabelPositionOptions.OneLess )

			.WithChangedOption( CSharpFormattingOptions.NewLineForCatch, true )
			.WithChangedOption( CSharpFormattingOptions.NewLineForClausesInQuery, true )
			.WithChangedOption( CSharpFormattingOptions.NewLineForElse, true )
			.WithChangedOption( CSharpFormattingOptions.NewLineForFinally, true )
			.WithChangedOption( CSharpFormattingOptions.NewLineForMembersInAnonymousTypes, true )
			.WithChangedOption( CSharpFormattingOptions.NewLineForMembersInObjectInit, true )
			.WithChangedOption( CSharpFormattingOptions.NewLinesForBracesInAnonymousMethods, true )
			.WithChangedOption( CSharpFormattingOptions.NewLinesForBracesInAnonymousTypes, true )
			.WithChangedOption( CSharpFormattingOptions.NewLinesForBracesInControlBlocks, true )
			.WithChangedOption( CSharpFormattingOptions.NewLinesForBracesInLambdaExpressionBody, true )
			.WithChangedOption( CSharpFormattingOptions.NewLinesForBracesInMethods, true )
			.WithChangedOption( CSharpFormattingOptions.NewLinesForBracesInObjectCollectionArrayInitializers, true )
			.WithChangedOption( CSharpFormattingOptions.NewLinesForBracesInTypes, true )

			.WithChangedOption( CSharpFormattingOptions.SpaceAfterCast, true )
			.WithChangedOption( CSharpFormattingOptions.SpaceAfterColonInBaseTypeDeclaration, true )
			.WithChangedOption( CSharpFormattingOptions.SpaceAfterComma, true )
			.WithChangedOption( CSharpFormattingOptions.SpaceAfterControlFlowStatementKeyword, true )
			.WithChangedOption( CSharpFormattingOptions.SpaceAfterDot, false )
			.WithChangedOption( CSharpFormattingOptions.SpaceAfterMethodCallName, false )
			.WithChangedOption( CSharpFormattingOptions.SpaceAfterSemicolonsInForStatement, true )

			.WithChangedOption( CSharpFormattingOptions.SpaceBeforeColonInBaseTypeDeclaration, true )
			.WithChangedOption( CSharpFormattingOptions.SpaceBeforeComma, false )
			.WithChangedOption( CSharpFormattingOptions.SpaceBeforeDot, false )
			.WithChangedOption( CSharpFormattingOptions.SpaceBeforeOpenSquareBracket, false )
			.WithChangedOption( CSharpFormattingOptions.SpaceBeforeSemicolonsInForStatement, false )
			
			.WithChangedOption( CSharpFormattingOptions.SpaceBetweenEmptyMethodCallParentheses, false )
			.WithChangedOption( CSharpFormattingOptions.SpaceBetweenEmptyMethodDeclarationParentheses, false )
			.WithChangedOption( CSharpFormattingOptions.SpaceBetweenEmptySquareBrackets, false )

			.WithChangedOption( CSharpFormattingOptions.SpacesIgnoreAroundVariableDeclaration, false )

			.WithChangedOption( CSharpFormattingOptions.SpaceWithinCastParentheses, true )
			.WithChangedOption( CSharpFormattingOptions.SpaceWithinExpressionParentheses, true )
			.WithChangedOption( CSharpFormattingOptions.SpaceWithinMethodCallParentheses, true )
			.WithChangedOption( CSharpFormattingOptions.SpaceWithinMethodDeclarationParenthesis, true )
			.WithChangedOption( CSharpFormattingOptions.SpaceWithinOtherParentheses, true )
			.WithChangedOption( CSharpFormattingOptions.SpaceWithinSquareBrackets, true )
			.WithChangedOption( CSharpFormattingOptions.SpacingAfterMethodDeclarationName, false )
			.WithChangedOption( CSharpFormattingOptions.SpacingAroundBinaryOperator, BinaryOperatorSpacingOptions.Single )
			
			.WithChangedOption( CSharpFormattingOptions.WrappingKeepStatementsOnSingleLine, true )
			.WithChangedOption( CSharpFormattingOptions.WrappingPreserveSingleLine, true );
	}
}
