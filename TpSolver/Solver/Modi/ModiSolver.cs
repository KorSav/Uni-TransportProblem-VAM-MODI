using System.Diagnostics;
using Profiling;
using TpSolver.BfsSearch;
using TpSolver.Perturbation;
using TpSolver.Shared;

namespace TpSolver.Solver.Modi;

public class ModiSolver(TransportProblem tp)
{
    readonly TransportProblem tp = tp;
    readonly int m = tp.Supply.Length;
    readonly double[] RPotential = new double[tp.Supply.Length];
    readonly int n = tp.Demand.Length;
    readonly double[] CPotential = new double[tp.Demand.Length];
    readonly Vam bfsSearcher = new(tp);
    EpsilonPerturbation perturbation = null!;
    AllocationMatrix sln = null!;
    public Profiler Profiler { get; set; } = new(); // TODO: make nullable and measure only when provided

    public AllocationMatrix? Solve(out int pivotCount) // FIXME: see ModiSolverParallel
    {
        using var _ = Profiler.Measure("Total");
        pivotCount = 0;
        sln = bfsSearcher.Search();
        int perturbCount = m + n - 1 - sln.Count(static a => a.IsBasic);
        if (perturbCount > 0)
        {
            // Deal with degeneracy
            perturbation = new(sln, tp.Cost);
            if (!perturbation.TryPerturb(perturbCount))
                return null;
        }
        PotentialsCalculator pc = new(tp.Cost, sln, RPotential, CPotential);
        CycleSearcher cs = new(sln);
        double min;
        Point pnt_min;
        do
        {
            using (Profiler.Measure("Potentials"))
                pc.CalcPotentials();
            using (Profiler.Measure("Argmin"))
                pnt_min = ArgminNonBasicCostDiffPotential(out min);
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
