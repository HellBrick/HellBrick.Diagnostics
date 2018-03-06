using Microsoft.CodeAnalysis.Diagnostics;

namespace TestHelper
{
	/// <summary>
	/// Superclass of all Unit Tests for DiagnosticAnalyzers
	/// </summary>
	public abstract partial class DiagnosticVerifier
	{
		/// <summary>
		/// Get the CSharp analyzer being tested - to be implemented in non-abstract class
		/// </summary>
		protected virtual DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => null;
	}
}
