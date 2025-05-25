namespace TpSolver;

/// <summary>
/// Lower - inclusive, upper - exclusive
/// </summary>
public class TPValueLimits
{
    public TPValueLimits(
        int supplyLowerBound = 20,
        int supplyUpperBound = 201,
        int demandLowerBound = 20,
        int demandUpperBound = 201,
        double costLowerBound = 1,
        double costUpperBound = 50
    )
    {
        if (supplyLowerBound > supplyUpperBound)
            throw new ArgumentException("SupplyLowerBound must be <= SupplyUpperBound");
        if (demandLowerBound > demandUpperBound)
            throw new ArgumentException("DemandLowerBound must be <= DemandUpperBound");
        if (costLowerBound > costUpperBound)
            throw new ArgumentException("CostLowerBound must be <= CostUpperBound");
        SupplyLowerBound = supplyLowerBound;
        SupplyUpperBound = supplyUpperBound;
        DemandLowerBound = demandLowerBound;
        DemandUpperBound = demandUpperBound;
        CostLowerBound = costLowerBound;
        CostUpperBound = costUpperBound;
    }

    public int SupplyLowerBound { get; }
    public int SupplyUpperBound { get; }
    public int DemandLowerBound { get; }
    public int DemandUpperBound { get; }
    public double CostLowerBound { get; }
    public double CostUpperBound { get; }
}
