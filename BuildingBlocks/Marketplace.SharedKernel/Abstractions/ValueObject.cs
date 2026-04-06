namespace Marketplace.SharedKernel.Abstractions;

/// <summary>
/// Represents the base type for value objects with structural equality.
/// </summary>
public abstract class ValueObject
{
    /// <summary>
    /// Returns the components that participate in equality comparison.
    /// </summary>
    /// <returns>The ordered sequence of equality components.</returns>
    protected abstract IEnumerable<object?> GetEqualityComponents();

    /// <summary>
    /// Determines whether the current value object is equal to another instance.
    /// </summary>
    /// <param name="obj">The object to compare with the current instance.</param>
    /// <returns><see langword="true"/> when both objects are equal; otherwise, <see langword="false"/>.</returns>
    public override bool Equals(object? obj)
    {
        if (obj is not ValueObject other || other.GetType() != GetType())
        {
            return false;
        }

        return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
    }

    /// <summary>
    /// Returns a hash code based on the equality components.
    /// </summary>
    /// <returns>A hash code for the current instance.</returns>
    public override int GetHashCode()
    {
        return GetEqualityComponents()
            .Aggregate(0, (current, value) => HashCode.Combine(current, value));
    }
}