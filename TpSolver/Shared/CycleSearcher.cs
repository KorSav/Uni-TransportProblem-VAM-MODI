using System.Diagnostics;

namespace TpSolver.Shared;

class CycleSearcher
{
    private readonly AllocationMatrix allocation;
    private readonly bool[,] visited;
    private List<Point> cycle = null!;

    private enum Move { }

    public CycleSearcher(AllocationMatrix allocation)
    {
        this.allocation = allocation;
        visited = new bool[allocation.NRows, allocation.NCols];
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
        MakeAllNonvisited();
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
                visited[cur.i, cur.j] = true;
                cycle.RemoveAt(cycle.Count - 1);
                cur = cycle[^1];
                continue;
            }
            cur = next.Value;
            cycle.Add(cur);
        }
        return cycle;
    }

    private void MakeAllNonvisited()
    {
        for (int i = 0; i < visited.GetLength(0); i++)
        for (int j = 0; j < visited.GetLength(1); j++)
        {
            visited[i, j] = false;
        }
    }

    private Point? GetNewAdjacent()
    {
        Debug.Assert(cycle.Count != 0);
        Point cur = cycle[^1];
        if (cycle.Count > 1) // searching adjacent not for initial aim
            return (cycle[^2].i == cur.i) switch // whether previous was in row
            {
                true => GetNonVisitedInColumn(cur.j, filter: cur.i),
                false => GetNonVisitedInRow(cur.i, filter: cur.j),
            };
        return GetNonVisitedInRow(cur.i, filter: cur.j)
            ?? GetNonVisitedInColumn(cur.j, filter: cur.i);
    }

    private Point? GetNonVisitedInRow(int i, int filter)
    {
        for (int j = 0; j < allocation.NCols; j++)
            if (allocation[i, j].IsBasic && !visited[i, j] && j != filter)
                return new(i, j);
        return null;
    }

    private Point? GetNonVisitedInColumn(int j, int filter)
    {
        for (int i = 0; i < allocation.NRows; i++)
            if (allocation[i, j].IsBasic && !visited[i, j] && i != filter)
                return new(i, j);
        return null;
    }
}
