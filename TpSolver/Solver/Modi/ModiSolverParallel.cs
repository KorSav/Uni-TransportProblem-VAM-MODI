using System.Diagnostics;
using Profiling;
using TpSolver.BfsSearch;
using TpSolver.Perturbation;
using TpSolver.Shared;
using TpSolver.Utils;

namespace TpSolver.Solver.Modi;

public class ModiSolverParallel(TransportProblem tp, ParallelOptions parallelOptions)
{
    readonly TransportProblem tp = tp;
    readonly int m = tp.Supply.Length;
    readonly double[] RPotential = new double[tp.Supply.Length];
    readonly int n = tp.Demand.Length;
    readonly double[] CPotential = new double[tp.Demand.Length];
    readonly VamParallel bfsSearcher = new(tp, parallelOptions);
    EpsilonPerturbationParallel perturbation = null!;
    AllocationMatrix sln = null!;
    readonly ParallelOptions parOpts = parallelOptions;
    public Profiler Profiler { get; private init; } = new(); // TODO: make a substage, and give to inner algorithms as profiler to write in

    public AllocationMatrix? Solve(out int pivotCount) // FIXME: profiler responsibility
    {
        using var _ = Profiler.Measure("Total");
        pivotCount = 0;
        sln = bfsSearcher.Search();
        int perturbCount = m + n - 1 - sln.Count(static a => a.IsBasic);
        if (perturbCount > 0)
        {
            // Deal with degeneracy
            perturbation = new(sln, tp.Cost, parOpts);
            if (!perturbation.TryPerturb(perturbCount))
                return null;
        }
        PotentialsCalculatorParallel pc = new(
            tp.Cost,
            sln,
            RPotential,
            CPotential,
            parOpts.MaxDegreeOfParallelism
        );
        CycleSearcher cs = new(sln);
        PntDiffPotential min;
        do
        {
            using (Profiler.Measure("Potentials"))
                pc.CalcPotentials();
            using (Profiler.Measure("Argmin"))
                min = ArgminNonBasicCostDiffPotential();
            if (min.diff >= 0)
                break; // sln is optimal
            List<Point>? cycle = cs.SearchClosed(min.pnt);
            Debug.Assert(cycle is not null); // math states that cycle will be always found
            if (cycle is null)
                break;
            pivotCount++;
            sln.Pivot(cycle);
        } while (true);
        return sln;
    }

    private PntDiffPotential ArgminNonBasicCostDiffPotential()
    {
        PntDiffPotential globalMin;
        var tasks = new Task<PntDiffPotential?>[parOpts.MaxDegreeOfParallelism];
        if (!tasks.TryRunDistributedRange(0, m * n, ArgminNonBasicCostDiffPotentialInRange))
            return ArgminNonBasicCostDiffPotentialInRange(0, m * n) ?? PntDiffPotential.MaxValue; // should never be null
        Task.WaitAll(tasks);
        globalMin = PntDiffPotential.MaxValue;
        foreach (var task in tasks)
        {
            PntDiffPotential? localMin = task.Result;
            if (localMin is null)
                continue;
            if (localMin < globalMin)
                globalMin = localMin.Value;
        }
        // The problem assures that globalMin will be always found
        return globalMin;
    }

    private PntDiffPotential? ArgminNonBasicCostDiffPotentialInRange(int iFrom, int iTo)
    {
        double min = double.PositiveInfinity;
        Point? pntMin = null;
        for (int compound = iFrom; compound < iTo; compound++)
        {
            int i = compound / n;
            int j = compound % n;
            double costDiffPotential = tp.Cost[i, j] - RPotential[i] - CPotential[j];
            if (!sln[i, j].IsBasic && costDiffPotential < min)
            {
                min = costDiffPotential;
                pntMin = new(i, j);
            }
        }
        if (pntMin is null)
            return null;
        return new(pntMin.Value, min);
    }

    private readonly struct PntDiffPotential(Point pnt, double diff)
    {
        public readonly Point pnt = pnt;
        public readonly double diff = diff;

        public static PntDiffPotential MaxValue =>
            new(new Point(int.MaxValue, int.MaxValue), double.PositiveInfinity);

        public static bool operator <(PntDiffPotential a, PntDiffPotential b) =>
            a.diff < b.diff || (a.diff == b.diff && a.pnt < b.pnt);

        public static bool operator >(PntDiffPotential a, PntDiffPotential b) => b < a;
    }
}
