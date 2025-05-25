using System.Diagnostics;
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

    public AllocationMatrix? Solve(out int pivotCount) // tmp out param
    {
        pivotCount = 0;
        sln = bfsSearcher.Search();
        int perturbCount = m + n - 1 - sln.CountBasic();
        if (perturbCount > 0)
        {
            // Deal with degeneracy
            perturbation = new(sln, tp.Cost);
            if (!perturbation.TryPerturb(perturbCount))
                return null;
        }
        PotentialsCalculator pc = new(tp.Cost, sln, RPotential, CPotential);
        CycleSearcher cs = new(sln);
        do
        {
            pc.CalcPotentials();
            Point pnt_min = ArgminNonBasicCostDiffPotential(out double min);
            if (min >= 0)
                break; // sln is optimal
            List<Point>? cycle = cs.SearchClosed(pnt_min);
            Debug.Assert(cycle is not null); // math states that cycle will be always found
            if (cycle is null)
                break;
            pivotCount++;
            Pivot(cycle);
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

    private void Pivot(List<Point> cycle)
    {
        // if min == 0, proceed because it changes list of basic cells
        int min = MinOfCycleAtOddIndexes(cycle);
        int nonBasicMetCount = 0;
        sln[cycle[0]] += new AllocationValue(min);
        sln[cycle[0]] = sln[cycle[0]].ToBasic();
        for (int i = 1; i < cycle.Count; i++)
        {
            if (i % 2 == 0)
                sln[cycle[i]] += new AllocationValue(min);
            else
                sln[cycle[i]] -= new AllocationValue(min);

            // avoid degeneracy
            var current = sln[cycle[i]];
            if (!current.IsBasic)
                if (nonBasicMetCount++ > 0)
                    sln[cycle[i]] = current.ToBasic();
        }
    }

    private int MinOfCycleAtOddIndexes(List<Point> cycle)
    {
        int min = int.MaxValue;
        for (int i = 1; i < cycle.Count; i++)
            if (i % 2 == 1)
            {
                int val = sln[cycle[i]];
                if (val < min)
                    min = val;
            }
        return min;
    }
}
