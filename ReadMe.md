# Nuget

[`install-package HellBrick.Diagnostics`](https://www.nuget.org/packages/HellBrick.Diagnostics)

# What is this?

Just a small opinionated set of diagnostics and code fixes to automate routine checks and producing boilerplate code.
Both diagnostics and code fixes assume the latest version of C#.

# Diagnostics

### `HBConfigureAwait`

A diagnostic that's reported when an instance of type that has a `ConfigureAwait( bool )` method is awaited.

There are two corresponding code fixes for inserting `ConfigureAwait( false )` or `ConfigureAwait( true )` call into the awaited expression.

![ConfigureAwait() code fix screenshot](https://i.imgur.com/1axBhX7.png)

### `HBStructImmutableNonReadonly`

A diagnostic that's reported when struct fields are never mutated, but the struct doesn't have a `readonly` modifier.

A corresponding code fix marks the struct as `readonly`.

![Readonly struct code fix screenshot](https://i.imgur.com/zpb3TSd.png)

### `HBStructEquatabilityMethodsMissing`

A diagnostic that's reported when readonly struct doesn't provide all required equatability traits (implementing `IEquatable`, overriding `Equals()` and `GetHashCode()`, providing `==` and `!=` operators).

A corresponding code fix generates the missing methods.

![Struct equatability code fix screenshot](https://i.imgur.com/RG3FItb.png)

### `HBUnusedParameter`

A diagnostic that's reported when a method contains a parameter that isn't used in the method body, unless the method is one of the obvious exception cases (is part of interface implementation, is a program entry point, etc.).

A corresponding code fix removes the parameter from the method definition and the corresponding argument from all its call sites.

![Unused parameter code fix screenshot](https://i.imgur.com/UqggZuf.png)

### `HBCommentedCode`

A diagnostic that's reported when a commented out code is detected.

A corresponding code fix removes the comment block that contains the code.

![Commented code fix screenshot](https://i.imgur.com/u0hrpcE.png)

### `HBMethodShouldBeStatic`

A diagnostic that's reported when a private non-static method doesn't reference any instance members.

A corresponding code fix makes the method static.

![Method should be static code fix screenshot](https://i.imgur.com/uC8FFd2.png)

### `HBEnforceLambda`

A diagnostic that's reported when a delegate is created from a method group instead of a lambda expression.

A corresponding code fix converts method group to a lambda expression.

![Convert to lambda code fix screenshot](https://i.imgur.com/VrNgM3v.png)

### `ValueTypeNullComparison`

A diagnostic that's reported when an instance of value type is compared for (in)equality with `null`.

A corresponding code fix replaces `null` with `default`.

![Value type == null code fix screenshot](https://i.imgur.com/8F1IyD7.png)
