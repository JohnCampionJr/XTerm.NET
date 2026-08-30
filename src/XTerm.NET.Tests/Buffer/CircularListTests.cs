using XTerm.Buffer;
#pragma warning disable CS8602 // Dereference of a possibly null reference.


namespace XTerm.Tests.Buffer;

[TestClass]

public class CircularListTests
{
    [TestMethod]
    public void Constructor_CreatesEmptyList()
    {
        // Arrange & Act
        var list = new CircularList<BufferLine>(10);

        // Assert
        list.Length.Should().Be(0);
        list.MaxLength.Should().Be(10);
    }

    [TestMethod]
    public void Push_AddsItem()
    {
        // Arrange
        var list = new CircularList<BufferLine>(10);
        var line = new BufferLine(80);

        // Act
        list.Push(line);

        // Assert
        list.Length.Should().Be(1);
        list[0].Should().Equal(line);
    }

    [TestMethod]
    public void Push_MultipleTimes_AddsItems()
    {
        // Arrange
        var list = new CircularList<BufferLine>(10);
        var line1 = new BufferLine(80);
        var line2 = new BufferLine(80);
        var line3 = new BufferLine(80);

        // Act
        list.Push(line1);
        list.Push(line2);
        list.Push(line3);

        // Assert
        list.Length.Should().Be(3);
        list[0].Should().Equal(line1);
        list[1].Should().Equal(line2);
        list[2].Should().Equal(line3);
    }

    [TestMethod]
    public void Push_ExceedMaxLength_OverwritesOldest()
    {
        // Arrange
        var list = new CircularList<BufferLine>(3);
        var line1 = new BufferLine(80);
        var line2 = new BufferLine(80);
        var line3 = new BufferLine(80);
        var line4 = new BufferLine(80);
        
        line1[0] = new BufferCell("1", 1, AttributeData.Default);
        line2[0] = new BufferCell("2", 1, AttributeData.Default);
        line3[0] = new BufferCell("3", 1, AttributeData.Default);
        line4[0] = new BufferCell("4", 1, AttributeData.Default);

        // Act
        list.Push(line1);
        list.Push(line2);
        list.Push(line3);
        list.Push(line4); // Should overwrite line1

        // Assert
        list.Length.Should().Be(3);
        list[0][0].Content.Should().Be("2"); // line1 was overwritten
        list[1][0].Content.Should().Be("3");
        list[2][0].Content.Should().Be("4");
    }

    [TestMethod]
    public void Pop_RemovesAndReturnsLastItem()
    {
        // Arrange
        var list = new CircularList<BufferLine>(10);
        var line1 = new BufferLine(80);
        var line2 = new BufferLine(80);
        list.Push(line1);
        list.Push(line2);

        // Act
        var popped = list.Pop();

        // Assert
        popped.Should().Equal(line2);
        list.Length.Should().Be(1);
    }

    [TestMethod]
    public void Pop_EmptyList_ReturnsNull()
    {
        // Arrange
        var list = new CircularList<BufferLine>(10);

        // Act
        var popped = list.Pop();

        // Assert
        popped.Should().BeNull();
    }

    [TestMethod]
    public void Indexer_Get_ReturnsItem()
    {
        // Arrange
        var list = new CircularList<BufferLine>(10);
        var line = new BufferLine(80);
        list.Push(line);

        // Act
        var retrieved = list[0];

        // Assert
        retrieved.Should().Equal(line);
    }

    [TestMethod]
    public void Indexer_Set_SetsItem()
    {
        // Arrange
        var list = new CircularList<BufferLine>(10);
        var line1 = new BufferLine(80);
        var line2 = new BufferLine(80);
        list.Push(line1);

        // Act
        list[0] = line2;

        // Assert
        list[0].Should().Equal(line2);
    }

    [TestMethod]
    public void Indexer_OutOfBounds_ThrowsException()
    {
        // Arrange
        var list = new CircularList<BufferLine>(10);
        list.Push(new BufferLine(80));

        // Act & Assert
        Assert.ThrowsExactly<IndexOutOfRangeException>(() => list[-1]);
        Assert.ThrowsExactly<IndexOutOfRangeException>(() => list[10]);
    }

    [TestMethod]
    public void Splice_DeleteOnly_RemovesItems()
    {
        // Arrange
        var list = new CircularList<BufferLine>(10);
        var line1 = new BufferLine(80);
        var line2 = new BufferLine(80);
        var line3 = new BufferLine(80);
        
        line1[0] = new BufferCell("1", 1, AttributeData.Default);
        line2[0] = new BufferCell("2", 1, AttributeData.Default);
        line3[0] = new BufferCell("3", 1, AttributeData.Default);
        
        list.Push(line1);
        list.Push(line2);
        list.Push(line3);

        // Act
        list.Splice(1, 1); // Remove line2

        // Assert
        list.Length.Should().Be(2);
        list[0][0].Content.Should().Be("1");
        list[1][0].Content.Should().Be("3");
    }

