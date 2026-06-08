using Moq;

namespace RandomSkunk.StructuredLogging.Testing;

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

        _mockLog.Verify(m => m.OnAppend("Hello, world!"), Times.Once());
        _mockLog.Verify(m => m.OnAppend("Good-bye, cruel world!"), Times.Once());
    }

    [Fact]
    public void InterfaceReturnValueMethod_PassesReturnValueToAbstractReturnValueMethod()
    {
        const int expectedReturnValue = 123;
        int actualReturnValue = _log.ReturnValue(expectedReturnValue);

        _mockLog.Verify(m => m.OnReturnValue(expectedReturnValue), Times.Once());
        Assert.Equal(expectedReturnValue, actualReturnValue);
    }

    [Fact]
    public void InterfaceExceptionMethod_PassesExceptionToAbstractExceptionMethod()
    {
        Exception expectedException = new();
        Exception actualException = _log.Exception(expectedException);

        _mockLog.Verify(m => m.OnException(expectedException), Times.Once());
        Assert.Equal(expectedException, actualException);
    }

    [Fact]
    public void InterfaceValueMethod_PassesValueToAbstractValueMethod()
    {
        const int expectedValue = 123;
        int actualValue = _log.Value(expectedValue);

        _mockLog.Verify(m => m.OnValue(expectedValue), Times.Once());
        Assert.Equal(expectedValue, actualValue);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void InterfaceConditionMethod_PassesConditionToAbstractConditionMethod(bool expectedCondition)
    {
        bool actualCondition = _log.Condition(expectedCondition);

        _mockLog.Verify(m => m.OnCondition(expectedCondition), Times.Once());
        Assert.Equal(expectedCondition, actualCondition);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData(null)]
    public void InterfaceIsNullWhereTIsNullableClassMethod_PassesValueToAbstractIsNullMethod(string? value)
    {
        bool actualIsNull = _log.IsNull(value);

        _mockLog.Verify(m => m.OnIsNull(value), Times.Once());
        Assert.Equal(value is null, actualIsNull);
    }

    [Theory]
    [InlineData(123)]
    [InlineData(null)]
    public void InterfaceIsNullWhereTIsNullableStructMethod_PassesValueToAbstractIsNullMethod(int? value)
    {
        bool actualIsNull = _log.IsNull(value);

        _mockLog.Verify(m => m.OnIsNull(value), Times.Once());
        Assert.Equal(!value.HasValue, actualIsNull);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData(null)]
    [InlineData("")]
    public void InterfaceIsNullOrEmptyMethod_PassesValueToAbstractIsNullOrEmptyMethod(string? value)
    {
        bool actualIsNullOrEmpty = _log.IsNullOrEmpty(value);

        _mockLog.Verify(m => m.OnIsNullOrEmpty(value), Times.Once());
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

        _mockLog.Verify(m => m.OnIsNullOrWhiteSpace(value), Times.Once());
        Assert.Equal(string.IsNullOrWhiteSpace(value), actualIsNullOrWhiteSpace);
    }
}
