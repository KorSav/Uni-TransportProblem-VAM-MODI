using TpSolver.BfsSearch;
using TpSolver.CycleSearch;
using TpSolver.Perturbation;
using TpSolver.Shared;
using TpSolver.Solver.Modi.PotentialsCalculator;

namespace TpSolver.Solver.Modi;

public class ModiSolver(TransportProblem tp) : ModiSolverBase(tp)
{
    protected override PntDiffPotential MinDiffNBCostPotential()
    {
        PntDiffPotential min = PntDiffPotential.MaxValue;
        for (int i = 0; i < m; i++)
        for (int j = 0; j < n; j++)
            if (!sln[i, j].IsBasic)
            {
                PntDiffPotential val = new(
                    new(i, j),
                    tp.Cost[i, j] - RPotential[i] - CPotential[j]
                );
                min = (val < min) ? val : min;
            }
        return min;
    }

    protected override VamBase CreateBfsSearcher() => new Vam(tp);

    protected override CycleSearcher CreateCycleSearcher(AllocationMatrix am) => new(am);

    protected override EpsilonPerturbation CreateEpsilonPerturbation(
        AllocationMatrix am,
        Matrix<double> cost
    ) => new(am, cost);

    private protected override PotCalcBase CreatePotentialsCalculator(
        AllocationMatrix am,
        Matrix<double> cost,
        double[] RPotential,
        double[] CPotential
    ) => new PotCalc(am, cost, RPotential, CPotential);
}
