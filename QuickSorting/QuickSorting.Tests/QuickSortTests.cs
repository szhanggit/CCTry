using QuickSorting;

namespace QuickSorting.Tests;

public class QuickSortTests
{
    private readonly ISort _sorter = new QuickSort();

    [Fact]
    public void Sort_AlreadySorted_ReturnsSameOrder()
    {
        var result = _sorter.Sort([1, 2, 3, 4, 5]);
        Assert.Equal([1, 2, 3, 4, 5], result);
    }

    [Fact]
    public void Sort_ReverseSorted_ReturnsAscending()
    {
        var result = _sorter.Sort([5, 4, 3, 2, 1]);
        Assert.Equal([1, 2, 3, 4, 5], result);
    }

    [Fact]
    public void Sort_RandomOrder_ReturnsSorted()
    {
        var result = _sorter.Sort([3, 6, 8, 10, 1, 2, 1]);
        Assert.Equal([1, 1, 2, 3, 6, 8, 10], result);
    }

    [Fact]
    public void Sort_AllDuplicates_ReturnsSameValues()
    {
        var result = _sorter.Sort([4, 4, 4, 4]);
        Assert.Equal([4, 4, 4, 4], result);
    }

    [Fact]
    public void Sort_SingleElement_ReturnsSingleElement()
    {
        var result = _sorter.Sort([42]);
        Assert.Equal([42], result);
    }

    [Fact]
    public void Sort_EmptyArray_ReturnsEmptyArray()
    {
        var result = _sorter.Sort([]);
        Assert.Empty(result);
    }

    [Fact]
    public void Sort_NegativeNumbers_ReturnsSorted()
    {
        var result = _sorter.Sort([-3, 1, -1, 0, 2]);
        Assert.Equal([-3, -1, 0, 1, 2], result);
    }

    [Fact]
    public void Sort_DoesNotMutateOriginalArray()
    {
        int[] original = [3, 1, 2];
        _sorter.Sort(original);
        Assert.Equal([3, 1, 2], original);
    }
}
