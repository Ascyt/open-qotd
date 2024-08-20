# General Usage

## Commands

Commands can either be written using Discord slash commands or by sending a message starting with the set prefix (see [config.md#prefix](./Commands/config.md#prefix))

Commands have arguments. The arguments are written in square brackets (`[]`) or angled brackets (`<>`), are in [camelCase](https://en.wikipedia.org/wiki/Camel_case) and do not have spaces.  

Square brackets (`[]`) mean that an argument is required, while angled brackets (`<>`) mean that an argument is not required and can be omitted. For non-required arguments, a default value can be provided using `=` after the type, such as in `<myArg (int)=10>` where, if you omit the argument, the value 10 will be used.

Data types are put after the argument name, such as `[myArg (string)]`, and can be mixed using the `|` character, which means that either of the listed data types are supported, such as in `[myArg (string|user[]|int:1~10)]` where a string, a user or an int going from 1 to 10 is supported.

### Data types

#### Basic data types

- `boolean`: Can be either `True` or `False`.
- 
- `string`: Plain text. For non-slash commands, this must be put in quotes, such as `"Hello world"` instead of just `Hello world`. For slash commands, the quotes are argumental. You can use `\n` to write new-lines, `\"` to write quotes, and `\\` to write a backslash. 
-
- `int`: An integer going from `-2147483648` to `2147483647`.
- `int:A`: An integer going from (inclusive) `A` to `2147483647`, such as `int:0` for only positive numbers.
- `int:A~B`: An integer with (inclusive) bounds from `A` to `B`, such as `int:1~10`
- 
- `float`: A floating point number or integer, going from `-1.7976931348623157E+308` to `1.7976931348623157E+308`.
- `float:A`: A floating point number or integer, going from (inclusive) `A` to `1.7976931348623157E+308`, such as `float.0` for only positive numbers.
- `float:A!`: A floating point number or integer, going from (exclusive) `A` to (inclusive) `1.7976931348623157E+308`, such as `float:0!` for only positive numbers excluding `0`.
- `float:A~B`: A floating point number or integer with inclusive bounds. 
- `float:A!~B!`: A floating point number or integer with exclusive bounds. `!` can be put for `A` or `B` regardless whether or not the other value has it.

#### Discord-specific data types

- `user`: A username (not display name!), ID or ping, such as `wumpus`, `@wumpus`, `123456789123456789`, or `<@123456789123456789>` (which is what Discord does by default if you type @ and the user).
- `channel`: A channel ID, link or reference, such as `1234567890123456789`, `https://discord.com/channels/9876543210987654321/1234567890123456789`, or `<#1234567890123456789>` (which is what Discord does by default if you type # and the channel name).
- `role`: A role ID, name or reference, such as `1234567890123456789`, `my role` (name must be exact, but it's not case-sensitive), or `<@&1234567890123456789>` (which is what Discord does by default if you type @ and the role).
- `message`: A message ID or link, such as `123123123123123123` or `https://discord.com/channels/9876543210987654321/1234567890123456789/123123123123123123`.

#### Lists

Lists are written by putting two square brackets (`[]`) after a data type, such as `user[]`. They must contain at least one element, unless the argument itself is not required (`[myArg (int[])]` requires at least one element while `<myArg (int[])>` does not)

For mixed data types, it's important to know the difference bettween having the `[]` be inside or outside the type (eg. `[myArg (user[]|string[])]` is either a string list or user list (but not mixed), while `[myArg (user|string)[]]`) is a list of users or strings in any order.

All lists are seperated by spaces, and for non-slash commands lists musts be written in square brackets, such as `1 2 3` for slash commands and `[1 2 3]` for non-slash commands. Commas (`,`) can argumentally be used for separation in place of or in addition to spaces. 

Strings must always be quoted, even if it's just a single element, and the spaces can be omitted in certain cases (eg. when writing multiple users by typing @ and writing the names).

#### Options

It is possible for an argument to have multiple options instead of a data type. The options have to be separated using `|`, have to be put in single quotes (`'`) and can contain spaces, such as `[myArg ('Option A'|'Option B'|'Option C')]`. 

Options can be mixed with data types, such as `[myArg (int|'Option A')]`, in which case they have to be typed manually. 

Options in Lists are also valid, such as `[myArg ('Option A'|'Option B'|'Option C')[]]`. 

Slash-commands generally have these options built-in as a dropdown. For normal commands and for cases when the option has to be typed manually, the single quotes (`'`) must be written, such as `'Option A'`. 
