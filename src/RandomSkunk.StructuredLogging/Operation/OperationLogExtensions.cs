using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace RandomSkunk.StructuredLogging.Operation;

/// <summary>
/// Extension methods for recording a value to an operation's (or sub-operation's) journal, or recording a
/// value returned from an operation as that operation's result. Each of <see cref="AppendResultTo{T}(T, IOperationLog)"/>,
/// <see cref="AddPropertyTo{T}(T, IOperationLog, string)"/>, <see cref="AppendValueTo{T}(T, IOperationLog, string?)"/>,
/// and <see cref="AppendJsonTo{T}(T, IOperationLog, string?)"/> has an <see cref="IOperationLog"/> overload
/// and an <see cref="ISubOperationLog"/> overload - the two interfaces are deliberately unrelated (each
/// declares its own copy of the members they share), so a single method typed to one would reject the
/// other. Each method's name mirrors the <see cref="IOperationLog"/>/<see cref="ISubOperationLog"/> member
/// it calls, plus a <c>To</c> suffix - e.g. <see cref="SetResultTo{T}"/> calls
/// <see cref="IOperationLog.SetResult{T}"/>, <see cref="AppendResultTo{T}(T, IOperationLog)"/> calls
/// <see cref="IOperationLog.AppendResult{T}"/>.
/// </summary>
public static class OperationLogExtensions
{
    /// <summary>
    /// Sets <paramref name="result"/> as the <c>Operation.Result</c> of the root operation <paramref name="log"/>
    /// belongs to, then returns <paramref name="result"/> unchanged - so this can be chained directly onto a
    /// return expression, e.g. <c>return OrderResult.Shipped(...).SetResultTo(log);</c>. See
    /// <see cref="IOperationLog.SetResult{T}"/>. Only available on the root <see cref="IOperationLog"/> -
    /// for a sub-operation, use <see cref="AppendResultTo{T}(T, ISubOperationLog)"/> instead.
    /// </summary>
    /// <typeparam name="T">The type of the result.</typeparam>
    /// <param name="result">The result to record.</param>
    /// <param name="log">The root operation the result belongs to.</param>
    /// <returns><paramref name="result"/>, unchanged.</returns>
    [return: NotNullIfNotNull(nameof(result))]
    public static T SetResultTo<T>(this T result, IOperationLog log)
    {
        ArgumentNullException.ThrowIfNull(log);

        log.SetResult(result);
        return result;
    }

    /// <summary>
    /// Appends <paramref name="result"/> to the journal of the root operation <paramref name="log"/>, in
    /// the form <c>Operation result: value</c>, then returns <paramref name="result"/> unchanged - so this
    /// can be chained directly onto a return expression, e.g.
    /// <c>return allocation.RecordAsShipped().AppendResultTo(log);</c>. See
    /// <see cref="IOperationLog.AppendResult{T}"/>. Use <see cref="SetResultTo{T}"/> instead if the result
    /// should also become the final entry's <c>Operation.Result</c> structured property.
    /// </summary>
    /// <typeparam name="T">The type of the result.</typeparam>
    /// <param name="result">The result to record.</param>
    /// <param name="log">The root operation the result belongs to.</param>
    /// <returns><paramref name="result"/>, unchanged.</returns>
    [return: NotNullIfNotNull(nameof(result))]
    public static T AppendResultTo<T>(this T result, IOperationLog log)
    {
        ArgumentNullException.ThrowIfNull(log);

        log.AppendResult(result);
        return result;
    }

    /// <summary>
    /// Appends <paramref name="result"/> to the journal of the sub-operation <paramref name="log"/>, in
    /// the form <c>`name` result: value</c>, then returns <paramref name="result"/> unchanged - so this
    /// can be chained directly onto a return expression, e.g.
    /// <c>return allocation.RecordAsShipped().AppendResultTo(subLog);</c>. See
    /// <see cref="ISubOperationLog.AppendResult{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of the result.</typeparam>
    /// <param name="result">The result to record.</param>
    /// <param name="log">The sub-operation the result belongs to.</param>
    /// <returns><paramref name="result"/>, unchanged.</returns>
    [return: NotNullIfNotNull(nameof(result))]
    public static T AppendResultTo<T>(this T result, ISubOperationLog log)
    {
        ArgumentNullException.ThrowIfNull(log);

        log.AppendResult(result);
        return result;
    }

