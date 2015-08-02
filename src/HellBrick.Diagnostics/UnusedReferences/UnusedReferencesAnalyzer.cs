using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HellBrick.Diagnostics.Utils;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace HellBrick.Diagnostics.UnusedReferences
{
	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	public class UnusedReferencesAnalyzer : DiagnosticAnalyzer
	{
		private const string _diagnosticID = IDPrefix.Value + "UnusedReferences";
		private static readonly DiagnosticDescriptor _rule = new DiagnosticDescriptor( _diagnosticID, "Unused reference", "{0} reference is not used", "Architecture", DiagnosticSeverity.Info, true );
		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create( _rule );

		public override void Initialize( AnalysisContext context )
		{
			context.RegisterCompilationAction( FindUnusedReferences );
		}

		private void FindUnusedReferences( CompilationAnalysisContext context )
		{
			using ( TimeMeasure.ToDebug( $"Finding the references not used by {context.Compilation.AssemblyName}" ) )
			{
				Debug.WriteLine( $"----------------------------------------" );
				Debug.WriteLine( $"Analyzing {context.Compilation.AssemblyName}..." );

				HashSet<AssemblyIdentity> compilationReferences = context.Compilation.ExternalReferences
					.OfType<CompilationReference>()
					.Select( r => r.Compilation.Assembly.Identity )
					.ToHashSet();

				using ( var syntaxTreeEnumerator = context.Compilation.SyntaxTrees.GetEnumerator() )
				{
					while ( compilationReferences.Count > 0 && syntaxTreeEnumerator.MoveNext() && !context.CancellationToken.IsCancellationRequested )
					{
						var syntaxTree = syntaxTreeEnumerator.Current;
						var semanticModel = context.Compilation.GetSemanticModel( syntaxTree );
						ReferenceDiscarder referenceFinder = new ReferenceDiscarder( compilationReferences, syntaxTree, semanticModel, context.CancellationToken );
						referenceFinder.DiscardUsedReferencesAsync().GetAwaiter().GetResult();
					}
				}

				//	I've no idea when this happens, but if it does, it means we haven't discarded all the used references.
				//	Thus the results can't be trusted and we shouldn't report any diagnostics.
				if ( context.CancellationToken.IsCancellationRequested )
					return;

				Debug.WriteLine( $"{compilationReferences.Count} unused references" );

				foreach ( var assembly in compilationReferences )
					context.ReportDiagnostic( Diagnostic.Create( _rule, null, assembly.Name ) );
			}
		}

		private class ReferenceDiscarder : CSharpSyntaxWalker
		{
			private readonly HashSet<AssemblyIdentity> _references;
			private readonly SemanticModel _semanticModel;
			private readonly SyntaxTree _syntaxTree;
			private readonly CancellationToken _cancellationToken;

			public ReferenceDiscarder( HashSet<AssemblyIdentity> references, SyntaxTree syntaxTree, SemanticModel semanticModel, CancellationToken cancellationToken )
			{
				_references = references;
				_syntaxTree = syntaxTree;
				_semanticModel = semanticModel;
				_cancellationToken = cancellationToken;
			}

			public async Task DiscardUsedReferencesAsync()
			{
				var root = await _syntaxTree.GetRootAsync( _cancellationToken ).ConfigureAwait( false );
				Visit( root );
			}

			public override void Visit( SyntaxNode node )
			{
				if ( _references.Count == 0 || _cancellationToken.IsCancellationRequested )
					return;

				base.Visit( node );
			}

			public override void DefaultVisit( SyntaxNode node )
			{
				var symbol = _semanticModel.GetSymbolInfo( node ).Symbol;
				TryDiscard( symbol );

				var returnTypeSymbol = ( symbol as IPropertySymbol )?.Type ?? ( symbol as IMethodSymbol )?.ReturnType;
				TryDiscard( returnTypeSymbol );

				base.DefaultVisit( node );
			}

			private void TryDiscard( ISymbol symbol )
			{
				var symbolAssembly = symbol?.ContainingAssembly?.Identity;
				if ( symbolAssembly != null )
				{
					bool removed = _references.Remove( symbolAssembly );
					if ( removed )
						Debug.WriteLine( $"Removed {symbolAssembly} because of {symbol}" );
				}
			}

			public override void VisitUsingDirective( UsingDirectiveSyntax node )
			{
			}
		}
	}
}