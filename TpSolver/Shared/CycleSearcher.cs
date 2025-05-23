using System.Diagnostics;

namespace TpSolver.Shared;

record struct Point(int i, int j);

class CycleSearcher
{
    private readonly int[,] allocations;
    private readonly int m;
    private readonly int n;
    private readonly bool[,] visited;
    private List<Point> cycle = null!;

    private enum Move { }

    public CycleSearcher(int[,] allocations)
    {
        this.allocations = allocations;
        m = allocations.GetLength(0);
        n = allocations.GetLength(1);
        visited = new bool[m, n];
    }

    public List<Point>? SearchClosed(int i, int j)
    {
        // Can search only from non basic cell
        Debug.Assert(allocations[i, j] == 0);
        List<Point>? cycle;
        // Make point basic and try to find cycle of all basic cells
        allocations[i, j] = int.MaxValue;
        cycle = SearchBasic(i, j);
        allocations[i, j] = 0;
        return cycle;
    }

    private List<Point>? SearchBasic(int i, int j)
    {
        MakeAllNonvisited();
        cycle = new(m + n - 1);
        Point aim = new(i, j);
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
        for (int j = 0; j < n; j++)
            if (IsBasicNonVisited(i, j) && j != filter)
                return new(i, j);
        return null;
    }

    private Point? GetNonVisitedInColumn(int j, int filter)
    {
        for (int i = 0; i < m; i++)
            if (IsBasicNonVisited(i, j) && i != filter)
                return new(i, j);
        return null;
    }

    private bool IsBasicNonVisited(int i, int j)
    {
        return allocations[i, j] != 0 && !visited[i, j];
    }
}
