using System.Collections;
using System.Collections.Concurrent;
using TpSolver.Shared;

namespace TpSolver.CycleSearch;

public class CycleSearcherParallel : CycleSearcher
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

        public IEnumerator<Point> GetEnumerator() =>
            RowAdjacents.Concat(ColAdjacents).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    private readonly int parDeg;
    private int workersCount => parDeg;

    private readonly ConcurrentDictionary<Point, PointAdjacents> nonBasicAdjacents;
    private readonly ConcurrentDictionary<Point, byte> consideredPoints;
    private ConcurrentQueue<PntParentPair> ptsToConsider;
    private SemaphoreSlim semaphore = new(1);
    private volatile int toBeProcessed;

    public CycleSearcherParallel(AllocationMatrix allocation, int parallelizationDegree)
        : base(allocation)
    {
        parDeg = parallelizationDegree;
        nonBasicAdjacents = new(parDeg, allocation.NRows + allocation.NCols - 1);
        ptsToConsider = new();
        consideredPoints = new();
    }

    protected override Point? GetColNonVisitedFor(Point cur) => GetNonVisitedFor(cur, inRow: false);

    protected override Point? GetRowNonVisitedFor(Point cur) => GetNonVisitedFor(cur, inRow: true);

    protected override void Init(Point aim)
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

    private Point? GetNonVisitedFor(Point cur, bool inRow)
    {
        PointAdjacents? adjacents;
        SpinWait sw = new();
        // If used in parallel with workers (slower)
        while (!nonBasicAdjacents.TryGetValue(cur, out adjacents))
            sw.SpinOnce();
        foreach (var adjacent in inRow ? adjacents.RowAdjacents : adjacents.ColAdjacents)
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
            { // go here on termination
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
            foreach (var next in adjacents.RowAdjacents.Concat(adjacents))
            {
                if (consideredPoints.ContainsKey(next))
                    continue; // avoid cyclic adjacent search
                ptsToConsider.Enqueue(new(next, queueItem.Cur));
                Interlocked.Increment(ref toBeProcessed);
                semaphore.Release();
            }
            Interlocked.Decrement(ref toBeProcessed);
        }
        semaphore.Release(workersCount); // should release all waiting workers on termination
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
