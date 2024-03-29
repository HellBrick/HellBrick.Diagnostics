﻿# Nuget

[`install-package HellBrick.Diagnostics`](https://www.nuget.org/packages/HellBrick.Diagnostics)

# What is this?

Just a small opinionated set of diagnostics and code fixes to automate routine checks and producing boilerplate code.
Both diagnostics and code fixes assume the latest version of C#.

# Diagnostics

### `HBConfigureAwait`

A diagnostic that's reported when an instance of type that has a `ConfigureAwait( bool )` method is awaited.

There are two corresponding code fixes for inserting `ConfigureAwait( false )` or `ConfigureAwait( true )` call into the awaited expression.

![ConfigureAwait() code fix screenshot](https://i.imgur.com/1axBhX7.png)

### `HBUnusedParameter`

A diagnostic that's reported when a method contains a parameter that isn't used in the method body, unless the method is one of the obvious exception cases (is part of interface implementation, is a program entry point, etc.).

A corresponding code fix removes the parameter from the method definition and the corresponding argument from all its call sites.

![Unused parameter code fix screenshot](https://i.imgur.com/UqggZuf.png)

### `HBCommentedCode`

A diagnostic that's reported when a commented out code is detected.

A corresponding code fix removes the comment block that contains the code.

![Commented code fix screenshot](https://i.imgur.com/u0hrpcE.png)
