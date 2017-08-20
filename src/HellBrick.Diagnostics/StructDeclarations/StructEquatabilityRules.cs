using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using HellBrick.Diagnostics.StructDeclarations.EquatabilityRules;
using Microsoft.CodeAnalysis;

namespace HellBrick.Diagnostics.StructDeclarations
{
	internal static class StructEquatabilityRules
	{
		private static readonly List<IEquatabilityRule> _rulesSorted =
			new List<IEquatabilityRule>
			{
				new OverrideGetHashCodeRule(),
				new ImplementEquatableRule(),
				new OverrideEqualsRule(),
				new ImplementEqualsOperatorRule(),
				new ImplementNotEqualsOperatorRule()
			};

		public static ImmutableDictionary<string, IEquatabilityRule> Rules { get; } = _rulesSorted.ToImmutableDictionary( rule => rule.ID );
		public static ImmutableDictionary<string, DiagnosticDescriptor> Descriptors { get; } =
			_rulesSorted
				.Select( rule => new DiagnosticDescriptor( rule.ID, "Structs " + rule.RuleText, "{0} " + rule.RuleText, DiagnosticCategory.Design, DiagnosticSeverity.Warning, true ) )
				.ToImmutableDictionary( descriptor => descriptor.Id );

		public static readonly IComparer<IEquatabilityRule> RuleComparer = new RuleOrderComparer();

		private class RuleOrderComparer : IComparer<IEquatabilityRule>
		{
			public int Compare( IEquatabilityRule x, IEquatabilityRule y )
			{
				int xIndex = _rulesSorted.FindIndex( rule => rule.ID == x.ID );
				int yIndex = _rulesSorted.FindIndex( rule => rule.ID == y.ID );
				return xIndex.CompareTo( yIndex );
			}
		}
	}
}
