using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace HellBrick.Diagnostics.EnforceLambda
{
	[DiagnosticAnalyzer( LanguageNames.CSharp )]
	public class EnforceLambdaAnalyzer : DiagnosticAnalyzer
	{
		public const string DiagnosticId = IDPrefix.Value + "EnforceLambda";

		private static readonly DiagnosticDescriptor _rule = new DiagnosticDescriptor
		(
			DiagnosticId,
			"Prefer lambda over method group",
			"Use a lambda expression instead of a method group",
			DiagnosticCategory.Design,
			DiagnosticSeverity.Warning,
			isEnabledByDefault: true
		);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create( _rule );

		public override void Initialize( AnalysisContext context )
		{
			context.EnableConcurrentExecution();
			context.ConfigureGeneratedCodeAnalysis( GeneratedCodeAnalysisFlags.None );
			context.RegisterOperationAction( c => AnalyzeMethodReference( c ), OperationKind.MethodReference );
		}

		private static void AnalyzeMethodReference( OperationAnalysisContext context )
		{
			IMethodReferenceOperation methodReference = (IMethodReferenceOperation) context.Operation;

			bool shouldBeLambda
				= methodReference.Parent is IDelegateCreationOperation delegateCreation
				&& !IsInstanceQualified( methodReference )
				&& !IsPassedToDelegateSubscription( delegateCreation )
				&& !IsPassedToEventSubscription( delegateCreation );

			if ( shouldBeLambda )
				context.ReportDiagnostic( Diagnostic.Create( _rule, methodReference.Syntax.GetLocation() ) );
		}

		private static bool IsInstanceQualified( IMethodReferenceOperation methodReference )
			=> methodReference.Instance != null
			&& !( methodReference.Instance is IInstanceReferenceOperation );

		private static bool IsPassedToDelegateSubscription( IDelegateCreationOperation delegateCreation )
			=> delegateCreation.Parent is ICompoundAssignmentOperation compoundAssignment
			&& ( compoundAssignment.OperatorKind == BinaryOperatorKind.Add || compoundAssignment.OperatorKind == BinaryOperatorKind.Subtract );

		private static bool IsPassedToEventSubscription( IDelegateCreationOperation delegateCreation )
			=> delegateCreation.Parent is IEventAssignmentOperation;
	}
}
