using XTerm.Buffer;

namespace XTerm.Tests.Buffer;

[TestClass]

public class AttributeDataTests
{
    [TestMethod]
    public void Constructor_Default_SetsDefaultValues()
    {
        // Arrange & Act
        var attr = new AttributeData();

        // Assert
        attr.Fg.Should().Be(256);
        attr.Bg.Should().Be(257);
        attr.Extended.Should().Be(0);
    }

    [TestMethod]
    public void Constructor_WithParameters_SetsValues()
    {
        // Arrange & Act
        var attr = new AttributeData(10, 20, 5);

        // Assert
        attr.Fg.Should().Be(10);
        attr.Bg.Should().Be(20);
        attr.Extended.Should().Be(5);
    }

    [TestMethod]
    public void Default_Property_ReturnsDefaultAttributes()
    {
        // Act
        var attr = AttributeData.Default;

        // Assert
        attr.Fg.Should().Be(256);
        attr.Bg.Should().Be(257);
        attr.Extended.Should().Be(0);
    }

    [TestMethod]
    public void SetBold_True_SetsBoldFlag()
    {
        // Arrange
        var attr = new AttributeData();

        // Act
        attr.SetBold(true);

        // Assert
        attr.IsBold().Should().BeTrue();
    }

    [TestMethod]
    public void SetBold_False_ClearsBoldFlag()
    {
        // Arrange
        var attr = new AttributeData();
        attr.SetBold(true);

        // Act
        attr.SetBold(false);

        // Assert
        attr.IsBold().Should().BeFalse();
    }

    [TestMethod]
    public void SetDim_True_SetsDimFlag()
    {
        // Arrange
        var attr = new AttributeData();

        // Act
        attr.SetDim(true);

        // Assert
        attr.IsDim().Should().BeTrue();
    }

    [TestMethod]
    public void SetItalic_True_SetsItalicFlag()
    {
        // Arrange
        var attr = new AttributeData();

        // Act
        attr.SetItalic(true);

        // Assert
        attr.IsItalic().Should().BeTrue();
    }

    [TestMethod]
    public void SetUnderline_True_SetsUnderlineFlag()
    {
        // Arrange
        var attr = new AttributeData();

        // Act
        attr.SetUnderline(true);

        // Assert
        attr.IsUnderline().Should().BeTrue();
    }

    [TestMethod]
    public void SetBlink_True_SetsBlinkFlag()
    {
        // Arrange
        var attr = new AttributeData();

        // Act
        attr.SetBlink(true);

        // Assert
        attr.IsBlink().Should().BeTrue();
    }

    [TestMethod]
    public void SetInverse_True_SetsInverseFlag()
    {
        // Arrange
        var attr = new AttributeData();

        // Act
        attr.SetInverse(true);

        // Assert
        attr.IsInverse().Should().BeTrue();
    }

    [TestMethod]
    public void SetInvisible_True_SetsInvisibleFlag()
    {
        // Arrange
        var attr = new AttributeData();

        // Act
        attr.SetInvisible(true);

        // Assert
        attr.IsInvisible().Should().BeTrue();
    }

    [TestMethod]
    public void SetStrikethrough_True_SetsStrikethroughFlag()
    {
        // Arrange
        var attr = new AttributeData();

        // Act
        attr.SetStrikethrough(true);

        // Assert
        attr.IsStrikethrough().Should().BeTrue();
    }

    [TestMethod]
    public void SetOverline_True_SetsOverlineFlag()
    {
        // Arrange
        var attr = new AttributeData();

        // Act
        attr.SetOverline(true);

        // Assert
        attr.IsOverline().Should().BeTrue();
    }

    [TestMethod]
    public void MultipleFlags_CanBeSetSimultaneously()
    {
        // Arrange
        var attr = new AttributeData();

        // Act
        attr.SetBold(true);
        attr.SetItalic(true);
        attr.SetUnderline(true);

        // Assert
        attr.IsBold().Should().BeTrue();
        attr.IsItalic().Should().BeTrue();
        attr.IsUnderline().Should().BeTrue();
    }

    [TestMethod]
    public void SetFgColor_SetsColor()
    {
        // Arrange
        var attr = new AttributeData();
        var color = 15;

        // Act
        attr.SetFgColor(color);

        // Assert
        attr.GetFgColor().Should().Be(color);
    }

    [TestMethod]
    public void SetFgColor_WithMode_SetsColorAndMode()
    {
        // Arrange
        var attr = new AttributeData();
        var color = 0xFF0000; // Red in RGB
        var mode = 1;

        // Act
        attr.SetFgColor(color, mode);

        // Assert
        attr.GetFgColor().Should().Be(color);
        attr.GetFgColorMode().Should().Be(mode);
    }

    [TestMethod]
    public void SetBgColor_SetsColor()
    {
        // Arrange
        var attr = new AttributeData();
        var color = 10;

        // Act
        attr.SetBgColor(color);

        // Assert
        attr.GetBgColor().Should().Be(color);
    }

