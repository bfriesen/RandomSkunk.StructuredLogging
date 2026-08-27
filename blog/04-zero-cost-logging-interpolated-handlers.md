# Zero-Cost Logging: How Interpolated String Handlers Skip Disabled Levels

Back in [post 1](01-introducing-randomskunk-structuredlogging.md) we mentioned, almost in passing,
that you don't need to guard calls like this:

```csharp
logger.Debug($"Cache miss for {key}, rebuilding from {ExpensiveLookup(key)}");
```

with `if (logger.IsEnabled(LogLevel.Debug))`. If Debug is disabled, `ExpensiveLookup(key)` is never
called — not "called and its result discarded," genuinely never invoked. That's a strong guarantee,
and it's worth understanding exactly how it holds, because it's not magic — it's a specific C#
language feature the library leans on deliberately.

## The problem with a plain `string` parameter

If `Debug`'s message parameter were a plain `string`, this line:

```csharp
logger.Debug($"Cache miss for {key}, rebuilding from {ExpensiveLookup(key)}");
```

would need to fully build that string *before* the call even happens — the compiler has to produce
a `string` value to pass as the argument, which means evaluating every interpolation hole,
including `ExpensiveLookup(key)`, regardless of whether Debug logging is enabled. This is exactly
the trap the old `if (logger.IsEnabled(...))` guard exists to work around, and it's easy to forget
at any of the hundreds of call sites in a real codebase.

## `[InterpolatedStringHandler]`: intercepting construction itself

C# 10 added interpolated string handlers: a type attributed with
`[InterpolatedStringHandler]` that the compiler can construct *instead of* a `string`, when an
interpolated string literal (`$"..."`) is passed to a parameter of that handler type. Instead of
building the whole string up front, the compiler:

1. Constructs the handler, passing it the interpolated string's *literal length* and *hole count*
   (not the evaluated values yet).
2. Calls `AppendLiteral`/`AppendFormatted` on the handler once per literal chunk and interpolation
   hole, in source order, evaluating each hole's expression only immediately before the matching
   `AppendFormatted` call.

Critically, the handler's own code runs *between* those steps — including its constructor, which
runs before any `AppendFormatted` call. That gives the handler a chance to decide, before a single
hole is evaluated, whether it wants the rest of the holes evaluated at all.

Each of RandomSkunk.StructuredLogging's level methods — `Trace`, `Debug`, `Information`, `Warning`,
`Error`, `Critical`, and the level-as-argument `Write` — has its own handler struct (e.g.
`DebugInterpolatedStringHandler`). Its constructor calls `logger.IsEnabled(level)` once and uses the
result twice: reported back to the compiler through an `out bool handlerIsValid` parameter, read by
the *compiler-generated call-site code* (not by `AppendFormatted`) to decide whether to evaluate the
interpolation holes at all; and separately stored on the handler itself, as an `IsEnabled` property
the method body reads afterward instead of calling `logger.IsEnabled` a second time.

