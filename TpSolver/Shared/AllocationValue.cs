namespace TpSolver.Shared;

/// <summary>
/// Encapsulates a non-negative allocation value.
/// </summary>
/// <remarks>
/// The default value is a <b>non-basic zero</b>. To make it basic use <see cref="ToBasic"/>
/// </remarks>
public readonly struct AllocationValue
{
    // Range: {nbZero, bZero, 1, 2, ..., int.MaxValue}
    // By default should be non basic zero
    // Forced to choose this value, because CLR by default sets all fields to 0 (even if parameterless ctor is called)
    const int nbZero = 0;
    const int bZero = -1;
    readonly int value = 0; // default is always 0, despite what is written here

    public readonly bool IsBasic => value != nbZero;

    /// <summary>
    /// Makes zero allocation to be basic, while converting to int gives 0.
    /// </summary>
    /// <remarks>
    /// Converting to <see cref="int"/> and then back to <see cref="AllocationValue"/>
    /// will wipe out information about zero basic
    /// </remarks>
    /// <returns>Modified struct</returns>
    public readonly AllocationValue ToBasic() => new(IsBasic ? value : bZero, true);

    private AllocationValue(int raw, bool _)
    {
        value = raw;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AllocationValue"/> struct
    /// with the specified non-negative value.
    /// </summary>
    /// <param name="value">Allocation amount ≥ 0. Zero is treated as non-basic.</param>
    /// <exception cref="ArgumentException">Thrown if <paramref name="value"/> is negative.</exception>
    public AllocationValue(int value)
    {
        if (value < 0)
            throw new ArgumentException($"Allocation can not be negative, but got {value}");
        this.value = (value == 0) ? nbZero : value;
    }

    public static implicit operator int(AllocationValue a) =>
        a.value switch
        {
            nbZero or bZero => 0,
            _ => a.value,
        };

    public static AllocationValue operator +(AllocationValue a, AllocationValue b) =>
        new((int)a + b);

    public static AllocationValue operator -(AllocationValue a, AllocationValue b) =>
        new((int)a - b);
}
