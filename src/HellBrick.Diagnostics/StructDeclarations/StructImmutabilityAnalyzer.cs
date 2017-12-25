using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace HellBrick.Diagnostics.StructDeclarations
{
	internal class StructImmutabilityAnalyzer : IStructSyntaxNodeAnalyzer
	{
		public const string DiagnosticId = StructIDPrefix.Value + "ImmutableNonReadonly";
		private static readonly DiagnosticDescriptor _rule = new DiagnosticDescriptor( DiagnosticId, "Immutable structs should be readonly", "{0} is immutable, therefore it should be marked as readonly", DiagnosticCategory.Design, DiagnosticSeverity.Warning, isEnabledByDefault: true );
		public ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create( _rule );

		public void AnalyzeStructSyntaxNode( StructDeclarationSyntax structDeclaration, ITypeSymbol structType, SyntaxNodeAnalysisContext context )
		{
			if ( !IsReadonlyStruct() && !IsRefStruct() && AllFieldsAreReadonly() && AllPropertiesAreMutable() )
				context.ReportDiagnostic( Diagnostic.Create( _rule, structDeclaration.Identifier.GetLocation(), structType.Name ) );

			bool IsReadonlyStruct() => structDeclaration.Modifiers.Any( SyntaxKind.ReadOnlyKeyword );
			bool IsRefStruct() => structDeclaration.Modifiers.Any( SyntaxKind.RefKeyword );

			bool AllFieldsAreReadonly()
				=> structDeclaration
				.EnumerateDataFields()
				.All( f => f.Modifiers.Any( SyntaxKind.ReadOnlyKeyword ) );

			bool AllPropertiesAreMutable()
				=> structDeclaration
				.EnumerateDataProperties()
				.All( p => p.AccessorList?.Accessors.Any( accessor => accessor.Keyword.IsKind( SyntaxKind.SetKeyword ) ) == false );
		}
	}
}
