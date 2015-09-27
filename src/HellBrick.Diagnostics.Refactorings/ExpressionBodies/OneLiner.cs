using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace HellBrick.Diagnostics.ExpressionBodies
{
	public struct OneLiner
	{
		public OneLiner( MethodDeclarationSyntax declaration, ReturnStatementSyntax returnStatement )
		{
			Declaration = declaration;
			ReturnStatement = returnStatement;
		}

		public MethodDeclarationSyntax Declaration { get; }
		public ReturnStatementSyntax ReturnStatement { get; }
	}
}
