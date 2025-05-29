using System.Diagnostics;
using Profiling;
using TpSolver.Shared;

namespace TpSolver.CycleSearch;

public class CycleSearcher(AllocationMatrix allocation)
{
    protected readonly AllocationMatrix allocation = allocation;
    protected readonly HashSet<Point> visited = new(allocation.NRows + allocation.NCols - 1);
    protected List<Point> cycle = [];
    public Profiler? Profiler { get; set; }

    public static class Stages
    {
        public const string Total = nameof(Total);
        public const string GetAdjacent = nameof(GetAdjacent);
    }

    public List<Point>? SearchClosed(Point pnt)
    {
        // Can search only from non basic cell
        Debug.Assert(!allocation[pnt].IsBasic);
        allocation[pnt] = allocation[pnt].ToBasic();
        SearchBasic(pnt);
        allocation[pnt] = new(0);
        return (cycle.Count == 1) ? null : cycle;
    }

    private void SearchBasic(Point aim)
    {
        using var _ = Profiler?.Measure(Stages.Total) ?? Profiler.NoOp();
        Init(aim);
        Point? next;
        Point cur = aim;
        cycle.Add(cur);
        while (aim != (next = GetNewAdjacent()))
        {
            if (next is null)
            {
                if (cycle.Count == 1)
                    return;
                visited.Add(cur);
                cycle.RemoveAt(cycle.Count - 1);
                cur = cycle[^1];
                continue;
            }
            cur = next.Value;
            cycle.Add(cur);
        }
    }

    protected virtual void Init(Point aim)
    {
        visited.Clear();
        cycle.Clear();
    }

    private Point? GetNewAdjacent()
    {
        using var _ = Profiler?.Measure(Stages.GetAdjacent) ?? Profiler.NoOp();
        Debug.Assert(cycle.Count != 0);
        Point cur = cycle[^1];
        if (cycle.Count == 1) // searching adjacent for starting point
            return GetRowNonVisitedFor(cur) ?? GetColNonVisitedFor(cur);

        if (cycle[^2].IRow == cur.IRow) // whether previous was in row
            return GetColNonVisitedFor(cur);
        return GetRowNonVisitedFor(cur);
    }

    protected virtual Point? GetRowNonVisitedFor(Point cur)
    {
        for (int j = 0; j < allocation.NCols; j++)
        {
            Point pnt = new(cur.IRow, j);
            if (allocation[pnt].IsBasic && !visited.Contains(pnt) && j != cur.ICol)
                return pnt;
        }
        return null;
    }

    protected virtual Point? GetColNonVisitedFor(Point cur)
    {
        for (int i = 0; i < allocation.NRows; i++)
        {
            Point pnt = new(i, cur.ICol);
            if (allocation[pnt].IsBasic && !visited.Contains(pnt) && i != cur.IRow)
                return pnt;
        }
        return null;
    }
}
