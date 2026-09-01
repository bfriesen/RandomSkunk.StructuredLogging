using System.Reflection;
using AwesomeAssertions;
using RandomSkunk.StructuredLogging.Operation;

namespace RandomSkunk.StructuredLogging.Tests;

/// <summary>
/// Reflection-based tests ensuring <see cref="IOperationLogBase"/>, <see cref="IOperationLog"/>, and
/// <see cref="ISubOperationLog"/> stay in sync: each declares its own copy of the members it shares with
/// the others (differing only in return type, per the fluent-return-type pattern documented on each
/// interface), and it's easy for a new/changed member to be added to one interface and forgotten on
/// another. These tests catch that in two parts: comparing declared methods across all three interfaces
/// while ignoring return type, and separately verifying each interface's own declared methods all use the
/// return type its fluent-return-type pattern calls for (<see langword="void"/> for
/// <see cref="IOperationLogBase"/>, except <see cref="IOperationLogBase.BeginSubOperation(string)"/>, which
/// returns <see cref="ISubOperationLog"/> everywhere).
/// </summary>
public class OperationLogInterfaceParityTests
{
    [Fact]
    public void ISubOperationLog_MethodsExistOnTheOtherTwoInterfaces()
    {
        List<MethodInfo> subOperationLogMethods = GetDeclaredMethods(typeof(ISubOperationLog));

        AssertAllFoundIgnoringReturnType(subOperationLogMethods, typeof(IOperationLogBase));
        AssertAllFoundIgnoringReturnType(subOperationLogMethods, typeof(IOperationLog));
    }

    [Fact]
    public void IOperationLog_MethodsExceptSetResultAndSetExceptionExistOnTheOtherTwoInterfaces()
    {
        List<MethodInfo> operationLogMethods = GetDeclaredMethods(typeof(IOperationLog))
            .Where(method => method.Name is not (nameof(IOperationLog.SetResult) or nameof(IOperationLog.SetException)))
            .ToList();

        AssertAllFoundIgnoringReturnType(operationLogMethods, typeof(IOperationLogBase));
        AssertAllFoundIgnoringReturnType(operationLogMethods, typeof(ISubOperationLog));
    }

    [Fact]
    public void IOperationLogBase_MethodsExceptBeginSubOperationExistOnTheOtherTwoInterfaces()
    {
        List<MethodInfo> operationLogBaseMethods = GetDeclaredMethods(typeof(IOperationLogBase))
            .Where(method => method.Name is not nameof(IOperationLogBase.BeginSubOperation))
            .ToList();

        AssertAllFoundIgnoringReturnType(operationLogBaseMethods, typeof(IOperationLog));
        AssertAllFoundIgnoringReturnType(operationLogBaseMethods, typeof(ISubOperationLog));
    }

    [Fact]
    public void ISubOperationLog_MethodsAllReturnISubOperationLog()
    {
        foreach (MethodInfo method in GetDeclaredMethods(typeof(ISubOperationLog)))
            method.ReturnType.Should().Be(typeof(ISubOperationLog), because: $"{Describe(method)} is fluent and should return this ISubOperationLog");
    }

    [Fact]
    public void IOperationLog_MethodsAllReturnIOperationLog()
    {
        foreach (MethodInfo method in GetDeclaredMethods(typeof(IOperationLog)))
            method.ReturnType.Should().Be(typeof(IOperationLog), because: $"{Describe(method)} is fluent and should return this IOperationLog");
    }

    [Fact]
    public void IOperationLogBase_MethodsReturnVoidExceptBeginSubOperationWhichReturnsISubOperationLog()
    {
        foreach (MethodInfo method in GetDeclaredMethods(typeof(IOperationLogBase)))
        {
            Type expectedReturnType = method.Name == nameof(IOperationLogBase.BeginSubOperation)
                ? typeof(ISubOperationLog)
                : typeof(void);

            method.ReturnType.Should().Be(expectedReturnType, because: $"{Describe(method)} should return {expectedReturnType.Name}");
        }
    }

    private static List<MethodInfo> GetDeclaredMethods(Type interfaceType) =>
        interfaceType
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(method => !method.IsSpecialName) // Excludes property accessors.
            .ToList();

    private static void AssertAllFoundIgnoringReturnType(List<MethodInfo> methods, Type otherInterfaceType)
    {
        List<MethodInfo> otherMethods = GetDeclaredMethods(otherInterfaceType);

        foreach (MethodInfo method in methods)
        {
            otherMethods.Should().Contain(
                otherMethod => HaveSameSignatureIgnoringReturnType(method, otherMethod),
                because: $"{Describe(method)} should have a matching member (differing only by return type) on {otherInterfaceType.Name}");
        }
    }

    private static bool HaveSameSignatureIgnoringReturnType(MethodInfo left, MethodInfo right)
    {
        if (left.Name != right.Name)
            return false;

        Type[] leftGenericArguments = left.IsGenericMethod ? left.GetGenericArguments() : Type.EmptyTypes;
        Type[] rightGenericArguments = right.IsGenericMethod ? right.GetGenericArguments() : Type.EmptyTypes;

        if (leftGenericArguments.Length != rightGenericArguments.Length)
            return false;

        ParameterInfo[] leftParameters = left.GetParameters();
        ParameterInfo[] rightParameters = right.GetParameters();

        if (leftParameters.Length != rightParameters.Length)
            return false;

        for (int i = 0; i < leftParameters.Length; i++)
        {
            if (!ParameterTypesMatch(leftParameters[i], leftGenericArguments, rightParameters[i], rightGenericArguments))
                return false;
        }

        return true;
    }

    private static bool ParameterTypesMatch(
        ParameterInfo left, Type[] leftGenericArguments, ParameterInfo right, Type[] rightGenericArguments)
    {
        Type leftType = left.ParameterType;
        Type rightType = right.ParameterType;

        int leftGenericPosition = GenericParameterPosition(leftType, leftGenericArguments);
        int rightGenericPosition = GenericParameterPosition(rightType, rightGenericArguments);

        // Both parameters reference the same position among their method's own generic type parameters
        // (e.g. both are "T", the first generic parameter of AddProperty<T>) - the two Type instances are
        // necessarily different (each interface declares its own T), but they play the same structural role.
        if (leftGenericPosition >= 0 || rightGenericPosition >= 0)
            return leftGenericPosition == rightGenericPosition;

        return leftType == rightType;
    }

    private static int GenericParameterPosition(Type type, Type[] methodGenericArguments)
    {
        Type elementType = type.IsByRef ? type.GetElementType()! : type;
        return elementType.IsGenericMethodParameter ? Array.IndexOf(methodGenericArguments, elementType) : -1;
    }

    private static string Describe(MethodInfo method) =>
        $"{method.DeclaringType!.Name}.{method.Name}({string.Join(", ", method.GetParameters().Select(p => p.ParameterType.Name))})";
}