    /// <summary>
    /// Adds <paramref name="propertyValue"/> as a structured property named <paramref name="propertyName"/>
    /// to the root operation <paramref name="log"/>, then returns <paramref name="propertyValue"/>
    /// unchanged - so this can be chained directly onto an expression, e.g.
    /// <c>var orderId = order.Id.AddPropertyTo(log, "OrderId");</c>. See
    /// <see cref="IOperationLog.AddProperty{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of the property value.</typeparam>
    /// <param name="propertyValue">The property value to record.</param>
    /// <param name="log">The root operation to record the property to.</param>
    /// <param name="propertyName">The name to record the property under.</param>
    /// <returns><paramref name="propertyValue"/>, unchanged.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="log"/> or <paramref name="propertyName"/> is <see langword="null"/>.</exception>
    [return: NotNullIfNotNull(nameof(propertyValue))]
    public static T AddPropertyTo<T>(this T propertyValue, IOperationLog log, string propertyName)
    {
        ArgumentNullException.ThrowIfNull(log);

        // Validated here as well as in AddProperty itself, so the exception names this method's own
        // parameter rather than the interface member's.
        ArgumentNullException.ThrowIfNull(propertyName);

        log.AddProperty(propertyName, propertyValue);
        return propertyValue;
    }

    /// <summary>
    /// Adds <paramref name="propertyValue"/> as a structured property named <paramref name="propertyName"/>
    /// to the sub-operation <paramref name="log"/>, then returns <paramref name="propertyValue"/>
    /// unchanged - so this can be chained directly onto an expression, e.g.
    /// <c>var orderId = order.Id.AddPropertyTo(subLog, "OrderId");</c>. See
    /// <see cref="ISubOperationLog.AddProperty{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of the property value.</typeparam>
    /// <param name="propertyValue">The property value to record.</param>
    /// <param name="log">The sub-operation to record the property to.</param>
    /// <param name="propertyName">The name to record the property under.</param>
    /// <returns><paramref name="propertyValue"/>, unchanged.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="log"/> or <paramref name="propertyName"/> is <see langword="null"/>.</exception>
    [return: NotNullIfNotNull(nameof(propertyValue))]
    public static T AddPropertyTo<T>(this T propertyValue, ISubOperationLog log, string propertyName)
    {
        ArgumentNullException.ThrowIfNull(log);

        // Validated here as well as in AddProperty itself, so the exception names this method's own
        // parameter rather than the interface member's.
        ArgumentNullException.ThrowIfNull(propertyName);

        log.AddProperty(propertyName, propertyValue);
        return propertyValue;
    }

    /// <summary>
    /// Records <paramref name="value"/> to the journal of the root operation <paramref name="log"/>, in the
    /// form <c>`valueName`: value</c>, then returns <paramref name="value"/> unchanged - so this can be
    /// chained directly onto an expression, e.g. <c>var total = order.Total.AppendValueTo(log);</c>. See
    /// <see cref="IOperationLog.AppendValue{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="value">The value to record.</param>
    /// <param name="log">The root operation to record the value to.</param>
    /// <param name="valueName">
    /// The name to label the value with. Defaults to the source text of the <paramref name="value"/>
    /// argument expression, via <see cref="CallerArgumentExpressionAttribute"/>.
    /// </param>
    /// <returns><paramref name="value"/>, unchanged.</returns>
    [return: NotNullIfNotNull(nameof(value))]
    public static T AppendValueTo<T>(this T value, IOperationLog log, [CallerArgumentExpression(nameof(value))] string? valueName = null)
    {
        ArgumentNullException.ThrowIfNull(log);

        log.AppendValue(value, valueName);
        return value;
    }

