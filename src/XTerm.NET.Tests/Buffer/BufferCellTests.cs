using XTerm.Buffer;
using XTerm.Common;

namespace XTerm.Tests.Buffer;

[TestClass]

public class BufferCellTests
{
    [TestMethod]
    public void Constructor_Default_SetsNullValues()
    {
        // Arrange & Act
        var cell = new BufferCell();

        // Assert
        cell.Content.Should().Be(BufferCell.Empty.Content);
        cell.Width.Should().Be(BufferCell.Empty.Width);
        cell.CodePoint.Should().Be(BufferCell.Empty.CodePoint);
        cell.Attributes.Should().Be(AttributeData.Default);
    }

    [TestMethod]
    public void Constructor_WithContent_SetsValues()
    {
        // Arrange
        var content = "A";
        var width = 1;
        var attr = new AttributeData(10, 20, 0);

        // Act
        var cell = new BufferCell(content, width, attr);

        // Assert
        cell.Content.Should().Be(content);
        cell.Width.Should().Be(width);
        cell.CodePoint.Should().Be('A');
        cell.Attributes.Should().Be(attr);
    }

    [TestMethod]
    public void Constructor_WithCodePoint_SetsValues()
    {
        // Arrange
        var codePoint = 65; // 'A'
        var width = 1;
        var attr = new AttributeData(10, 20, 0);

        // Act
        var cell = new BufferCell(codePoint, width, attr);

        // Assert
        cell.Content.Should().Be("A");
        cell.Width.Should().Be(width);
        cell.CodePoint.Should().Be(codePoint);
        cell.Attributes.Should().Be(attr);
    }

    [TestMethod]
    public void Null_Property_ReturnsNullCell()
    {
        // Act
        var cell = BufferCell.Empty;

        // Assert
        cell.Content.Should().Be(BufferCell.Empty.Content);
        cell.Width.Should().Be(BufferCell.Empty.Width);
        cell.CodePoint.Should().Be(BufferCell.Empty.CodePoint);
    }

    [TestMethod]
    public void Whitespace_Property_ReturnsWhitespaceCell()
    {
        // Act
        var cell = BufferCell.Space;

        // Assert
        cell.Content.Should().Be(BufferCell.Space.Content);
        cell.Width.Should().Be(BufferCell.Space.Width);
        cell.CodePoint.Should().Be(BufferCell.Space.CodePoint);
    }

    [TestMethod]
    public void IsNull_NullCell_ReturnsTrue()
    {
        // Arrange
        var cell = BufferCell.Empty;

        // Act & Assert
        cell.IsEmpty().Should().BeTrue();
    }

    [TestMethod]
    public void IsNull_NonNullCell_ReturnsFalse()
    {
        // Arrange
        var cell = new BufferCell("A", 1, AttributeData.Default);

        // Act & Assert
        cell.IsEmpty().Should().BeFalse();
    }

    [TestMethod]
    public void IsWhitespace_WhitespaceCell_ReturnsTrue()
    {
        // Arrange
        var cell = BufferCell.Space;

        // Act & Assert
        cell.IsSpace().Should().BeTrue();
    }

    [TestMethod]
    public void IsWhitespace_NonWhitespaceCell_ReturnsFalse()
    {
        // Arrange
        var cell = new BufferCell("A", 1, AttributeData.Default);

        // Act & Assert
        cell.IsSpace().Should().BeFalse();
    }

    [TestMethod]
    public void GetWidth_ReturnsWidth()
    {
        // Arrange
        var cell = new BufferCell("A", 2, AttributeData.Default);

        // Act
        var width = cell.Width;

        // Assert
        width.Should().Be(2);
    }

    [TestMethod]
    public void GetChars_ReturnsContent()
    {
        // Arrange
        var content = "ABC";
        var cell = new BufferCell(content, 1, AttributeData.Default);

        // Act
        var chars = cell.Content;

        // Assert
        chars.Should().Be(content);
    }

    [TestMethod]
    public void GetCode_ReturnsCodePoint()
    {
        // Arrange
        var codePoint = 65;
        var cell = new BufferCell(codePoint, 1, AttributeData.Default);

        // Act
        var code = cell.CodePoint;

        // Assert
        code.Should().Be(codePoint);
    }

    [TestMethod]
    public void Equals_SameCells_ReturnsTrue()
    {
        // Arrange
        var attr = new AttributeData(10, 20, 0);
        var cell1 = new BufferCell("A", 1, attr);
        var cell2 = new BufferCell("A", 1, attr);

        // Act & Assert
        cell1.Equals(cell2).Should().BeTrue();
        ((cell1 == cell2)).Should().BeTrue();
    }

    [TestMethod]
    public void Equals_DifferentContent_ReturnsFalse()
    {
        // Arrange
        var attr = new AttributeData(10, 20, 0);
        var cell1 = new BufferCell("A", 1, attr);
        var cell2 = new BufferCell("B", 1, attr);

        // Act & Assert
        cell1.Equals(cell2).Should().BeFalse();
        ((cell1 != cell2)).Should().BeTrue();
    }

    [TestMethod]
    public void Equals_DifferentWidth_ReturnsFalse()
    {
        // Arrange
        var attr = new AttributeData(10, 20, 0);
        var cell1 = new BufferCell("A", 1, attr);
        var cell2 = new BufferCell("A", 2, attr);

        // Act & Assert
        cell1.Equals(cell2).Should().BeFalse();
    }

    [TestMethod]
    public void Equals_DifferentAttributes_ReturnsFalse()
    {
        // Arrange
        var attr1 = new AttributeData(10, 20, 0);
        var attr2 = new AttributeData(30, 40, 0);
        var cell1 = new BufferCell("A", 1, attr1);
        var cell2 = new BufferCell("A", 1, attr2);

        // Act & Assert
        cell1.Equals(cell2).Should().BeFalse();
    }

    [TestMethod]
    public void Clone_CreatesIndependentCopy()
    {
        // Arrange
        var attr = new AttributeData(10, 20, 0);
        var cell = new BufferCell("A", 1, attr);

        // Act
        var clone = cell;

        // Assert
        clone.Content.Should().Be(cell.Content);
        clone.Width.Should().Be(cell.Width);
        clone.CodePoint.Should().Be(cell.CodePoint);
        clone.Attributes.Should().Be(cell.Attributes);
        
        // Verify it's a true copy (modifying attributes doesn't affect original)
        clone.Attributes.SetBold(true);
        clone.Attributes.IsBold().Should().NotBe(cell.Attributes.IsBold());
    }

    [TestMethod]
    public void GetHashCode_SameCells_ReturnsSameHash()
    {
        // Arrange
        var attr = new AttributeData(10, 20, 0);
        var cell1 = new BufferCell("A", 1, attr);
        var cell2 = new BufferCell("A", 1, attr);

        // Act & Assert
        cell2.GetHashCode().Should().Be(cell1.GetHashCode());
    }

    [TestMethod]
    [DataRow("A", 1)]
    [DataRow("?", 2)] // Wide character
    [DataRow("??", 2)] // Emoji
    [DataRow(" ", 1)] // Space
    public void Constructor_VariousCharacters_HandlesCorrectly(string content, int expectedWidth)
    {
        // Arrange & Act
        var cell = new BufferCell(content, expectedWidth, AttributeData.Default);

        // Assert
        cell.Content.Should().Be(content);
        cell.Width.Should().Be(expectedWidth);
    }
}
