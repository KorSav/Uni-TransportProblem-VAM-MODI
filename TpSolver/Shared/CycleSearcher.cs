using System.Diagnostics;
using Profiling;

namespace TpSolver.Shared;

class CycleSearcher
{
    private readonly AllocationMatrix allocation;
    private readonly Matrix<bool> visited;
    private List<Point> cycle = null!;
    public Profiler Profiler { get; }

    public CycleSearcher(AllocationMatrix allocation)
    {
        this.allocation = allocation;
        visited = new bool[allocation.NRows, allocation.NCols];
        Profiler = new();
    }

    public CycleSearcher(int[,] allocation)
        : this(new AllocationMatrix(allocation)) { }

    public List<Point>? SearchClosed(Point pnt)
    {
        // Can search only from non basic cell
        Debug.Assert(!allocation[pnt].IsBasic);
        List<Point>? cycle;
        allocation[pnt] = allocation[pnt].ToBasic();
        cycle = SearchBasic(pnt);
        allocation[pnt] = new(0);
        return cycle;
    }

    private List<Point>? SearchBasic(Point aim)
    {
        using var _ = Profiler.Measure("Total");
        visited.Fill(p => false);
        cycle = new(allocation.NRows + allocation.NCols - 1);
        Point? next;
        Point cur = aim;
        cycle.Add(cur);
        while (aim != (next = GetNewAdjacent()))
        {
            if (next is null)
            {
                if (cycle.Count == 1)
                    return null;
                visited[cur.IRow, cur.ICol] = true;
                cycle.RemoveAt(cycle.Count - 1);
                cur = cycle[^1];
                continue;
            }
            cur = next.Value;
            cycle.Add(cur);
        }
        return cycle;
    }

    private Point? GetNewAdjacent()
    {
        Debug.Assert(cycle.Count != 0);
        Point cur = cycle[^1];
        if (cycle.Count == 1) // searching adjacent for starting point
            return GetNonVisitedInRow(cur.IRow, filter: cur.ICol)
                ?? GetNonVisitedInColumn(cur.ICol, filter: cur.IRow);

        if (cycle[^2].IRow == cur.IRow) // whether previous was in row
            return GetNonVisitedInColumn(cur.ICol, filter: cur.IRow);
        return GetNonVisitedInRow(cur.IRow, filter: cur.ICol);
    }

    private Point? GetNonVisitedInRow(int i, int filter)
    {
        using var _ = Profiler.Measure("Searching allocation");
        for (int j = 0; j < allocation.NCols; j++)
            if (allocation[i, j].IsBasic && !visited[i, j] && j != filter)
                return new(i, j);
        return null;
    }

    private Point? GetNonVisitedInColumn(int j, int filter)
    {
        using var _ = Profiler.Measure("Searching allocation");
        for (int i = 0; i < allocation.NRows; i++)
            if (allocation[i, j].IsBasic && !visited[i, j] && i != filter)
                return new(i, j);
        return null;
    }
}
