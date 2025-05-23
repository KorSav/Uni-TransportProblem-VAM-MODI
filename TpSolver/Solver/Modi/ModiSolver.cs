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
        bool isOptimal = false;
        PotentialsCalculator pc = new(cost, sln, RPotential, CPotential);
        do
        {
            pc.CalcPotentials();
            // if optimal - done
            // find min
            // cycle
        } while (!isOptimal);
        return sln;
    }
}