That's the piece that actually skips the holes: the compiler generates code that evaluates each
hole's expression immediately before its own `AppendFormatted` call, one hole at a time, guarded by
a check of `handlerIsValid` before each one — not all up front, and not by `AppendFormatted` itself.
There's no way to skip evaluating `ExpensiveLookup(key)` from *inside* `AppendFormatted` — by the
time `AppendFormatted` would run, the value would already have to be computed. The actual skip works
one level higher, at the call site: **the compiler only reaches a given hole's evaluation, and the
matching `AppendFormatted` call, once `handlerIsValid` says the whole append pipeline is worth
running.** If it's `false`, the compiler-generated code skips straight past every hole and every
`AppendFormatted` call, all the way to invoking the method itself with the (still "under
construction," untouched) handler.

## `logger.IsEnabled(level)` runs first — the rest follows for free

The struct constructor runs first, before any interpolation hole is touched. So for a call like:

```csharp
logger.Debug($"Cache miss for {key}, rebuilding from {ExpensiveLookup(key)}");
```

the sequence is:

1. `DebugInterpolatedStringHandler`'s constructor runs, checks `logger.IsEnabled(LogLevel.Debug)`.
2. If disabled, `handlerIsValid = false` and the constructor returns immediately — no work done.
3. The compiler's generated code for each hole checks `handlerIsValid` before evaluating that
   hole's expression. Because it's already `false`, `key` and `ExpensiveLookup(key)` are never
   evaluated, and neither `AppendFormatted` call ever runs.
4. `Debug`'s method body checks the handler's own `IsEnabled` property — set from the exact same
   `logger.IsEnabled(LogLevel.Debug)` result the constructor already computed in step 1 — and
   returns without touching `ILogger.Log` if that's `false`. It doesn't call `logger.IsEnabled`
   again itself; the handler already did that work, so the method body just reads the answer back
   off it instead of asking `logger` a second time.

If Debug is enabled, the same sequence runs, but every step actually does its work: the handler
builds the message text (delegating to a wrapped `DefaultInterpolatedStringHandler` — more on that
below), captures any `<PropertyName>` tags along the way, and the level method hands the result off
to `ILogger.Log`.

## Why the handler wraps `DefaultInterpolatedStringHandler` instead of reinventing it

Each handler struct doesn't build the message text itself character by character. It wraps a
`System.Runtime.CompilerServices.DefaultInterpolatedStringHandler` — the same handler type the
runtime uses for ordinary `$"..."` string construction — and delegates `AppendLiteral`/
`AppendFormatted` straight through to it (after first checking for and consuming a `<PropertyName>`
tag, if any). `DefaultInterpolatedStringHandler` already implements pooled-buffer building,
`IFormattable`/`ISpanFormattable` dispatch, and alignment padding correctly; there's no reason to
hand-roll any of that.

One consequence worth knowing: because `DefaultInterpolatedStringHandler` is itself a `ref struct`,
every wrapper handler in this library has to be a `ref struct` too. And it's specifically *not* a
`readonly struct` — a `readonly struct` defensively copies its mutable-struct fields on every
method call, which would make each `Append*` call silently mutate a throwaway copy of the inner
handler instead of the real one. The message parameter is also declared `ref` (e.g.
`ref DebugInterpolatedStringHandler message`), so the ref struct isn't copied into the callee — the
compiler constructs it as a local at the call site and passes it by reference, with no `ref` needed
at the call site itself; that's standard behavior for interpolated-string-handler parameters
declared `ref`/`in`/`out`.

## What a plain `string` argument gets instead

None of this applies — or needs to — when you pass a plain `string`:

```csharp
string message = ComputeMessage();
logger.Debug(message);
```

There's no interpolation happening at the call site, so there's no handler to construct and no
holes to conditionally evaluate. Every level method has a plain `string message` overload sitting
alongside its handler overload for exactly this case: an explicit `IsEnabled` check happens inside
the method body, then the string (already fully built) is logged or discarded. A precomputed or
hardcoded message is never penalized by any of the handler machinery — it just doesn't need it.

This is also why `$"literal"` and `someStringVariable` behave differently even though both are
strings: per the C# specification's interpolated-string betterness rule, an interpolated string
*literal* still prefers the handler overload over the `string` overload whenever both are
applicable. So `logger.Debug($"literal")` gets the handler's tag-capturing, `IsEnabled`-gated
behavior, while `logger.Debug(someStringVariable)` binds the plain `string` overload — there's no
implicit conversion from `string` to the handler type to blur that line.

## Compare: `Microsoft.Extensions.Logging`'s formatter cache

It's worth contrasting this with how `Microsoft.Extensions.Logging`'s own `LogInformation`/etc.
extension methods avoid repeatedly re-parsing a message template: they cache a compiled formatter
function per distinct template *string*, keyed by that string's contents. That works well when
templates are compile-time constants (the common, recommended case) — but it means a template built
by concatenation or interpolation at the call site produces a new cache entry every time it's
called with different literal content, since the cache has no way to know the "shape" was the same
even if the actual property values differ.

RandomSkunk.StructuredLogging sidesteps this differently, and more fundamentally: the non-generic
and each generic-arity `LogPropertiesState<T1..T6>` type gets its own `static readonly Func<...>`
formatter field, one per closed generic instantiation. The CLR caches each of those for free simply
by virtue of how generic instantiation works — there's no runtime cache to grow unboundedly in the
first place, because the "key" is a compile-time type, not a runtime string.

## Next up

Post 5 covers the analyzers package: how it flags existing `Microsoft.Extensions.Logging` calls in
your codebase and offers a one-click migration to `RandomSkunk.StructuredLogging`.
