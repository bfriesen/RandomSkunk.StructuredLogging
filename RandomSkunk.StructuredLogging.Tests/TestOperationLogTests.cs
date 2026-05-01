using Moq;

namespace RandomSkunk.StructuredLogging.Tests;

public class TestOperationLogTests
{
    private readonly Mock<TestOperationLog> _mockLog = new();
    private readonly IOperationLog _log;

    public TestOperationLogTests()
    {
        _log = _mockLog.Object;
    }

    [Theory]
    [InlineData("world")]
    public void InterfaceAppendMethod_PassesLogEntryToAbstractAppendMethod(string who)
    {
        _log.Append($"Hello, {who}!");
        _log.Append($"Good-bye, cruel {who}!");

        _mockLog.Verify(m => m.Append("Hello, world!"), Times.Once());
        _mockLog.Verify(m => m.Append("Good-bye, cruel world!"), Times.Once());
    }

    [Fact]
    public void InterfaceReturnValueMethod_PassesReturnValueToAbstractReturnValueMethod()
    {
        const int expectedReturnValue = 123;
        int actualReturnValue = _log.ReturnValue(expectedReturnValue);

        _mockLog.Verify(m => m.ReturnValue(expectedReturnValue), Times.Once());
        Assert.Equal(expectedReturnValue, actualReturnValue);
    }

    [Fact]
    public void InterfaceExceptionMethod_PassesExceptionToAbstractExceptionMethod()
    {
        Exception expectedException = new();
        Exception actualException = _log.Exception(expectedException);

        _mockLog.Verify(m => m.Exception(expectedException), Times.Once());
        Assert.Equal(expectedException, actualException);
    }

    [Fact]
    public void InterfaceValueMethod_PassesValueToAbstractValueMethod()
    {
        const int expectedValue = 123;
        int actualValue = _log.Value(expectedValue);

        _mockLog.Verify(m => m.Value(expectedValue), Times.Once());
        Assert.Equal(expectedValue, actualValue);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void InterfaceConditionMethod_PassesConditionToAbstractConditionMethod(bool expectedCondition)
    {
        bool actualCondition = _log.Condition(expectedCondition);

        _mockLog.Verify(m => m.Condition(expectedCondition), Times.Once());
        Assert.Equal(expectedCondition, actualCondition);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData(null)]
    public void InterfaceIsNullWhereTIsNullableClassMethod_PassesValueToAbstractIsNullMethod(string? value)
    {
        bool actualIsNull = _log.IsNull(value);

        _mockLog.Verify(m => m.IsNull(value), Times.Once());
        Assert.Equal(value is null, actualIsNull);
    }

    [Theory]
    [InlineData(123)]
    [InlineData(null)]
    public void InterfaceIsNullWhereTIsNullableStructMethod_PassesValueToAbstractIsNullMethod(int? value)
    {
        bool actualIsNull = _log.IsNull(value);

        _mockLog.Verify(m => m.IsNull(value), Times.Once());
        Assert.Equal(!value.HasValue, actualIsNull);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData(null)]
    [InlineData("")]
    public void InterfaceIsNullOrEmptyMethod_PassesValueToAbstractIsNullOrEmptyMethod(string? value)
    {
        bool actualIsNullOrEmpty = _log.IsNullOrEmpty(value);

        _mockLog.Verify(m => m.IsNullOrEmpty(value), Times.Once());
        Assert.Equal(string.IsNullOrEmpty(value), actualIsNullOrEmpty);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void InterfaceIsNullOrWhiteSpaceMethod_PassesValueToAbstractIsNullOrWhiteSpaceMethod(string? value)
    {
        bool actualIsNullOrWhiteSpace = _log.IsNullOrWhiteSpace(value);

        _mockLog.Verify(m => m.IsNullOrWhiteSpace(value), Times.Once());
        Assert.Equal(string.IsNullOrWhiteSpace(value), actualIsNullOrWhiteSpace);
    }
}
