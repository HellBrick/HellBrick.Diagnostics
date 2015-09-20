﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HellBrick.Diagnostics.Utils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Text;

namespace HellBrick.Diagnostics.Formatting
{
	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	public class FormattingAnalyzer : DiagnosticAnalyzer
	{
		public const string DiagnosticID = IDPrefix.Value + "Formatting";

		private const string _title = "Invalid code formatting";
		private const string _category = "Style";

		private static readonly DiagnosticDescriptor _rule = new DiagnosticDescriptor( DiagnosticID, _title, _title, _category, DiagnosticSeverity.Warning, isEnabledByDefault: true );
		private static readonly AdhocWorkspace _workspace = new AdhocWorkspace();

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create( _rule );

		public override void Initialize( AnalysisContext context )
		{
			context.RegisterSyntaxTreeAction( FindInvalidFormatting );
		}

		private void FindInvalidFormatting( SyntaxTreeAnalysisContext context )
		{
			try
			{
				SyntaxNode root = context.Tree.GetRoot( context.CancellationToken );
				SourceText sourceText = root.GetText();
				AttributeSyntax generatedCodeAttribute = root
					.DescendantNodes()
					.OfType<AttributeSyntax>()
					.FirstOrDefault( attribute => attribute.Name.ToString().Contains( "GeneratedCode" ) );

				if ( generatedCodeAttribute != null )
					return;

				IList<TextChange> changes = Formatter.GetFormattedTextChanges( root, _workspace, ProperFormattingOptions.Instance, context.CancellationToken );

				foreach ( TextChange change in changes )
				{
					//	For the reasons unknown, Formatter suggests changing endline trivia to exactly the same endline trivia %)
					//	This weird check filters away such cases.
					string textToReplace = sourceText.GetSubText( change.Span ).ToString();
					if ( textToReplace == change.NewText )
						continue;

					TextSpan diagnosticSpan = change.Span.IsEmpty ? new TextSpan( change.Span.Start - 1, 2 ) : change.Span;
					context.ReportDiagnostic( Diagnostic.Create( _rule, Location.Create( context.Tree, diagnosticSpan ) ) );
				}
			}
			catch ( OperationCanceledException )
			{
			}
		}
	}
}