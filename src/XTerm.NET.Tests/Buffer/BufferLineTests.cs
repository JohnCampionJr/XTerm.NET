using XTerm.Buffer;

namespace XTerm.Tests.Buffer;

[TestClass]

public class BufferLineTests
{
    [TestMethod]
    public void Constructor_CreatesLineWithSpecifiedColumns()
    {
        // Arrange
        var cols = 80;

        // Act
        var line = new BufferLine(cols);

        // Assert
        line.Length.Should().Be(cols);
        line.IsWrapped.Should().BeFalse();
    }

    [TestMethod]
    public void Constructor_WithFillCell_FillsAllCells()
    {
        // Arrange
        var cols = 10;
        var fillCell = new BufferCell("X", 1, AttributeData.Default);

        // Act
        var line = new BufferLine(cols, fillCell);

        // Assert
        for (int i = 0; i < cols; i++)
        {
            line[i].Content.Should().Be("X");
        }
    }

    [TestMethod]
    public void Indexer_Get_ReturnsCell()
    {
        // Arrange
        var line = new BufferLine(10);
        var cell = new BufferCell("A", 1, AttributeData.Default);
        line[5] = cell;

        // Act
        var retrieved = line[5];

        // Assert
        retrieved.Content.Should().Be("A");
    }

    [TestMethod]
    public void Indexer_Set_SetsCell()
    {
        // Arrange
        var line = new BufferLine(10);
        var cell = new BufferCell("B", 1, AttributeData.Default);

        // Act
        line[3] = cell;

        // Assert
        line[3].Content.Should().Be("B");
    }

    [TestMethod]
    public void Indexer_OutOfBounds_ReturnsNullCell()
    {
        // Arrange
        var line = new BufferLine(10);

        // Act
        var cell = line[-1];
        var cell2 = line[100];

        // Assert
        cell.IsEmpty().Should().BeTrue();
        cell2.IsEmpty().Should().BeTrue();
    }

    [TestMethod]
    public void Indexer_Set_OutOfBounds_DoesNotThrow()
    {
        // Arrange
        var line = new BufferLine(10);
        var cell = new BufferCell("X", 1, AttributeData.Default);

        // Act & Assert - Should not throw
        line[-1] = cell;
        line[100] = cell;
    }

    [TestMethod]
    public void SetCell_SetsCell()
    {
        // Arrange
        var line = new BufferLine(10);
        var cell = new BufferCell("D", 1, AttributeData.Default);

        // Act
        line.SetCell(4, ref cell);

        // Assert
        line[4].Content.Should().Be("D");
    }

    [TestMethod]
    public void GetCodePoint_ReturnsCodePoint()
    {
        // Arrange
        var line = new BufferLine(10);
        var cell = new BufferCell(65, 1, AttributeData.Default); // 'A'
        line[6] = cell;

        // Act
        var code = line.GetCodePoint(6);

        // Assert
        code.Should().Be(65);
    }

    [TestMethod]
    public void GetCodePoint_OutOfBounds_ReturnsZero()
    {
        // Arrange
        var line = new BufferLine(10);

        // Act
        var code = line.GetCodePoint(-1);
        var code2 = line.GetCodePoint(100);

        // Assert
        code.Should().Be(0);
        code2.Should().Be(0);
    }

    [TestMethod]
    public void Resize_Expand_AddsNewCells()
    {
        // Arrange
        var line = new BufferLine(10);
        var fillCell = new BufferCell("X", 1, AttributeData.Default);
        line[5] = new BufferCell("A", 1, AttributeData.Default);

        // Act
        line.Resize(20, fillCell);

        // Assert
        line.Length.Should().Be(20);
        line[5].Content.Should().Be("A"); // Original data preserved
        line[15].Content.Should().Be("X"); // New cells filled
    }

    [TestMethod]
    public void Resize_Shrink_TruncatesCells()
    {
        // Arrange
        var line = new BufferLine(20);
        var fillCell = BufferCell.Empty;
        line[15] = new BufferCell("A", 1, AttributeData.Default);

        // Act
        line.Resize(10, fillCell);

        // Assert
        line.Length.Should().Be(10);
    }

    [TestMethod]
    public void Resize_SameSize_DoesNothing()
    {
        // Arrange
        var line = new BufferLine(10);
        var fillCell = BufferCell.Empty;
        line[5] = new BufferCell("A", 1, AttributeData.Default);

        // Act
        line.Resize(10, fillCell);

        // Assert
        line.Length.Should().Be(10);
        line[5].Content.Should().Be("A");
    }