    [TestMethod]
    public void Splice_InsertOnly_AddsItems()
    {
        // Arrange
        var list = new CircularList<BufferLine>(10);
        var line1 = new BufferLine(80);
        var line2 = new BufferLine(80);
        var lineInsert = new BufferLine(80);
        
        line1[0] = new BufferCell("1", 1, AttributeData.Default);
        line2[0] = new BufferCell("2", 1, AttributeData.Default);
        lineInsert[0] = new BufferCell("X", 1, AttributeData.Default);
        
        list.Push(line1);
        list.Push(line2);

        // Act
        list.Splice(1, 0, lineInsert); // Insert at position 1

        // Assert
        list.Length.Should().Be(3);
        list[0][0].Content.Should().Be("1");
        list[1][0].Content.Should().Be("X");
        list[2][0].Content.Should().Be("2");
    }

    [TestMethod]
    public void Splice_DeleteAndInsert_ReplacesItems()
    {
        // Arrange
        var list = new CircularList<BufferLine>(10);
        var line1 = new BufferLine(80);
        var line2 = new BufferLine(80);
        var line3 = new BufferLine(80);
        var lineNew = new BufferLine(80);
        
        line1[0] = new BufferCell("1", 1, AttributeData.Default);
        line2[0] = new BufferCell("2", 1, AttributeData.Default);
        line3[0] = new BufferCell("3", 1, AttributeData.Default);
        lineNew[0] = new BufferCell("N", 1, AttributeData.Default);
        
        list.Push(line1);
        list.Push(line2);
        list.Push(line3);

        // Act
        list.Splice(1, 1, lineNew); // Replace line2 with lineNew

        // Assert
        list.Length.Should().Be(3);
        list[0][0].Content.Should().Be("1");
        list[1][0].Content.Should().Be("N");
        list[2][0].Content.Should().Be("3");
    }

    [TestMethod]
    public void Splice_OutOfBounds_ThrowsException()
    {
        // Arrange
        var list = new CircularList<BufferLine>(10);
        list.Push(new BufferLine(80));

        // Act & Assert
        Assert.ThrowsExactly<IndexOutOfRangeException>(() => list.Splice(-1, 1));
        Assert.ThrowsExactly<IndexOutOfRangeException>(() => list.Splice(10, 1));
    }

    [TestMethod]
    public void TrimStart_RemovesItemsFromStart()
    {
        // Arrange
        var list = new CircularList<BufferLine>(10);
        var line1 = new BufferLine(80);
        var line2 = new BufferLine(80);
        var line3 = new BufferLine(80);
        
        line1[0] = new BufferCell("1", 1, AttributeData.Default);
        line2[0] = new BufferCell("2", 1, AttributeData.Default);
        line3[0] = new BufferCell("3", 1, AttributeData.Default);
        
        list.Push(line1);
        list.Push(line2);
        list.Push(line3);

        // Act
        list.TrimStart(1);

        // Assert
        list.Length.Should().Be(2);
        list[0][0].Content.Should().Be("2");
        list[1][0].Content.Should().Be("3");
    }

    [TestMethod]
    public void TrimStart_ExceedLength_TrimsToEmpty()
    {
        // Arrange
        var list = new CircularList<BufferLine>(10);
        list.Push(new BufferLine(80));
        list.Push(new BufferLine(80));

        // Act
        list.TrimStart(5);

        // Assert
        list.Length.Should().Be(0);
    }

    [TestMethod]
    public void TrimStart_NegativeCount_DoesNothing()
    {
        // Arrange
        var list = new CircularList<BufferLine>(10);
        list.Push(new BufferLine(80));
        list.Push(new BufferLine(80));

        // Act
        list.TrimStart(-1);

        // Assert
        list.Length.Should().Be(2);
    }

    [TestMethod]
    public void ShiftElements_Right_ShiftsElements()
    {
        // Arrange
        var list = new CircularList<BufferLine>(10);
        var line1 = new BufferLine(80);
        var line2 = new BufferLine(80);
        var line3 = new BufferLine(80);
        var line4 = new BufferLine(80);
        
        line1[0] = new BufferCell("1", 1, AttributeData.Default);
        line2[0] = new BufferCell("2", 1, AttributeData.Default);
        line3[0] = new BufferCell("3", 1, AttributeData.Default);
        
        list.Push(line1);
        list.Push(line2);
        list.Push(line3);
        list.Push(line4);

        // Act
        list.ShiftElements(1, 2, 1); // Shift 2 elements starting at index 1, right by 1

        // Assert
        list[2][0].Content.Should().Be("2");
        list[3][0].Content.Should().Be("3");
    }

