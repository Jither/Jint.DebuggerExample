# Notes (and early documentation) on Jint's DebugHandler

Jint's DebugHandler provides functionality for inspecting a script while it runs - mainly events triggered at each execution point in the script, as well as information about call stack, scopes etc. It also has built in support for breakpoints.

Note that this is useful for other use cases than interactive debuggers: Any use case where inspection of the engine at each "step" of the script is needed.

## Setup
There are a few DebugHandler-related options that can be specified via the Jint `Engine` constructor's options parameter:

* `DebugMode()` - enables the debug handler.
* `DebuggerStatementHandling()` - specifies how Javascript `debugger` statements are handled. Either by:
  * breaking in the CLR debugger (e.g. Visual Studio) - `DebuggerStatementHandling.Clr`.
  * triggering an event on `DebugHandler`, equivalent to a breakpoint (`DebuggerStatementHandling.Script`) - this would be used for implementing the typical behavior of the statement.
  * being ignored completely, which is the default (`DebuggerStatementHandling.Ignore`).
* `InitialStepMode()` - specifies the `StepMode` to use when the script is first executed. By default, this is `StepMode.None`.

## Stepping
`DebugHandler` maintains a `StepMode`, which can be set initially (through the `InitialStepMode()` option) - and changed through its events.

* `StepMode.None` - means "don't step" - equivalent to "Continue" in debuggers.
* `StepMode.Into` - means "step into functions calls and getters/setters".
* `StepMode.Over` - means "step over function calls and getter/setter invocations".
* `StepMode.Out` - means "step out of the current function/getter/setter".

__How it is now:__ `Into`/`Over`/`Out` will trigger the `Step` event at the next appropriate execution point. `None` will only trigger `Break` if it hits a breakpoint or debugger statement - no other events will be triggered with `StepMode.None`.

__How it should be:__ *Some* event will trigger for *every* step of the script, regardless of `StepMode` - what it changes is the *kind* of event that will trigger.

__Note__ that "steps" does not mean "statements". See below.

## Breakpoints
Breakpoints can be set through `DebugHandler.BreakPoints`. A `BreakPoint` consists of a break location (line/column/source) and an optional condition. Note that - across all of Jint and Esprima.NET - the first line is line 1, and the first column is column 0.

The `BreakPoint` class may be extended to add additional properties needed by the debugger implementation - e.g. for logpoints, hit counts etc.

* `Set` - adds a breakpoint - if a breakpoint already exists at the same break location, it will be replaced.
* `RemoveAt` - removes the breakpoint at a given location. Note that if the `source` of the location given is null, it will match *any* source. __To be considered:__ Does this make sense? Should it be changed?

In addition, `DebugHandler.BreakPoints` can be cleared (`.Clear()`), all breakpoints can be enabled/disabled (`.Active`), and you can check the number of breakpoints (`.Count`) as well as whether a breakpoint exists at a given position (`Contains()`).

Breakpoints can be added at any of the execution points in the code that Jint supports - including at return points from functions, the beginning *or* iterator of a `for` loop, etc.

Breakpoints aren't line based: Multiple execution points, and hence breakpoint locations, may exist in a single line - whether it's multiple statements on the same line, or different points in a loop statement. __For this reason, the break location *must* match a "step-eligible" AST node's position *exactly*.__ (This also allows for efficient internal handling of breakpoints).

__How it should be:__ Jint should provide a method to find valid break locations - and locate the valid break location nearest to a location given by the user. Jint.ExampleDebugger has an example implementation of such a method, but since Jint decides what constitutes a valid break location, it should probably also be Jint's job to make those locations easy to determine for the user.

## Events
The bulk of interfacing with `DebugHandler` happens through its events. For each execution point, `DebugHandler` triggers an event.

__How it is now:__ ... except when `StepMode = None`. There are currently two events: `Step` is triggered at each eligible step when stepping. `Break` is triggered when `StepMode = None` (or while e.g. stepping over or out), and the interpreter reaches a breakpoint or `debugger` statement. Other steps that are reached while `StepMode = None` will *not* trigger an event.

__How it should be:__ Three events - but they could - and should probably - be collapsed into being a single `Step` event, with a `Reason` parameter. 

