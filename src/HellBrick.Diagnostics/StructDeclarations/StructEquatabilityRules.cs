using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HellBrick.Diagnostics.StructDeclarations.EquatabilityRules;
using Microsoft.CodeAnalysis;

namespace HellBrick.Diagnostics.StructDeclarations
{
	internal static class StructEquatabilityRules
	{
		public static ImmutableDictionary<string, IEquatabilityRule> Rules { get; } =
			new IEquatabilityRule[]
			{
				new ImplementEquatableRule(),
				new OverrideEqualsRule(),
				new ImplementEqualsOperatorRule(),
				new ImplementNotEqualsOperatorRule()
			}
			.ToImmutableDictionary( rule => rule.ID );

		public static ImmutableDictionary<string, DiagnosticDescriptor> Descriptors { get; } =
			Rules.Values
				.Select( rule => new DiagnosticDescriptor( rule.ID, "Structs " + rule.RuleText, "{0} " + rule.RuleText, DiagnosticCategory.Design, DiagnosticSeverity.Warning, true ) )
				.ToImmutableDictionary( descriptor => descriptor.Id );
	}
}