    [TestMethod]
    public void SetBgColor_WithMode_SetsColorAndMode()
    {
        // Arrange
        var attr = new AttributeData();
        var color = 0x00FF00; // Green in RGB
        var mode = 1;

        // Act
        attr.SetBgColor(color, mode);

        // Assert
        attr.GetBgColor().Should().Be(color);
        attr.GetBgColorMode().Should().Be(mode);
    }

    [TestMethod]
    public void Equals_SameAttributes_ReturnsTrue()
    {
        // Arrange
        var attr1 = new AttributeData(10, 20, 5);
        var attr2 = new AttributeData(10, 20, 5);

        // Act & Assert
        attr1.Equals(attr2).Should().BeTrue();
        ((attr1 == attr2)).Should().BeTrue();
    }

    [TestMethod]
    public void Equals_DifferentFg_ReturnsFalse()
    {
        // Arrange
        var attr1 = new AttributeData(10, 20, 5);
        var attr2 = new AttributeData(15, 20, 5);

        // Act & Assert
        attr1.Equals(attr2).Should().BeFalse();
        ((attr1 != attr2)).Should().BeTrue();
    }

    [TestMethod]
    public void Equals_DifferentBg_ReturnsFalse()
    {
        // Arrange
        var attr1 = new AttributeData(10, 20, 5);
        var attr2 = new AttributeData(10, 25, 5);

        // Act & Assert
        attr1.Equals(attr2).Should().BeFalse();
    }

    [TestMethod]
    public void Equals_DifferentExtended_ReturnsFalse()
    {
        // Arrange
        var attr1 = new AttributeData(10, 20, 5);
        var attr2 = new AttributeData(10, 20, 10);

        // Act & Assert
        attr1.Equals(attr2).Should().BeFalse();
    }

    [TestMethod]
    public void Clone_CreatesIndependentCopy()
    {
        // Arrange
        var attr = new AttributeData(10, 20, 0);
        attr.SetBold(true);
        attr.SetItalic(true);

        // Act
        var clone = attr;

        // Assert
        clone.Fg.Should().Be(attr.Fg);
        clone.Bg.Should().Be(attr.Bg);
        clone.Extended.Should().Be(attr.Extended);
        clone.IsBold().Should().BeTrue();
        clone.IsItalic().Should().BeTrue();
        
        // Verify it's a true copy
        clone.SetBold(false);
        attr.IsBold().Should().BeTrue();
        clone.IsBold().Should().BeFalse();
    }

    [TestMethod]
    public void GetHashCode_SameAttributes_ReturnsSameHash()
    {
        // Arrange
        var attr1 = new AttributeData(10, 20, 5);
        var attr2 = new AttributeData(10, 20, 5);

        // Act & Assert
        attr2.GetHashCode().Should().Be(attr1.GetHashCode());
    }

    [TestMethod]
    public void AllFlags_CanBeToggled()
    {
        // Arrange
        var attr = new AttributeData();

        // Act - Set all flags
        attr.SetBold(true);
        attr.SetDim(true);
        attr.SetItalic(true);
        attr.SetUnderline(true);
        attr.SetBlink(true);
        attr.SetInverse(true);
        attr.SetInvisible(true);
        attr.SetStrikethrough(true);
        attr.SetOverline(true);

        // Assert - All should be true
        attr.IsBold().Should().BeTrue();
        attr.IsDim().Should().BeTrue();
        attr.IsItalic().Should().BeTrue();
        attr.IsUnderline().Should().BeTrue();
        attr.IsBlink().Should().BeTrue();
        attr.IsInverse().Should().BeTrue();
        attr.IsInvisible().Should().BeTrue();
        attr.IsStrikethrough().Should().BeTrue();
        attr.IsOverline().Should().BeTrue();

        // Act - Clear all flags
        attr.SetBold(false);
        attr.SetDim(false);
        attr.SetItalic(false);
        attr.SetUnderline(false);
        attr.SetBlink(false);
        attr.SetInverse(false);
        attr.SetInvisible(false);
        attr.SetStrikethrough(false);
        attr.SetOverline(false);

        // Assert - All should be false
        attr.IsBold().Should().BeFalse();
        attr.IsDim().Should().BeFalse();
        attr.IsItalic().Should().BeFalse();
        attr.IsUnderline().Should().BeFalse();
        attr.IsBlink().Should().BeFalse();
        attr.IsInverse().Should().BeFalse();
        attr.IsInvisible().Should().BeFalse();
        attr.IsStrikethrough().Should().BeFalse();
        attr.IsOverline().Should().BeFalse();
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(7)]
    [DataRow(15)]
    [DataRow(255)]
    public void SetFgColor_VariousValues_WorksCorrectly(int color)
    {
        // Arrange
        var attr = new AttributeData();

        // Act
        attr.SetFgColor(color);

        // Assert
        attr.GetFgColor().Should().Be(color);
    }

    [TestMethod]
    [DataRow(0)]
    [DataRow(7)]
    [DataRow(15)]
    [DataRow(255)]
    public void SetBgColor_VariousValues_WorksCorrectly(int color)
    {
        // Arrange
        var attr = new AttributeData();

        // Act
        attr.SetBgColor(color);

        // Assert
        attr.GetBgColor().Should().Be(color);
    }
}
