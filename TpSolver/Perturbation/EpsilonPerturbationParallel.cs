using System.Diagnostics;
using Profiling;
using TpSolver.CycleSearch;
using TpSolver.Shared;

namespace TpSolver.Perturbation;

class EpsilonPerturbationParallel
{
    private readonly AllocationMatrix allocation;
    private readonly Matrix<double> cost;
    private readonly CycleSearcherParallel cs;
    private readonly Matrix<bool> toCheck;
    private readonly int m;
    private readonly int n;
    private readonly int parDeg;
    private readonly Point?[] localMins;

    public Profiler CycleProfiler { get; }
    public Profiler Profiler { get; }

    public EpsilonPerturbationParallel(
        AllocationMatrix allocation,
        Matrix<double> cost,
        int parallelizationDegree
    )
    {
        this.allocation = allocation;
        parDeg = parallelizationDegree;
        m = allocation.NRows;
        n = allocation.NCols;
        CycleProfiler = new();
        cs = new(this.allocation, parDeg) { Profiler = CycleProfiler };
        this.cost = cost;
        toCheck = new bool[m, n];
        localMins = new Point?[parDeg];
        Profiler = new();
    }

    public bool TryPerturb(int pntCount)
    {
        Debug.Assert(pntCount > 0);
        if (pntCount <= 0)
            return false;

        toCheck.Fill((p) => !allocation[p].IsBasic);
        Point? pntMin;
        List<Point>? cycle = null;
        int perturbedCount = 0;
        int cyclesFound = 0;
        for (; perturbedCount < pntCount; perturbedCount++)
        {
            do
            {
                using (Profiler.Measure("par, argmin"))
                    pntMin = ArgminCostInCheckList();
                if (pntMin is null) // all non basic cells were tried
                    return false;
                cycle = cs.SearchClosed(pntMin.Value);
                if (cycle is not null)
                { // dont check points that are known to have cycle
                    toCheck[pntMin.Value] = false;
                    cyclesFound++;
                }
            } while (cycle is not null);
            allocation[pntMin.Value] = allocation[pntMin.Value].ToBasic();
            toCheck[pntMin.Value] = false;
        }
        return true;
    }

    private Point? ArgminCostInCheckList()
    {
        Task[] tasks = new Task[parDeg];
        int baseChunkSize = m * n / tasks.Length;
        int residue = m * n % tasks.Length;
        int curI = 0;
        for (int i = 0; i < tasks.Length; i++)
        {
            int chunkSize = baseChunkSize + (i < residue ? 1 : 0);
            int iFrom = curI;
            int iTo = curI + chunkSize;
            curI = iTo;
            int taskIndex = i;
            tasks[i] = Task.Run(() => UpdateArgminInRange(taskIndex, iFrom, iTo));
        }
        Task.WaitAll(tasks);
        Point? globalMin = localMins[0];
        for (int i = 0; i < localMins.Length; i++)
        {
            if (localMins[i] is null)
                continue;
            if (globalMin is null)
            {
                globalMin = localMins[i];
                continue;
            }
            if (
                cost[localMins[i]!.Value] < cost[globalMin.Value]
                || (cost[localMins[i]!.Value] == cost[globalMin.Value] && localMins[i] < globalMin)
            )
                globalMin = localMins[i];
        }
        return globalMin;
    }

    private void UpdateArgminInRange(int iLocalMin, int fromInc, int toExc)
    {
        Point? min = null;
        double costMin = double.PositiveInfinity;
        for (int compound = fromInc + 1; compound < toExc; compound++)
        {
            int i = compound / n;
            int j = compound % n;
            double cost = this.cost[i, j];
            if (toCheck[i, j] && cost < costMin)
            {
                costMin = cost;
                min = new(i, j);
            }
        }
        localMins[iLocalMin] = min;
    }
}
