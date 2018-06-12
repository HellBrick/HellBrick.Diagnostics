using System;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

using static System.Linq.Expressions.Expression;

namespace HellBrick.Diagnostics.Utils
{
	public static class TypeSymbolExtensions
	{
		private static readonly Func<ITypeSymbol, bool> _readOnlyStructCheck = GenerateReadOnlyStructCheck();

		private static Func<ITypeSymbol, bool> GenerateReadOnlyStructCheck()
		{
			ParameterExpression typeSymbol = Parameter( typeof( ITypeSymbol ), "typeSymbol" );

			Type typeSymbolType = typeof( SyntaxKind ).GetTypeInfo().Assembly.GetType( "Microsoft.CodeAnalysis.CSharp.Symbols.TypeSymbol" );
			PropertyInfo isReadOnlyProperty = typeSymbolType.GetTypeInfo().GetDeclaredProperty( "IsReadOnly" );

			Expression body
				= AndAlso
				(
					TypeIs( typeSymbol, typeSymbolType ),
					Property
					(
						Convert( typeSymbol, typeSymbolType ),
						isReadOnlyProperty
					)
				);

			Expression<Func<ITypeSymbol, bool>> lambda = Lambda<Func<ITypeSymbol, bool>>( body, parameters: typeSymbol.AsArray() );
			return lambda.Compile();
		}

		public static bool IsReadOnlyStruct( this ITypeSymbol typeSymbol ) => _readOnlyStructCheck( typeSymbol );
	}
}
