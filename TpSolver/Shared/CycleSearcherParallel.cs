using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Diagnostics;
using Profiling;

namespace TpSolver.Shared;

public class CycleSearcherParallel
{
    private readonly struct PntParentPair(Point cur, Point? parent)
    {
        public readonly Point Cur = cur;
        public readonly Point? Parent = parent;
    }

    private class PointAdjacents(HashSet<Point> inRow, HashSet<Point> inCol) : IEnumerable<Point>
    {
        public readonly HashSet<Point> RowAdjacents = inRow;
        public readonly HashSet<Point> ColAdjacents = inCol;

        public IEnumerator<Point> GetEnumerator()
        {
            foreach (var pnt in RowAdjacents)
                yield return pnt;
            foreach (var pnt in ColAdjacents)
                yield return pnt;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    private readonly AllocationMatrix allocation;
    private readonly List<Point> cycle;
    private readonly int parDeg;
    private readonly int workersCount;

    private readonly HashSet<Point> visited = []; // used only by main

    private ConcurrentDictionary<Point, PointAdjacents> nonBasicAdjacents;
    private readonly ConcurrentDictionary<Point, byte> consideredPoints;
    private ConcurrentQueue<PntParentPair> ptsToConsider;
    private SemaphoreSlim semaphore = new(1);
    private volatile int toBeProcessed;

    public Profiler Profiler { get; }

    public CycleSearcherParallel(AllocationMatrix allocation, ParallelOptions parallelOptions)
    {
        this.allocation = allocation;
        parDeg = parallelOptions.MaxDegreeOfParallelism;
        nonBasicAdjacents = new(parDeg, allocation.NRows + allocation.NCols - 1);
        ptsToConsider = new();
        workersCount = parDeg - 1;
        cycle = [];
        Profiler = new();
        consideredPoints = new();
    }

    public List<Point>? SearchClosed(Point pnt)
    {
        // Can search only from non basic cell
        Debug.Assert(!allocation[pnt].IsBasic);
        allocation[pnt] = allocation[pnt].ToBasic();
        SearchBasic(pnt);
        allocation[pnt] = new(0);
        if (cycle.Count == 1)
        {
            Debug.Assert(cycle[0] == pnt);
            return null;
        }
        return cycle;
    }

    private void PrecalculateAdjacentsInParallel(Point aim)
    {
        visited.Clear();
        cycle.Clear();
        consideredPoints.Clear();
        nonBasicAdjacents.Clear();
        semaphore = new(1);

        ptsToConsider = new([new(aim, null)]);

        toBeProcessed = 1;
        Task[] tasks = new Task[workersCount];
        {
            for (int i = 0; i < workersCount; i++)
                tasks[i] = Task.Run(WorkerSearchAdjacentFromQueue);
        }
        Task.WaitAll(tasks);
    }

    private void SearchBasic(Point aim)
    {
        using var _ = Profiler.Measure("Total");
        PrecalculateAdjacentsInParallel(aim);
        Point? next;
        Point cur = aim;
        cycle.Add(cur);

        using (Profiler.Measure("Backtrack"))
        {
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
    }

    private Point? GetNewAdjacent()
    {
        Debug.Assert(cycle.Count != 0);
        Point cur = cycle[^1];
        if (cycle.Count == 1) // searching adjacent for starting point
            return GetRowNonVisitedFor(cur) ?? GetColNonVisitedFor(cur);

        if (cycle[^2].IRow == cur.IRow) // whether previous was in row
            return GetColNonVisitedFor(cur);
        return GetRowNonVisitedFor(cur);
    }

    private Point? GetColNonVisitedFor(Point cur)
    {
        PointAdjacents? adjacents;
        SpinWait sw = new();
        // If used in parallel with workers (slower)
        while (!nonBasicAdjacents.TryGetValue(cur, out adjacents))
            sw.SpinOnce();
        foreach (var adjacent in adjacents.ColAdjacents)
        {
            if (!visited.Contains(adjacent))
                return adjacent;
        }
        return null;
    }

    private Point? GetRowNonVisitedFor(Point cur)
    {
        PointAdjacents? adjacents;
        SpinWait sw = new();
        // If used in parallel with workers (slower)
        while (!nonBasicAdjacents.TryGetValue(cur, out adjacents))
            sw.SpinOnce();
        foreach (var adjacent in adjacents.RowAdjacents)
        {
            if (!visited.Contains(adjacent))
                return adjacent;
        }
        return null;
    }

    private void WorkerSearchAdjacentFromQueue()
    {
        while (toBeProcessed != 0)
        {
            semaphore.Wait();
            if (!ptsToConsider.TryDequeue(out PntParentPair queueItem))
            { // spurious wake-up or race condition
                continue;
            }
            if (!consideredPoints.TryAdd(queueItem.Cur, 0))
            {
                Interlocked.Decrement(ref toBeProcessed);
                continue;
            }
            var adjInRow = GetAllNonBasicInRow(queueItem.Cur).ToHashSet();
            var adjInCol = GetAllNonBasicInCol(queueItem.Cur).ToHashSet();
            PointAdjacents adjacents = new(adjInRow, adjInCol);
            if (!nonBasicAdjacents.TryAdd(queueItem.Cur, adjacents))
            {
                Interlocked.Decrement(ref toBeProcessed);
                continue;
            }
            foreach (var next in adjacents)
            {
                if (consideredPoints.ContainsKey(next))
                    continue; // avoid cyclic adjacent search
                ptsToConsider.Enqueue(new(next, queueItem.Cur));
                Interlocked.Increment(ref toBeProcessed);
                semaphore.Release();
            }
            Interlocked.Decrement(ref toBeProcessed);
        }
        semaphore.Release(workersCount); // should release all waiting workers, after race condition in while clause
    }

    private List<Point> GetAllNonBasicInRow(Point cur)
    {
        List<Point> pts = new(allocation.NCols);
        for (int j = 0; j < allocation.NCols; j++)
        {
            Point pnt = new(cur.IRow, j);
            if (allocation[pnt].IsBasic && j != cur.ICol)
                pts.Add(pnt);
        }
        return pts;
    }

    private List<Point> GetAllNonBasicInCol(Point cur)
    {
        List<Point> pts = new(allocation.NRows);
        for (int i = 0; i < allocation.NRows; i++)
        {
            Point pnt = new(i, cur.ICol);
            if (allocation[pnt].IsBasic && i != cur.IRow)
                pts.Add(pnt);
        }
        return pts;
    }
}
