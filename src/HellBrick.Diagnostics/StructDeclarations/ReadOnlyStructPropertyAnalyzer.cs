using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using HellBrick.Diagnostics.Utils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace HellBrick.Diagnostics.StructDeclarations
{
	public class ReadOnlyStructPropertyAnalyzer : IStructSyntaxNodeAnalyzer
	{
		public const string DiagnosticID = StructIDPrefix.Value + "ReadOnlyProperty";
		private const string _title = "Struct properties should be readonly";
		private const string _messageFormat = "{0} is a struct, so {0}.{1} should be read-only";

		private static readonly DiagnosticDescriptor _rule = new DiagnosticDescriptor( DiagnosticID, _title, _messageFormat, DiagnosticCategory.Design, DiagnosticSeverity.Warning, true );
		public ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create( _rule );

		public void AnalyzeStructSyntaxNode( StructDeclarationSyntax structDeclaration, ITypeSymbol structType, SyntaxNodeAnalysisContext context )
		{
			var mutablePropertyQuery =
				from property in structDeclaration.EnumerateDataProperties()
				let setter = property.AccessorList?.Accessors.FirstOrDefault( accessor => accessor.Keyword.IsKind( SyntaxKind.SetKeyword ) )
				where setter != null
				select new { Property = property, Setter = setter };

			foreach ( var mutableProperty in mutablePropertyQuery )
			{
				Diagnostic diagnostic = Diagnostic.Create( _rule, mutableProperty.Setter.GetLocation(), structDeclaration.Identifier.ValueText, mutableProperty.Property.Identifier.ValueText );
				context.ReportDiagnostic( diagnostic );
			}
		}
	}
}