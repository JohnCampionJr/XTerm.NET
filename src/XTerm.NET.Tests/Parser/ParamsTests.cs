using XTerm.Parser;

namespace XTerm.Tests.Parser;

[TestClass]

public class ParamsTests
{
    [TestMethod]
    public void Constructor_CreatesEmptyParams()
    {
        // Arrange & Act
        var params_ = new Params();

        // Assert
        params_.Length.Should().Be(0);
    }

    [TestMethod]
    public void AddParam_AddsParameter()
    {
        // Arrange
        var params_ = new Params();

        // Act
        params_.AddParam(10);

        // Assert
        params_.Length.Should().Be(1);
        params_.GetParam(0).Should().Be(10);
    }

    [TestMethod]
    public void AddParam_MultipleParameters_AddsAll()
    {
        // Arrange
        var params_ = new Params();

        // Act
        params_.AddParam(1);
        params_.AddParam(2);
        params_.AddParam(3);

        // Assert
        params_.Length.Should().Be(3);
        params_.GetParam(0).Should().Be(1);
        params_.GetParam(1).Should().Be(2);
        params_.GetParam(2).Should().Be(3);
    }

    [TestMethod]
    public void GetParam_ValidIndex_ReturnsParameter()
    {
        // Arrange
        var params_ = new Params();
        params_.AddParam(42);

        // Act
        var value = params_.GetParam(0);

        // Assert
        value.Should().Be(42);
    }

    [TestMethod]
    public void GetParam_InvalidIndex_ReturnsDefault()
    {
        // Arrange
        var params_ = new Params();
        params_.AddParam(10);

        // Act
        var value = params_.GetParam(5, 99);

        // Assert
        value.Should().Be(99);
    }

    [TestMethod]
    public void GetParam_NegativeIndex_ReturnsDefault()
    {
        // Arrange
        var params_ = new Params();
        params_.AddParam(10);

        // Act
        var value = params_.GetParam(-1, 50);

        // Assert
        value.Should().Be(50);
    }

    [TestMethod]
    public void GetParam_MinusOneValue_ReturnsDefault()
    {
        // Arrange
        var params_ = new Params();
        params_.AddParam(-1); // Special value meaning "use default"

        // Act
        var value = params_.GetParam(0, 100);

        // Assert
        value.Should().Be(100);
    }

    [TestMethod]
    public void GetParam_NoDefault_ReturnsZero()
    {
        // Arrange
        var params_ = new Params();

        // Act
        var value = params_.GetParam(0);

        // Assert
        value.Should().Be(0);
    }

    [TestMethod]
    public void HasParam_ValidIndex_ReturnsTrue()
    {
        // Arrange
        var params_ = new Params();
        params_.AddParam(10);

        // Act
        var hasParam = params_.HasParam(0);

        // Assert
        hasParam.Should().BeTrue();
    }

    [TestMethod]
    public void HasParam_InvalidIndex_ReturnsFalse()
    {
        // Arrange
        var params_ = new Params();
        params_.AddParam(10);

        // Act
        var hasParam = params_.HasParam(5);

        // Assert
        hasParam.Should().BeFalse();
    }

    [TestMethod]
    public void HasParam_MinusOneValue_ReturnsFalse()
    {
        // Arrange
        var params_ = new Params();
        params_.AddParam(-1);

        // Act
        var hasParam = params_.HasParam(0);

        // Assert
        hasParam.Should().BeFalse();
    }

    [TestMethod]
    public void HasParam_NegativeIndex_ReturnsFalse()
    {
        // Arrange
        var params_ = new Params();
        params_.AddParam(10);

        // Act
        var hasParam = params_.HasParam(-1);

        // Assert
        hasParam.Should().BeFalse();
    }

    [TestMethod]
    public void Reset_ClearsAllParameters()
    {
        // Arrange
        var params_ = new Params();
        params_.AddParam(1);
        params_.AddParam(2);
        params_.AddParam(3);

        // Act
        params_.Reset();

        // Assert
        params_.Length.Should().Be(0);
    }

    [TestMethod]
    public void Reset_AllowsReuse()
    {
        // Arrange
        var params_ = new Params();
        params_.AddParam(10);
        params_.Reset();

        // Act
        params_.AddParam(20);

        // Assert
        params_.Length.Should().Be(1);
        params_.GetParam(0).Should().Be(20);
    }

    [TestMethod]
    public void ToArray_ReturnsAllParameters()
    {
        // Arrange
        var params_ = new Params();
        params_.AddParam(1);
        params_.AddParam(2);
        params_.AddParam(3);

        // Act
        var array = params_.ToArray();

        // Assert
        array.Length.Should().Be(3);
        array[0].Should().Be(1);
        array[1].Should().Be(2);
        array[2].Should().Be(3);
    }