    [TestMethod]
    public void Fill_FillsRange()
    {
        // Arrange
        var line = new BufferLine(10);
        var fillCell = new BufferCell("F", 1, AttributeData.Default);

        // Act
        line.Fill(fillCell, 2, 5);

        // Assert
        (line[1].IsSpace()).Should().BeTrue(); // Before range
        line[2].Content.Should().Be("F");
        line[3].Content.Should().Be("F");
        line[4].Content.Should().Be("F");
        (line[5].IsSpace()).Should().BeTrue(); // After range
    }

    [TestMethod]
    public void Fill_NoParameters_FillsEntireLine()
    {
        // Arrange
        var line = new BufferLine(10);
        var fillCell = new BufferCell("G", 1, AttributeData.Default);

        // Act
        line.Fill(fillCell);

        // Assert
        for (int i = 0; i < 10; i++)
        {
            line[i].Content.Should().Be("G");
        }
    }

    [TestMethod]
    public void CopyCellsFrom_Forward_CopiesCells()
    {
        // Arrange
        var srcLine = new BufferLine(10);
        var destLine = new BufferLine(10);
        srcLine[2] = new BufferCell("A", 1, AttributeData.Default);
        srcLine[3] = new BufferCell("B", 1, AttributeData.Default);
        srcLine[4] = new BufferCell("C", 1, AttributeData.Default);

        // Act
        destLine.CopyCellsFrom(srcLine, 2, 5, 3, false);

        // Assert
        destLine[5].Content.Should().Be("A");
        destLine[6].Content.Should().Be("B");
        destLine[7].Content.Should().Be("C");
    }

    [TestMethod]
    public void CopyCellsFrom_Reverse_CopiesCells()
    {
        // Arrange
        var srcLine = new BufferLine(10);
        var destLine = new BufferLine(10);
        srcLine[2] = new BufferCell("A", 1, AttributeData.Default);
        srcLine[3] = new BufferCell("B", 1, AttributeData.Default);
        srcLine[4] = new BufferCell("C", 1, AttributeData.Default);

        // Act
        destLine.CopyCellsFrom(srcLine, 2, 5, 3, true);

        // Assert
        destLine[5].Content.Should().Be("A");
        destLine[6].Content.Should().Be("B");
        destLine[7].Content.Should().Be("C");
    }

    [TestMethod]
    public void TranslateToString_ConvertsLineToString()
    {
        // Arrange
        var line = new BufferLine(5);
        line[0] = new BufferCell("H", 1, AttributeData.Default);
        line[1] = new BufferCell("e", 1, AttributeData.Default);
        line[2] = new BufferCell("l", 1, AttributeData.Default);
        line[3] = new BufferCell("l", 1, AttributeData.Default);
        line[4] = new BufferCell("o", 1, AttributeData.Default);

        // Act
        var result = line.TranslateToString();

        // Assert
        result.Should().Be("Hello");
    }

    [TestMethod]
    public void TranslateToString_TrimRight_TrimsWhitespace()
    {
        // Arrange
        var line = new BufferLine(10);
        line[0] = new BufferCell("H", 1, AttributeData.Default);
        line[1] = new BufferCell("i", 1, AttributeData.Default);
        // Rest are null/spaces

        // Act
        var result = line.TranslateToString(trimRight: true);

        // Assert
        result.TrimEnd().Should().Be("Hi");
    }

    [TestMethod]
    public void TranslateToString_WithRange_ConvertsRange()
    {
        // Arrange
        var line = new BufferLine(10);
        line[2] = new BufferCell("A", 1, AttributeData.Default);
        line[3] = new BufferCell("B", 1, AttributeData.Default);
        line[4] = new BufferCell("C", 1, AttributeData.Default);

        // Act
        var result = line.TranslateToString(false, 2, 5);

        // Assert
        result.Should().Contain("ABC");
    }

    [TestMethod]
    public void GetTrimmedLength_ReturnsTrimmedLength()
    {
        // Arrange
        var line = new BufferLine(10);
        line[0] = new BufferCell("T", 1, AttributeData.Default);
        line[1] = new BufferCell("e", 1, AttributeData.Default);
        line[2] = new BufferCell("s", 1, AttributeData.Default);
        line[3] = new BufferCell("t", 1, AttributeData.Default);
        // Rest are whitespace/null

        // Act
        var length = line.GetTrimmedLength();

        // Assert
        length.Should().Be(4);
    }

    [TestMethod]
    public void GetTrimmedLength_EmptyLine_ReturnsZero()
    {
        // Arrange
        var line = new BufferLine(10);

        // Act
        var length = line.GetTrimmedLength();

        // Assert
        length.Should().Be(0);
    }