    /// <summary>
    /// Records <paramref name="value"/> to the journal of the sub-operation <paramref name="log"/>, in the
    /// form <c>`valueName`: value</c>, then returns <paramref name="value"/> unchanged - so this can be
    /// chained directly onto an expression, e.g. <c>var total = order.Total.AppendValueTo(subLog);</c>. See
    /// <see cref="ISubOperationLog.AppendValue{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="value">The value to record.</param>
    /// <param name="log">The sub-operation to record the value to.</param>
    /// <param name="valueName">
    /// The name to label the value with. Defaults to the source text of the <paramref name="value"/>
    /// argument expression, via <see cref="CallerArgumentExpressionAttribute"/>.
    /// </param>
    /// <returns><paramref name="value"/>, unchanged.</returns>
    [return: NotNullIfNotNull(nameof(value))]
    public static T AppendValueTo<T>(this T value, ISubOperationLog log, [CallerArgumentExpression(nameof(value))] string? valueName = null)
    {
        ArgumentNullException.ThrowIfNull(log);

        log.AppendValue(value, valueName);
        return value;
    }

    /// <summary>
    /// Records <paramref name="value"/> to the journal of the root operation <paramref name="log"/>, in the
    /// form <c>`valueName`: value</c> with <paramref name="value"/> rendered as indented JSON, then returns
    /// <paramref name="value"/> unchanged - so this can be chained directly onto an expression, e.g.
    /// <c>var order = FetchOrder(id).AppendJsonTo(log);</c>. See <see cref="IOperationLog.AppendJson{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="value">The value to record.</param>
    /// <param name="log">The root operation to record the value to.</param>
    /// <param name="valueName">
    /// The name to label the value with. Defaults to the source text of the <paramref name="value"/>
    /// argument expression, via <see cref="CallerArgumentExpressionAttribute"/>.
    /// </param>
    /// <returns><paramref name="value"/>, unchanged.</returns>
    [RequiresUnreferencedCode("AppendJson serializes an arbitrary value using reflection-based System.Text.Json, whose required members cannot be statically determined. Use AppendValue instead, or preserve the serialized type.")]
    [RequiresDynamicCode("AppendJson serializes an arbitrary value using reflection-based System.Text.Json, which may require runtime code generation. Use AppendValue instead in a Native AOT application.")]
    [return: NotNullIfNotNull(nameof(value))]
    public static T AppendJsonTo<T>(this T value, IOperationLog log, [CallerArgumentExpression(nameof(value))] string? valueName = null)
    {
        ArgumentNullException.ThrowIfNull(log);

        log.AppendJson(value, valueName);
        return value;
    }

    /// <summary>
    /// Records <paramref name="value"/> to the journal of the sub-operation <paramref name="log"/>, in the
    /// form <c>`valueName`: value</c> with <paramref name="value"/> rendered as indented JSON, then returns
    /// <paramref name="value"/> unchanged - so this can be chained directly onto an expression, e.g.
    /// <c>var order = FetchOrder(id).AppendJsonTo(subLog);</c>. See <see cref="ISubOperationLog.AppendJson{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <param name="value">The value to record.</param>
    /// <param name="log">The sub-operation to record the value to.</param>
    /// <param name="valueName">
    /// The name to label the value with. Defaults to the source text of the <paramref name="value"/>
    /// argument expression, via <see cref="CallerArgumentExpressionAttribute"/>.
    /// </param>
    /// <returns><paramref name="value"/>, unchanged.</returns>
    [RequiresUnreferencedCode("AppendJson serializes an arbitrary value using reflection-based System.Text.Json, whose required members cannot be statically determined. Use AppendValue instead, or preserve the serialized type.")]
    [RequiresDynamicCode("AppendJson serializes an arbitrary value using reflection-based System.Text.Json, which may require runtime code generation. Use AppendValue instead in a Native AOT application.")]
    [return: NotNullIfNotNull(nameof(value))]
    public static T AppendJsonTo<T>(this T value, ISubOperationLog log, [CallerArgumentExpression(nameof(value))] string? valueName = null)
    {
        ArgumentNullException.ThrowIfNull(log);

        log.AppendJson(value, valueName);
        return value;
    }
}
