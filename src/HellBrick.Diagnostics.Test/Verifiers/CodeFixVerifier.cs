using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Xunit;

namespace TestHelper
{
	/// <summary>
	/// Superclass of all Unit tests made for diagnostics with codefixes.
	/// Contains methods used to verify correctness of codefixes
	/// </summary>
	public abstract partial class CodeFixVerifier : DiagnosticVerifier
	{
		/// <summary>
		/// Returns the codefix being tested (C#) - to be implemented in non-abstract class
		/// </summary>
		/// <returns>The CodeFixProvider to be used for CSharp code</returns>
		protected virtual CodeFixProvider GetCSharpCodeFixProvider() => null;

		protected void VerifyNoFix( params string[] sources ) => VerifyCSharpFix( sources, sources );

		protected void VerifyCSharpFix( string[] oldSources, string[] newSources, int? codeFixIndex = null )
			=> VerifyFix( GetCSharpDiagnosticAnalyzer(), GetCSharpCodeFixProvider(), oldSources, newSources, codeFixIndex );

		/// <summary>
		/// General verifier for codefixes.
		/// Creates a Document from the source string, then gets diagnostics on it and applies the relevant codefixes.
		/// Then gets the string after the codefix is applied and compares it with the expected result.
		/// </summary>
		/// <param name="analyzer">The analyzer to be applied to the source code</param>
		/// <param name="codeFixProvider">The codefix to be applied to the code wherever the relevant Diagnostic is found</param>
		/// <param name="oldSource">A class in the form of a string before the CodeFix was applied to it</param>
		/// <param name="newSource">A class in the form of a string after the CodeFix was applied to it</param>
		/// <param name="codeFixIndex">Index determining which codefix to apply if there are multiple</param>
		private void VerifyFix( DiagnosticAnalyzer analyzer, CodeFixProvider codeFixProvider, string[] oldSources, string[] newSources, int? codeFixIndex )
		{
			Project project = CreateProject( oldSources );
			Document[] documents = project.Documents.ToArray();
			Diagnostic[] analyzerDiagnostics = GetAnalyzerDiagnosticsTargetedByCodeFixProvider( documents );
			for ( int documentIndex = 0; documentIndex < documents.Length; documentIndex++ )
			{
				Document document = documents[ documentIndex ];
				string newSource = newSources[ documentIndex ];

				IEnumerable<Diagnostic> compilerDiagnostics = GetCompilerDiagnostics( document );
				int attempts = analyzerDiagnostics.Length;

				for ( int i = 0; i < attempts; ++i )
				{
					List<CodeAction> actions = new List<CodeAction>();
					TextSpan span = analyzerDiagnostics[ 0 ].Location.SourceSpan;
					ImmutableArray<Diagnostic> spanDiagnostics = ImmutableArray.Create( analyzerDiagnostics.Where( d => d.Location.SourceSpan == span ).ToArray() );
					CodeFixContext context = new CodeFixContext( document, span, spanDiagnostics, ( a, d ) => actions.Add( a ), CancellationToken.None );
					codeFixProvider.RegisterCodeFixesAsync( context ).Wait();

					if ( !actions.Any() )
					{
						break;
					}

					if ( codeFixIndex != null )
					{
						document = ApplyFix( document, actions.ElementAt( (int) codeFixIndex ) );
						break;
					}

					document = ApplyFix( document, actions.ElementAt( 0 ) );
					analyzerDiagnostics = GetAnalyzerDiagnosticsTargetedByCodeFixProvider( document );

					IEnumerable<Diagnostic> newCompilerDiagnostics = GetNewDiagnostics( compilerDiagnostics, GetCompilerDiagnostics( document ) );

					//check if applying the code fix introduced any new compiler diagnostics
					if ( newCompilerDiagnostics.Any() )
					{
						// Format and get the compiler diagnostics again so that the locations make sense in the output
						document = document.WithSyntaxRoot( Formatter.Format( document.GetSyntaxRootAsync().Result, Formatter.Annotation, document.Project.Solution.Workspace ) );
						newCompilerDiagnostics = GetNewDiagnostics( compilerDiagnostics, GetCompilerDiagnostics( document ) );

						Assert.True( false,
							System.String.Format( "Fix introduced new compiler diagnostics:\r\n{0}\r\n\r\nNew document:\r\n{1}\r\n",
								System.String.Join( "\r\n", newCompilerDiagnostics.Select( d => d.ToString() ) ),
								document.GetSyntaxRootAsync().Result.ToFullString() ) );
					}

					//check if there are analyzer diagnostics left after the code fix
					if ( !analyzerDiagnostics.Any() )
					{
						break;
					}
				}

				//after applying all of the code fixes, compare the resulting string to the inputted one
				string actual = GetStringFromDocument( document );
				Assert.Equal( newSource, actual );
			}

			Diagnostic[] GetAnalyzerDiagnosticsTargetedByCodeFixProvider( params Document[] documentsToAnalyze )
				=> GetSortedDiagnosticsFromDocuments( analyzer, documentsToAnalyze )
				.Where( d => codeFixProvider.FixableDiagnosticIds.Contains( d.Id ) )
				.ToArray();
		}
	}
}
