﻿using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace HellBrick.Diagnostics.StructDeclarations
{
	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	public class ReadOnlyStructFieldsAnalyzer : DiagnosticAnalyzer
	{
		public const string DiagnosticID = StructIDPrefix.Value + "ReadOnlyFields";
		private const string _title = "Struct fields should be readonly";
		private const string _messageformat = "{0} is a struct, so {0}.{1} should be read-only";

		private static readonly DiagnosticDescriptor _rule = new DiagnosticDescriptor( DiagnosticID, _title, _messageformat, DiagnosticCategory.Design, DiagnosticSeverity.Warning, true );
		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create( _rule );

		private static readonly SyntaxKind[] _kinds = new SyntaxKind[] { SyntaxKind.StructDeclaration };

		public override void Initialize( AnalysisContext context )
		{
			context.RegisterSyntaxNodeAction( FindMutableStructFields, _kinds );
		}

		private void FindMutableStructFields( SyntaxNodeAnalysisContext context )
		{
			StructDeclarationSyntax structDeclaration = context.Node as StructDeclarationSyntax;
			FieldDeclarationSyntax[] mutableFields = structDeclaration
				.ChildNodes()
				.Where( node => node.IsKind( SyntaxKind.FieldDeclaration ) )
				.Cast<FieldDeclarationSyntax>()
				.Where( field => !field.Modifiers.Any( SyntaxKind.ReadOnlyKeyword ) )
				.ToArray();

			foreach ( FieldDeclarationSyntax mutableField in mutableFields )
			{
				string fieldName = mutableField.Declaration.Variables.First().Identifier.ValueText;
				Diagnostic diagnostic = Diagnostic.Create( _rule, mutableField.GetLocation(), structDeclaration.Identifier.ValueText, fieldName );
				context.ReportDiagnostic( diagnostic );
			}
		}
	}
}