namespace TpSolver;

public class TransportProblem
{
    public double[,] Cost { get; private set; } = null!;
    public int[] Supply { get; private set; } = null!;
    public int[] Demand { get; private set; } = null!;

    int m;
    int n;

    private TransportProblem() { }

    /// <summary>
    /// Makes copies of all arrays if not balanced problem provided.
    /// </summary>
    public TransportProblem(double[,] cost, int[] supply, int[] demand)
        : this(cost, supply, demand, true) => BalanceIfNecessary(isSpaceReserved: false);

    // for testing purposes
    internal TransportProblem(double[,] cost, int[] supply, int[] demand, bool noBalancing)
    {
        m = cost.GetLength(0);
        n = cost.GetLength(1);
        if (m != supply.Length)
            throw new ArgumentException(
                $"Invalid TP: supply ({supply.Length}) should match cost rows ({m})"
            );
        if (n != demand.Length)
            throw new ArgumentException(
                $"Invalid TP: demand ({demand.Length}) should match cost columns ({n})"
            );
        Cost = cost;
        Supply = supply;
        Demand = demand;
    }

    /// <returns>Whether balancing was made</returns>
    private bool BalanceIfNecessary(bool isSpaceReserved = true)
    {
        int totalSupply = 0;
        int totalDemand = 0;
        for (int i = 0; i < m; i++)
            totalSupply += Supply[i];
        for (int j = 0; j < n; j++)
            totalDemand += Demand[j];

        if (totalSupply == totalDemand)
            return false;

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

            Cost = extCost;
            Supply = extSupply;
            Demand = extDemand;
            m++;
            n++;
        }
        if (totalSupply > totalDemand) // need dummy destination
            Demand[^1] = totalSupply - totalDemand;
        if (totalDemand > totalSupply) // need dummy supply
            Supply[^1] = totalDemand - totalSupply;
        return true;
    }

    public static TransportProblem GenerateRandom(int NSupply, int NDemand)
    {
        return GenerateRandom(NSupply, NDemand, new());
    }

    public static TransportProblem GenerateRandom(int NSupply, int NDemand, TPValueLimits lim)
    {
        Random rnd = new();
        var cost = new double[NSupply + 1, NDemand + 1];
        var supply = new int[NSupply + 1];
        var demand = new int[NDemand + 1];

        for (int i = 0; i < NSupply; i++)
            supply[i] = rnd.Next(lim.SupplyLowerBound, lim.SupplyUpperBound);
        for (int j = 0; j < NDemand; j++)
            demand[j] = rnd.Next(lim.DemandLowerBound, lim.DemandUpperBound);
        for (int i = 0; i < NSupply; i++)
        for (int j = 0; j < NDemand; j++)
        {
            double d = lim.CostUpperBound - lim.CostLowerBound;
            cost[i, j] = lim.CostLowerBound + d * rnd.NextDouble();
        }

        TransportProblem tp = new(cost, supply, demand, true);
        tp.BalanceIfNecessary(); // without arrays realloc
        return tp;
    }
}
