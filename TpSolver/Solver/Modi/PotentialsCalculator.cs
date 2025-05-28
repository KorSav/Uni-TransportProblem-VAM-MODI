using System.Diagnostics;
using TpSolver.Shared;

namespace TpSolver.Solver.Modi;

class PotentialsCalculator(
    Matrix<double> cost,
    AllocationMatrix allocation,
    double[] RPotential,
    double[] CPotential
)
{
    readonly AllocationMatrix allocation = allocation;
    readonly Matrix<double> cost = cost;

    readonly int m = RPotential.Length;
    readonly double[] RPotential = RPotential;
    readonly bool[] RDone = new bool[RPotential.Length];

    readonly int n = CPotential.Length;
    readonly double[] CPotential = CPotential;
    readonly bool[] CDone = new bool[CPotential.Length];

    /// <summary>
    /// Starts with RPotential[0] = 0
    /// </summary>
    public void CalcPotentials()
    {
        // clear done lists - to allow multiple calls
        for (int i = 0; i < m; i++)
            RDone[i] = false;
        for (int j = 0; j < n; j++)
            CDone[j] = false;

        RDone[0] = true;
        Queue<int> toCheck = new(m + n);
        toCheck.Enqueue(0);
        while (toCheck.TryDequeue(out int idx))
        {
            bool isRPotential = idx < m;
            List<int> calculated = isRPotential
                ? CalcAllNotCDoneUsing(idx)
                : CalcAllNotRDoneUsing(idx - m);
            foreach (var potential in calculated)
                toCheck.Enqueue(potential);
        }
    }

    private List<int> CalcAllNotCDoneUsing(int i)
    {
        Debug.Assert(RDone[i]);
        List<int> res = new(n);
        for (int j = 0; j < n; j++)
        {
            if (allocation[i, j].IsBasic && !CDone[j])
            {
                res.Add(m + j);
                CPotential[j] = cost[i, j] - RPotential[i];
                CDone[j] = true;
            }
        }
        return res;
    }

    private List<int> CalcAllNotRDoneUsing(int j)
    {
        Debug.Assert(CDone[j]);
        List<int> res = new(n);
        for (int i = 0; i < m; i++)
        {
            if (allocation[i, j].IsBasic && !RDone[i])
            {
                res.Add(i);
                RPotential[i] = cost[i, j] - CPotential[j];
                RDone[i] = true;
            }
        }
        return res;
    }
}
