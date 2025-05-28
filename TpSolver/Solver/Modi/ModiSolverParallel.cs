using System.Diagnostics;
using Profiling;
using TpSolver.BfsSearch;
using TpSolver.Perturbation;
using TpSolver.Shared;

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
        do
        {
            using (Profiler.Measure("Potentials"))
                pc.CalcPotentials();
            Point pnt_min = ArgminNonBasicCostDiffPotential(out double min);
            if (min >= 0)
                break; // sln is optimal
            List<Point>? cycle = cs.SearchClosed(pnt_min);
            Debug.Assert(cycle is not null); // math states that cycle will be always found
            if (cycle is null)
                break;
            pivotCount++;
            sln.Pivot(cycle);
        } while (true);
        return sln;
    }

    private Point ArgminNonBasicCostDiffPotential(out double min)
    {
        min = double.PositiveInfinity;
        Point pnt = new(-1, -1);
        for (int i = 0; i < m; i++)
        for (int j = 0; j < n; j++)
        {
            double costDiffPotential = tp.Cost[i, j] - RPotential[i] - CPotential[j];
            if (!sln[i, j].IsBasic && costDiffPotential < min)
            {
                min = costDiffPotential;
                pnt = new(i, j);
            }
        }
        return pnt;
    }
}
