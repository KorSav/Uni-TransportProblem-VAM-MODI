using TpSolver.Shared;

namespace TpSolver.Solver.Modi.PotentialsCalculator;

abstract class PotCalcBase(
    AllocationMatrix allocation,
    Matrix<double> cost,
    double[] RPotential,
    double[] CPotential
)
{
    protected readonly AllocationMatrix allocation = allocation;
    protected readonly Matrix<double> cost = cost;

    protected readonly int m = RPotential.Length;
    protected readonly double[] RPotential = RPotential;

    protected readonly int n = CPotential.Length;
    protected readonly double[] CPotential = CPotential;

    /// <summary>
    /// Starts with RPotential[0] = 0
    /// </summary>
    public virtual void CalcPotentials()
    {
        RPotential[0] = 0;
        CalcPotentialsFrom(idx: 0, isRPotential: true);
    }

    protected abstract void CalcPotentialsFrom(int idx, bool isRPotential);
}
