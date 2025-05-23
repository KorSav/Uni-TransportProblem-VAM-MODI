namespace TpSolver.Shared;

/// <summary>
/// Encapsulates a non-negative allocation value,
/// additionally storing whether the value is basic or not.
/// <para>
/// Allows any value to be made basic via <see cref="AsBasic"/>.
/// </para>
/// </summary>
public struct Allocation
{
    const int emptyValue = -1;
    int value;

    public readonly bool IsBasic => value != emptyValue;

    /// <summary>
    /// Makes Allocation to be basic, despite its current value
    /// </summary>
    /// <returns>Modified struct</returns>
    public Allocation AsBasic()
    {
        value = IsBasic ? value : 0;
        return this;
    }

    /// <summary>
    /// By default all positive values - basic, if zero - non basic
    /// </summary>
    /// <param name="value">Non negative allocation amount</param>
    /// <exception cref="ArgumentException">If negative <paramref name="value"/> was given</exception>
    public Allocation(int value = 0)
    {
        if (value < 0)
            throw new ArgumentException($"Allocation can not be negative, but got {value}");
        this.value = (value == 0) ? emptyValue : value;
    }

    public static implicit operator int(Allocation a) => (a.value == emptyValue) ? 0 : a.value;
}