    [TestMethod]
    public void Clone_CreatesIndependentCopy()
    {
        // Arrange
        var line = new BufferLine(10);
        line[0] = new BufferCell("A", 1, AttributeData.Default);
        line[1] = new BufferCell("B", 1, AttributeData.Default);
        line.IsWrapped = true;

        // Act
        var clone = line.Clone();

        // Assert
        clone.Length.Should().Be(line.Length);
        clone.IsWrapped.Should().Be(line.IsWrapped);
        clone[0].Content.Should().Be("A");
        clone[1].Content.Should().Be("B");
        
        // Verify independence
        clone[0] = new BufferCell("Z", 1, AttributeData.Default);
        line[0].Content.Should().Be("A");
        clone[0].Content.Should().Be("Z");
    }

    [TestMethod]
    public void CopyFrom_CopiesEntireLine()
    {
        // Arrange
        var srcLine = new BufferLine(10);
        srcLine[0] = new BufferCell("X", 1, AttributeData.Default);
        srcLine[1] = new BufferCell("Y", 1, AttributeData.Default);
        srcLine.IsWrapped = true;

        var destLine = new BufferLine(10);

        // Act
        destLine.CopyFrom(srcLine);

        // Assert
        destLine.Length.Should().Be(srcLine.Length);
        destLine.IsWrapped.Should().Be(srcLine.IsWrapped);
        destLine[0].Content.Should().Be("X");
        destLine[1].Content.Should().Be("Y");
    }

    [TestMethod]
    public void CopyFrom_DifferentSize_ResizesAndCopies()
    {
        // Arrange
        var srcLine = new BufferLine(20);
        srcLine[0] = new BufferCell("M", 1, AttributeData.Default);

        var destLine = new BufferLine(10);

        // Act
        destLine.CopyFrom(srcLine);

        // Assert
        destLine.Length.Should().Be(20);
        destLine[0].Content.Should().Be("M");
    }

    [TestMethod]
    public void IsWrapped_CanBeSetAndGet()
    {
        // Arrange
        var line = new BufferLine(10);

        // Act
        line.IsWrapped = true;

        // Assert
        line.IsWrapped.Should().BeTrue();

        // Act
        line.IsWrapped = false;

        // Assert
        line.IsWrapped.Should().BeFalse();
    }

    [TestMethod]
    public void LineAttribute_DefaultsToNormal()
    {
        // Arrange & Act
        var line = new BufferLine(10);

        // Assert
        line.LineAttribute.Should().Be(LineAttribute.Normal);
        line.IsDoubleWidth.Should().BeFalse();
    }

    [TestMethod]
    public void LineAttribute_CanBeSetToDoubleWidth()
    {
        // Arrange
        var line = new BufferLine(10);

        // Act
        line.LineAttribute = LineAttribute.DoubleWidth;

        // Assert
        line.LineAttribute.Should().Be(LineAttribute.DoubleWidth);
        line.IsDoubleWidth.Should().BeTrue();
    }

    [TestMethod]
    public void LineAttribute_DoubleHeightTop_IsDoubleWidth()
    {
        // Arrange
        var line = new BufferLine(10);

        // Act
        line.LineAttribute = LineAttribute.DoubleHeightTop;

        // Assert
        line.LineAttribute.Should().Be(LineAttribute.DoubleHeightTop);
        line.IsDoubleWidth.Should().BeTrue();
    }

    [TestMethod]
    public void LineAttribute_DoubleHeightBottom_IsDoubleWidth()
    {
        // Arrange
        var line = new BufferLine(10);

        // Act
        line.LineAttribute = LineAttribute.DoubleHeightBottom;

        // Assert
        line.LineAttribute.Should().Be(LineAttribute.DoubleHeightBottom);
        line.IsDoubleWidth.Should().BeTrue();
    }

    [TestMethod]
    public void Clone_PreservesLineAttribute()
    {
        // Arrange
        var line = new BufferLine(10);
        line.LineAttribute = LineAttribute.DoubleWidth;

        // Act
        var clone = line.Clone();

        // Assert
        clone.LineAttribute.Should().Be(LineAttribute.DoubleWidth);
    }

    [TestMethod]
    public void CopyFrom_PreservesLineAttribute()
    {
        // Arrange
        var srcLine = new BufferLine(10);
        srcLine.LineAttribute = LineAttribute.DoubleHeightTop;

        var destLine = new BufferLine(10);

        // Act
        destLine.CopyFrom(srcLine);

        // Assert
        destLine.LineAttribute.Should().Be(LineAttribute.DoubleHeightTop);
    }

    [TestMethod]
    public void LineAttribute_SetClearsCache()
    {
        // Arrange
        var line = new BufferLine(10);
        line.Cache = new object();

        // Act
        line.LineAttribute = LineAttribute.DoubleWidth;

        // Assert
        line.Cache.Should().BeNull();
    }
}
