using TpSolver.Shared;

namespace TpSolver;

public class TransportProblem : Matrix<double>
{
    public Matrix<double> Cost => data;
    public int[] Supply { get; private set; }
    public int[] Demand { get; private set; }

    /// <summary>
    /// Makes copies of all arrays if not balanced problem provided.
    /// </summary>
    public TransportProblem(Matrix<double> cost, int[] supply, int[] demand)
        : this(cost, supply, demand, shouldBalance: true, isPlaceReserved: false) { }

    // for testing purposes
    internal TransportProblem(
        Matrix<double> cost,
        int[] supply,
        int[] demand,
        bool shouldBalance,
        bool isPlaceReserved = true
    )
        : base(cost)
    {
        if (m != supply.Length)
            throw new ArgumentException(
                $"Invalid TP: supply ({supply.Length}) should match cost rows ({m})"
            );
        if (n != demand.Length)
            throw new ArgumentException(
                $"Invalid TP: demand ({demand.Length}) should match cost columns ({n})"
            );
        Supply = supply;
        Demand = demand;
        if (shouldBalance)
            BalanceIfNecessary(isPlaceReserved);
    }

    /// <returns>Whether balancing was made</returns>
    private bool BalanceIfNecessary(bool isSpaceReserved)
    {
        int totalSupply = 0;
        int totalDemand = 0;
        for (int i = 0; i < m; i++)
            totalSupply += Supply[i];
        for (int j = 0; j < n; j++)
            totalDemand += Demand[j];

        if (totalSupply == totalDemand)
        {
            if (isSpaceReserved)
            {
                Supply[^1] = 1;
                Demand[^1] = 1;
            }
            return false;
        }

        // balance problem
        if (!isSpaceReserved)
        {
            var extCost = new double[m + 1, n + 1];
            var extSupply = new int[m + 1];
            var extDemand = new int[n + 1];

            for (int i = 0; i < m; i++)
                extSupply[i] = Supply[i];
            for (int j = 0; j < n; j++)
                extDemand[j] = Demand[j];
            for (int i = 0; i < m; i++)
            for (int j = 0; j < n; j++)
                extCost[i, j] = Cost[i, j];

            data = extCost;
            Supply = extSupply;
            Demand = extDemand;
        }
        if (totalSupply > totalDemand) // need dummy destination
            Demand[^1] = totalSupply - totalDemand;
        if (totalDemand > totalSupply) // need dummy supply
            Supply[^1] = totalDemand - totalSupply;
        return true;
    }

    public static TransportProblem GenerateRandom(int NSupply, int NDemand) =>
        GenerateRandom(NSupply, NDemand, new());

    public static TransportProblem GenerateRandom(int NSupply, int NDemand, TPValueLimits lim)
    {
        Random rnd = new();
        var cost = new Matrix<double>(new double[NSupply + 1, NDemand + 1]);
        var supply = new int[NSupply + 1];
        var demand = new int[NDemand + 1];

        for (int i = 0; i < NSupply; i++)
            supply[i] = rnd.Next(lim.SupplyLowerBound, lim.SupplyUpperBound);
        for (int j = 0; j < NDemand; j++)
            demand[j] = rnd.Next(lim.DemandLowerBound, lim.DemandUpperBound);
        cost.Fill(p =>
        {
            double d = lim.CostUpperBound - lim.CostLowerBound;
            return lim.CostLowerBound + d * rnd.NextDouble();
        });

        return new(cost, supply, demand, shouldBalance: true, isPlaceReserved: true);
    }
}
