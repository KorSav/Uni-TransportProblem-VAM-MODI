using TpSolver.Shared;

namespace TpSolver.Tests;

public class AllocationValueTests
{
    [Fact]
    public void AsBasic_ShouldMakeEmptyValueBasic()
    {
        AllocationValue av = new();
        Assert.Equal(0, av);
        Assert.False(av.IsBasic);
        av = av.ToBasic();
        Assert.Equal(0, av);
        Assert.True(av.IsBasic);
    }

    [Fact]
    public void AsBasic_ShouldNotModifyValueIfBasic()
    {
        AllocationValue av = new(1);
        Assert.Equal(1, av);
        Assert.True(av.IsBasic);
        av = av.ToBasic();
        Assert.Equal(1, av);
        Assert.True(av.IsBasic);
    }

    [Fact]
    public void Sum_ShouldTreatEmptyBasicAsZero()
    {
        AllocationValue a = new(10);
        var b = new AllocationValue().ToBasic();
        var s = a + b;
        Assert.Equal(10, s);
        Assert.True(s.IsBasic);
    }

    [Fact]
    public void Diff_ShouldTreatEmptyBasicAsZero()
    {
        AllocationValue a = new(10);
        var b = new AllocationValue().ToBasic();
        var s = a - b;
        Assert.Equal(10, s);
        Assert.True(s.IsBasic);
    }

    [Fact]
    public void Sum_ShouldNotMakeEmptyValueBasic()
    {
        AllocationValue a = new(),
            b = new();
        AllocationValue s = a + b;
        Assert.Equal(0, s);
        Assert.False(s.IsBasic);

        a = b = new AllocationValue().ToBasic();
        s = a + b;
        Assert.Equal(0, s);
        Assert.False(s.IsBasic);
    }

    [Fact]
    public void Diff_ShouldNotMakeEmptyValueBasic()
    {
        AllocationValue a = new(),
            b = new();
        var s = a - b;
        Assert.Equal(0, s);
        Assert.False(s.IsBasic);

        a = b = new AllocationValue().ToBasic();
        s = a - b;
        Assert.Equal(0, s);
        Assert.False(s.IsBasic);
    }

    [Fact]
    public void Diff_ShouldNotAllowNegativeValue()
    {
        AllocationValue a = new(),
            b = new(10);
        Assert.ThrowsAny<Exception>(() => a - b);
    }
}
