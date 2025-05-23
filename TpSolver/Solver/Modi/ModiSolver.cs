using System.Diagnostics;
using TpSolver.BfsSearch;
using TpSolver.Perturbation;
using TpSolver.Shared;
using TpSolver.Solver.Modi;

namespace TpSolver.Solver;

public class ModiSolver(double[,] cost, int[] supply, int[] demand)
{
    readonly double[,] cost = cost;
    readonly int m = supply.Length;
    readonly double[] CPotential = new double[supply.Length];
    readonly int n = demand.Length;
    readonly double[] RPotential = new double[demand.Length];
    readonly Vam bfsSearcher = new(cost, supply, demand);
    EpsilonPerturbation perturbation = null!;
    AllocationMatrix sln = null!;

    public AllocationMatrix? Solve()
    {
        sln = bfsSearcher.Search();
        int perturbCount = m + n - 1 - sln.CountBasic();
        if (perturbCount > 0)
        {
            // Deal with degeneracy
            perturbation = new(sln, cost);
            if (!perturbation.TryPerturb(perturbCount))
                return null;
        }
        PotentialsCalculator pc = new(cost, sln, RPotential, CPotential);
        do
        {
            pc.CalcPotentials();
            Point pnt_min = ArgminNonBasicCostDiffPotential();
            if (cost[pnt_min.i, pnt_min.j] >= 0)
                break; // sln is optimal
            // find cycle
            // find min value to remove
            // pivot
        } while (true);
        return sln;
    }

    private Point ArgminNonBasicCostDiffPotential()
    {
        double min = double.PositiveInfinity;
        Point pnt = new(-1, -1);
        for (int i = 0; i < m; i++)
        for (int j = 0; j < n; j++)
        {
            double costDiffPotential = cost[i, j] - RPotential[i] - CPotential[j];
            if (!sln[i, j].IsBasic && costDiffPotential < min)
            {
                min = costDiffPotential;
                pnt = new(i, j);
            }
        }
        return pnt;
    }
}