    [TestMethod]
    public void ShiftElements_Left_ShiftsElements()
    {
        // Arrange
        var list = new CircularList<BufferLine>(10);
        var line1 = new BufferLine(80);
        var line2 = new BufferLine(80);
        var line3 = new BufferLine(80);
        
        line1[0] = new BufferCell("1", 1, AttributeData.Default);
        line2[0] = new BufferCell("2", 1, AttributeData.Default);
        line3[0] = new BufferCell("3", 1, AttributeData.Default);
        
        list.Push(line1);
        list.Push(line2);
        list.Push(line3);

        // Act
        list.ShiftElements(1, 2, -1); // Shift 2 elements starting at index 1, left by 1

        // Assert
        list[0][0].Content.Should().Be("2");
        list[1][0].Content.Should().Be("3");
    }

    [TestMethod]
    public void Recycle_AtMaxCapacity_ReturnsPoppedItem()
    {
        // Arrange
        var list = new CircularList<BufferLine>(3);
        var line1 = new BufferLine(80);
        var line2 = new BufferLine(80);
        var line3 = new BufferLine(80);
        
        list.Push(line1);
        list.Push(line2);
        list.Push(line3);

        // Act
        var recycled = list.Recycle();

        // Assert
        recycled.Should().NotBeNull();
        list.Length.Should().Be(2);
    }

    [TestMethod]
    public void Recycle_BelowMaxCapacity_ReturnsNull()
    {
        // Arrange
        var list = new CircularList<BufferLine>(10);
        list.Push(new BufferLine(80));

        // Act
        var recycled = list.Recycle();

        // Assert
        recycled.Should().BeNull();
    }

    [TestMethod]
    public void Clear_RemovesAllItems()
    {
        // Arrange
        var list = new CircularList<BufferLine>(10);
        list.Push(new BufferLine(80));
        list.Push(new BufferLine(80));
        list.Push(new BufferLine(80));

        // Act
        list.Clear();

        // Assert
        list.Length.Should().Be(0);
    }

    [TestMethod]
    public void Resize_Expand_IncreasesMaxLength()
    {
        // Arrange
        var list = new CircularList<BufferLine>(5);
        list.Push(new BufferLine(80));
        list.Push(new BufferLine(80));

        // Act
        list.Resize(10);

        // Assert
        list.MaxLength.Should().Be(10);
        list.Length.Should().Be(2);
    }

    [TestMethod]
    public void Resize_Shrink_DecreasesMaxLength()
    {
        // Arrange
        var list = new CircularList<BufferLine>(10);
        var line1 = new BufferLine(80);
        var line2 = new BufferLine(80);
        var line3 = new BufferLine(80);
        
        line1[0] = new BufferCell("1", 1, AttributeData.Default);
        line2[0] = new BufferCell("2", 1, AttributeData.Default);
        line3[0] = new BufferCell("3", 1, AttributeData.Default);
        
        list.Push(line1);
        list.Push(line2);
        list.Push(line3);

        // Act
        list.Resize(2);

        // Assert
        list.MaxLength.Should().Be(2);
        list.Length.Should().Be(2);
        list[0][0].Content.Should().Be("1");
        list[1][0].Content.Should().Be("2");
    }

    [TestMethod]
    public void Resize_SameSize_DoesNothing()
    {
        // Arrange
        var list = new CircularList<BufferLine>(10);
        list.Push(new BufferLine(80));

        // Act
        list.Resize(10);

        // Assert
        list.MaxLength.Should().Be(10);
        list.Length.Should().Be(1);
    }

    [TestMethod]
    public void GetItems_ReturnsAllItems()
    {
        // Arrange
        var list = new CircularList<BufferLine>(10);
        var line1 = new BufferLine(80);
        var line2 = new BufferLine(80);
        var line3 = new BufferLine(80);
        
        list.Push(line1);
        list.Push(line2);
        list.Push(line3);

        // Act
        var items = list.GetItems().ToList();

        // Assert
        items.Count.Should().Be(3);
        items[0].Should().Equal(line1);
        items[1].Should().Equal(line2);
        items[2].Should().Equal(line3);
    }

    [TestMethod]
    public void GetItems_EmptyList_ReturnsEmptyEnumerable()
    {
        // Arrange
        var list = new CircularList<BufferLine>(10);

        // Act
        var items = list.GetItems().ToList();

        // Assert
        items.Should().BeEmpty();
    }

    [TestMethod]
    public void CircularBehavior_MaintainsOrder()
    {
        // Arrange
        var list = new CircularList<BufferLine>(5);
        var lines = new List<BufferLine>();
        
        for (int i = 0; i < 7; i++)
        {
            var line = new BufferLine(80);
            line[0] = new BufferCell(i.ToString(), 1, AttributeData.Default);
            lines.Add(line);
            list.Push(line);
        }

        // Act & Assert
        // After pushing 7 items into a list with max 5, oldest 2 should be overwritten
        list.Length.Should().Be(5);
        list[0][0].Content.Should().Be("2"); // Items 0 and 1 were overwritten
        list[1][0].Content.Should().Be("3");
        list[2][0].Content.Should().Be("4");
        list[3][0].Content.Should().Be("5");
        list[4][0].Content.Should().Be("6");
    }
}