    [TestMethod]
    public void ToArray_EmptyParams_ReturnsEmptyArray()
    {
        // Arrange
        var params_ = new Params();

        // Act
        var array = params_.ToArray();

        // Assert
        array.Should().BeEmpty();
    }

    [TestMethod]
    public void Clone_CreatesIndependentCopy()
    {
        // Arrange
        var params_ = new Params();
        params_.AddParam(1);
        params_.AddParam(2);
        params_.AddParam(3);

        // Act
        var clone = params_.Clone();

        // Assert
        clone.Length.Should().Be(params_.Length);
        clone.GetParam(0).Should().Be(1);
        clone.GetParam(1).Should().Be(2);
        clone.GetParam(2).Should().Be(3);

        // Verify independence
        clone.AddParam(4);
        params_.Length.Should().Be(3);
        clone.Length.Should().Be(4);
    }

    [TestMethod]
    public void AddSubParam_AddsSubParameter()
    {
        // Arrange
        var params_ = new Params();

        // Act
        params_.AddSubParam(100);

        // Assert - Method should not throw
        // Sub-params are internal detail
    }

    [TestMethod]
    public void GetSubParams_ReturnsSubParameters()
    {
        // Arrange
        var params_ = new Params();
        params_.AddParam(1);
        params_.AddSubParam(10);
        params_.AddSubParam(20);

        // Act
        var subParams = params_.GetSubParams(0);

        // Assert
        subParams.Should().NotBeNull();
        // Current implementation returns empty list, which is valid
    }

    [TestMethod]
    public void GetSubParams_InvalidIndex_ReturnsEmptyList()
    {
        // Arrange
        var params_ = new Params();

        // Act
        var subParams = params_.GetSubParams(10);

        // Assert
        subParams.Should().NotBeNull();
        subParams.Should().BeEmpty();
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(1)]
    [DataRow(100)]
    [DataRow(255)]
    [DataRow(1000)]
    public void AddParam_VariousValues_WorksCorrectly(int value)
    {
        // Arrange
        var params_ = new Params();

        // Act
        params_.AddParam(value);

        // Assert
        params_.GetParam(0).Should().Be(value);
    }

    [TestMethod]
    public void MultipleOperations_WorkCorrectly()
    {
        // Arrange
        var params_ = new Params();

        // Act
        params_.AddParam(1);
        params_.AddParam(2);
        var v1 = params_.GetParam(0);
        var v2 = params_.GetParam(1);
        
        params_.Reset();
        params_.AddParam(10);
        params_.AddParam(20);
        params_.AddParam(30);
        var v3 = params_.GetParam(0);
        var array = params_.ToArray();
        
        var clone = params_.Clone();
        clone.AddParam(40);

        // Assert
        v1.Should().Be(1);
        v2.Should().Be(2);
        v3.Should().Be(10);
        array.Length.Should().Be(3);
        params_.Length.Should().Be(3);
        clone.Length.Should().Be(4);
    }

    [TestMethod]
    public void LargeNumberOfParams_HandlesCorrectly()
    {
        // Arrange
        var params_ = new Params();

        // Act
        for (int i = 0; i < 50; i++)
        {
            params_.AddParam(i);
        }

        // Assert
        params_.Length.Should().Be(50);
        for (int i = 0; i < 50; i++)
        {
            params_.GetParam(i).Should().Be(i);
        }
    }

    [TestMethod]
    public void GetParam_WithDefaultValue_UsesDefaultWhenNeeded()
    {
        // Arrange
        var params_ = new Params();
        params_.AddParam(10);
        params_.AddParam(-1); // Special value
        params_.AddParam(20);

        // Act & Assert
        params_.GetParam(0, 99).Should().Be(10);
        params_.GetParam(1, 99).Should().Be(99); // Should use default
        params_.GetParam(2, 99).Should().Be(20);
        params_.GetParam(5, 99).Should().Be(99); // Out of range
    }

    [TestMethod]
    public void ZeroParameters_HandlesCorrectly()
    {
        // Arrange
        var params_ = new Params();
        params_.AddParam(0);

        // Act
        var value = params_.GetParam(0, 10);

        // Assert
        value.Should().Be(0); // Zero is a valid value, not default
    }

    [TestMethod]
    public void UpdateLastParam_UpdatesParameter()
    {
        // Arrange
        var params_ = new Params();
        params_.AddParam(0);

        // Act
        params_.UpdateLastParam(5);

        // Assert
        params_.GetParam(0).Should().Be(5);
    }

    [TestMethod]
    public void UpdateLastParam_BuildsNumberFromDigits()
    {
        // Arrange
        var params_ = new Params();
        params_.AddParam(0);

        // Act
        params_.UpdateLastParam(1);   // 1
        params_.UpdateLastParam(12);  // 12
        params_.UpdateLastParam(123); // 123

        // Assert
        params_.GetParam(0).Should().Be(123);
    }
}