Let's define two states for the execution: "stepping" and "running":

* "stepping": when `StepMode = Into/Over/Out` and the next step is reached.
* "running" when `StepMode = None` *or* when `StepMode = Over/Out` and the next step hasn't been reached yet.

Then the reason given by the `Step` event would be:

* `Step` - when stepping.
* `BreakPoint` - when reaching an active breakpoint while running.
* `DebuggerStatement` - when reaching a debugger statement while running.
* `Skip` - all other cases - i.e. while running, but we're not at a breakpoint or debugger statement.

Note that `BreakPoint` and `DebuggerStatement` are only triggered while running. Stepping to an execution point that has a breakpoint or debugger statement will only trigger `Step` (because the reason for the event isn't the breakpoint, but simply that we stepped to the execution point).

The only difference between these events is the reason - which may be used by the debugger implementation to make decisions on UI etc. All of the events have access to the same data from the `DebugHandler` - current node, the current breakpoint (even if it wasn't the reason for the event triggering), the call stack, etc.

`Step` still has access to the breakpoint, even when it wasn't the breakpoint that triggered the event. This is useful for e.g. log points (which, depending on implementation, would still need to log even when stepping through the log point location), or breakpoints with hit count (which, again, would still need to increment the hit counter even when stepping through the breakpoint location).

There are a few use cases for the `Skip` event, e.g. responding to the user clicking `Pause` or `Stop` in a debugger while the script is running. When running, only `BreakPoint`/`DebuggerStatement` and `Skip` events will be called. In most other cases, `Skip` events should result in no action from the debugger.

## Information and methods available in event handlers

__How it is now:__ The event includes a parameter of type `DebugInformation`, which contains most of the information. In addition, `DebugHandler` itself has a property, `CurrentLocation`, which holds the location of the current execution point. The reason this is on both the `DebugInformation` object and the `DebugHandler` itself is that e.g. a console object may need access to it in order to output the code location.

__How it should be:__ Considering removing the `DebugInformation` parameter and moving all its properties to `DebugHandler` alongsie `CurrentLocation` - it's not just `CurrentLocation` that's useful outside of the step events. Also, it avoids allocating a `DebugInformation` object for every execution point.

The information includes:

* `PauseType` (which should probably be renamed when adding the `Skip` event type) - this is the `Reason` enum described above.
* `BreakPoint` - breakpoint at the current location. This is set even if the breakpoint wasn't what triggered the event.
* `CurrentNode` - the AST node that will be executed at the next step. Note that this will be `null` when execution is at a return point.
* `Location` - same as the `CurrentLocation` currently on `DebugHandler` - the location (start and end position) in the code. For return points, the start and end are at the end of the function body (before the closing brace if there is one).
* `CallStack` - read only list of all call stack frames (and their scope chains), starting with the current call frame. See below.
* `CurrentCallFrame` - the current call stack frame - in other words, simply a "shortcut" to `CallStack[0]`.
* `CurrentScopeChain` - the current scope chain - in other words, simply a "shortcut" to `CurrentCallFrame.ScopeChain`.
* `ReturnValue` - the return value of the currently executing call frame. Only set at return points - otherwise, it's `null`.
* `CurrentMemoryUsage` - only there for historical reasons. I believe it will still always return 0. __How it should be:__ Consider removing?

The API allows inspection of every scope chain for every call frame on the stack through `CallStack`. For many use cases of simple scope inspection, `CurrentScopeChain` will be enough.

### Call stack frames

Each `CallFrame` includes:

* `FunctionName` - name of the function of this frame. For global scope (and other cases), this will be `"(anonymous)"`.
* `FunctionLocation` - code location of the function (start and end of its definition). For top level (global) call frames, as well as for functions not defined in script, this will be `null`.
* `Location` - currently executing source location in this call frame.
* `ScopeChain` - read only list of the scopes in the scope chain of this call frame. See below.
* `This` - the value of `this` in the call frame.
* `ReturnValue` - the return value of this call frame. Will be `null` if not at a return point - which also means it's `null` for everything that isn't the top of the call stack (since no other frame has reached the return point yet). In other words, if any `CallFrame` has a non-null `ReturnValue`, it will be the top frame, and the same value will be in `ReturnValue` on the main information object.

### Scopes

A scope chain, `DebugScopes`, contains all the scopes of a call frame. Note that each call frame may have a very different scope chain from another - all of them are accessible through `CallStack[n].ScopeChain`, allowing a debugger implementation to make the call stack "browsable", updating the scope chain depending on where in the call stack the user is inspecting.

__How it is now:__ The `DebugScopes`, in addition to the read only list of scopes, also includes two read only properties: `Global` and `Local` for accessing those two scopes in particular.

__How it should be:__ `DebugScopes` shouldn't include any properties for specific scopes. `Global` and `Local`, by now, are rather arbitrary choices of scopes - and some scope types may appear multiple times in the list. Simpler to just use the list directly.

Scopes are organized similarly to Chromium:

* It uses the same scope types - see the source and comments for the `DebugScopeTypes` enum. Only `Eval` and `WasmExpressionStack` are not currently used.
* The __Global__ scope only includes the properties of the global object, variables declared with `var`, and functions (in other words, the global *object environment record*).
* The __Script__ scope includes the top level variables declared with `let`/`const` as well as top level declared classes (in other words, the global *declarative environment record*).
* Scopes with no bindings are not included in the chain. __How it should be:__ Should they be?
* There can only be a single __Local__ scope - in the innermost function. Any outer "local" scopes have the type `Closure`.
* `catch` gets its own __Catch__ scope, which only ever includes the argument to `catch`.
* Modules also get their own `Module` scope, which corresponds to the *module environment record*.

__How it is now:__ Bindings that are shadowed by inner scopes are removed from the scope's list of bindings.

__How it should be:__ Bindings are left as-is. Debuggers allow inspection of shadowed variables in outer scopes. It also makes lazy evaluation of binding names simpler.

Each scope in the chain includes:

* `ScopeType` - the type of scope (see above).
* `IsTopLevel` - boolean indicating whether this is a block scope that is at the top level of its containing function. Chromium combines top level block scopes with the local scope - this allows a debugger to do the same. It's only needed because we cannot infer "top level" from the scope chain order alone - because we're leaving out any scopes that happen to not have any bindings (in other words, if the top level block scope has no bindings, any inner block scope would appear to be top level).
* `BindingNames` - the names of all bindings in the scope.
* `BindingObject` - for *object environment records* (i.e. the Global scope and With scopes), this returns the actual binding object. This is mainly intended as an optimization for a Chromium dev-tools implementation, allowing direct serialization of the object without creating a new transient object.
* `GetBindingValue()` - retrieves the value of a named binding.
* `SetBindingValue()` - sets the value of an *existing* (and mutable) binding.

### Lazy evaluation and semantics

All of the objects and collections within the debug information are lazily instantiated - since many uses won't need them (e.g. most uses of `Skip` events would not need any of the debug information).

__How it is now:__ New `CallFrame`, `DebugScopes` and `DebugScope` objects are created when queried on each step.

__How it should be:__ It would be preferable to only create a single object for each internal call frame, scope chain and scope (environment record) - both for memory use and performance, but also to make it easier for a debugger to determine changes between steps (through reference equality). Haven't decided on the best approach here - any ideas? Considering e.g. unique ID's for frames/scopes (dev-tools wants a unique ID for call frames in any event). Or maybe better(?): `ConditionalWeakTable` mapping environment records and frames to `DebugScope` and `CallFrame`.

### Access to script AST's

In many cases, debuggers will need access to the AST of the scripts. Jint obviously already parses the scripts and creates the ASTs.

__How it is now:__ Recently added a `Loaded` event to the `DefaultModuleLoader`. If implementing your own `IModuleLoader`, it's obviously trivial to add events or direct calls to hand the AST to the debugger. That covers the case of modules.

__How it should be:__ Ideally, whenever Jint does the parsing of the script, it should trigger an event to hand the AST (and possibly other info) to the debugger. In the case of interactive debuggers such an event would also be a good time for the debugger to prepare for debugging and notify any external parties (e.g. Chromium dev-tools) that the script is ready for execution - regardless of whether the debugger could retrieve the AST elsewhere (e.g. by parsing the script itself).