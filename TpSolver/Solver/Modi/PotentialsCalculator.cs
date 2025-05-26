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
        // null done lists - to allow multiple calls
        for (int i = 0; i < m; i++)
            RDone[i] = false;
        for (int j = 0; j < n; j++)
            CDone[j] = false;

        List<int> potentialsToAdd = new(m + n);
        int doneCount = 1;
        RDone[0] = true;
        List<int> potentialsToCheck = new(m + n) { 0 };
        do
        {
            potentialsToAdd.Clear();
            int count = potentialsToCheck.Count;
            for (int i = 0; i < count; i++)
            {
                int idx = potentialsToCheck[i];
                bool isRPotential = true;
                if (idx >= m)
                {
                    isRPotential = false;
                    idx -= m;
                }
                potentialsToAdd.AddRange(
                    isRPotential
                        ? CalcAllNotCDoneUsing(idx, ref doneCount)
                        : CalcAllNotRDoneUsing(idx, ref doneCount)
                );
            }
            potentialsToCheck.Clear();
            potentialsToCheck.AddRange(potentialsToAdd);
        } while (doneCount != RDone.Length + CDone.Length);
    }

    private List<int> CalcAllNotCDoneUsing(int i, ref int doneCount)
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
                doneCount += 1;
            }
        }
        return res;
    }

    private List<int> CalcAllNotRDoneUsing(int j, ref int doneCount)
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
                doneCount += 1;
            }
        }
        return res;
    }
}
