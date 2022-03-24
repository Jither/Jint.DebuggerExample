(Relatively) Simple Jint Debugger Example
=========================================
A relatively basic console application demonstrating many of the concepts needed to implement an interactive debugger for [Jint](https://github.com/sebastienros/jint). Currently builds against the latest `main` branch and .NET 6.0.

* Stepping (into/over/out)
* Setting, deleting, and clearing break points
* Example of extending the `BreakPoint` class to add support for temporary break points
* Displaying scopes and their bindings (variables and properties)
* Displaying call stack
* Evaluating expressions

Usage
-----
Simply call the executable with a path to a script file. The script will be paused before execution of the first line. Type `help` for a list of debugger commands. The listing shows full command name, short command name (if any), arguments (if any - angular brackets = required, square brackets = optional), and a description of the commmand.
