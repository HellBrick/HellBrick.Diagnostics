using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace HellBrick.Diagnostics.StructDeclarations
{
	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	public class StructAnalyzer : DiagnosticAnalyzer
	{
		private static readonly ImmutableArray<SyntaxKind> _syntaxKinds = new SyntaxKind[] { SyntaxKind.StructDeclaration }.ToImmutableArray();

		private static readonly ImmutableArray<IStructSyntaxNodeAnalyzer> _analyzers
			= new IStructSyntaxNodeAnalyzer[]
			{
				new ReadOnlyStructFieldsAnalyzer(),
				new ReadOnlyStructPropertyAnalyzer(),
				new StructEquatabilityAnalyzer()
			}
			.ToImmutableArray();

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }
			= _analyzers
			.SelectMany( a => a.SupportedDiagnostics )
			.ToImmutableArray();

		public override void Initialize( AnalysisContext context )
		{
			context.ConfigureGeneratedCodeAnalysis( GeneratedCodeAnalysisFlags.None );
			context.RegisterSyntaxNodeAction( syntaxNodeContext => AnalyzeSyntaxNode( syntaxNodeContext ), _syntaxKinds );
		}

		private static void AnalyzeSyntaxNode( SyntaxNodeAnalysisContext syntaxNodeContext )
		{
			StructDeclarationSyntax structDeclaration = syntaxNodeContext.Node as StructDeclarationSyntax;
			ITypeSymbol structType = syntaxNodeContext.SemanticModel.GetDeclaredSymbol( structDeclaration );

			if ( IsEnumerator( structType ) )
				return;

			foreach ( IStructSyntaxNodeAnalyzer analyzer in _analyzers )
				analyzer.AnalyzeStructSyntaxNode( structDeclaration, structType, syntaxNodeContext );
		}

		private static bool IsEnumerator( ITypeSymbol typeSymbol )
			=> typeSymbol
			.AllInterfaces
			.Any( i => i.MetadataName == "IEnumerator`1" );
	}
}
