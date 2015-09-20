using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace HellBrick.Diagnostics.Formatting
{
	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	public class IndentAnalyzer : DiagnosticAnalyzer
	{
		public const string DiagnosticID = IDPrefix.Value + "Indent";

		private const string _title = "Tabs must be used for indentation";
		private const string _category = "Style";

		private static readonly DiagnosticDescriptor _rule = new DiagnosticDescriptor( DiagnosticID, _title, _title, _category, DiagnosticSeverity.Warning, isEnabledByDefault: true );

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create( _rule );

		public override void Initialize( AnalysisContext context )
		{
			context.RegisterSyntaxTreeAction( FindSpaceIndents );
		}

		private void FindSpaceIndents( SyntaxTreeAnalysisContext context )
		{
			SyntaxNode root = context.Tree.GetRoot( context.CancellationToken );
			SpaceIndentFinder badTriviaFinder = new SpaceIndentFinder();
			badTriviaFinder.Visit( root );

			foreach ( SyntaxTrivia badTrivia in badTriviaFinder.SpaceIndentTrivia )
				context.ReportDiagnostic( Diagnostic.Create( _rule, badTrivia.GetLocation() ) );
		}

		private class SpaceIndentFinder : CSharpSyntaxWalker
		{
			private static readonly List<SyntaxTrivia> _emptyList = new List<SyntaxTrivia>();

			private readonly List<SyntaxTrivia> _spaceIndentTrivia = new List<SyntaxTrivia>();
			private bool _isGeneratedFile = false;

			public SpaceIndentFinder()
				: base( SyntaxWalkerDepth.Trivia )
			{
			}

			public IReadOnlyCollection<SyntaxTrivia> SpaceIndentTrivia => _isGeneratedFile ? _emptyList : _spaceIndentTrivia;

			public override void DefaultVisit( SyntaxNode node )
			{
				if ( !_isGeneratedFile )
					base.DefaultVisit( node );
			}

			public override void VisitAttribute( AttributeSyntax node )
			{
				_isGeneratedFile = node.Name.ToString().Contains( "GeneratedCode" );
				if ( !_isGeneratedFile )
					base.VisitAttribute( node );
			}

			public override void VisitLeadingTrivia( SyntaxToken token )
			{
				if ( token.HasLeadingTrivia )
				{
					var invalidWhitespaceTrivia = token.LeadingTrivia
						.Where( t => t.IsKind( SyntaxKind.WhitespaceTrivia ) )
						.Where( t => t.ToString().StartsWith( " " ) );

					foreach ( var spacey in invalidWhitespaceTrivia )
					{
						_spaceIndentTrivia.Add( spacey );
					}
				}

				base.VisitLeadingTrivia( token );
			}
		}
	}
}