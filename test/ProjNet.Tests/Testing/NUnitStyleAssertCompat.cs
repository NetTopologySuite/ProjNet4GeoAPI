namespace ProjNET.Tests.Testing;

using System;
using System.Collections;
using System.Globalization;
using Xunit.Sdk;

/// <summary>
/// Compatibility delegate matching NUnit assertion delegates.
/// </summary>
public delegate void TestDelegate();

/// <summary>
/// Compatibility attribute for legacy NUnit repeat annotations.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class RepeatAttribute : Attribute
{
    public RepeatAttribute(int count)
    {
        this.Count = count;
    }

    public int Count { get; }
}

/// <summary>
/// Compatibility entry point for throw constraints.
/// </summary>
public static class Throws
{
    public static ThrowsNothingConstraint Nothing { get; } = new ThrowsNothingConstraint();
}

/// <summary>
/// Marker constraint for "throws nothing" assertions.
/// </summary>
public sealed class ThrowsNothingConstraint
{
    internal ThrowsNothingConstraint()
    {
    }
}

/// <summary>
/// Base class for compatibility constraints.
/// </summary>
public abstract class Constraint
{
    public abstract bool Matches(object actual);

    public virtual string Describe(object actual)
    {
        return $"Constraint '{this.GetType().Name}' failed for value '{actual ?? "null"}'.";
    }

    public OrOperator Or => new OrOperator(this);
}

public sealed class OrOperator
{
    private readonly Constraint left;

    internal OrOperator(Constraint left)
    {
        this.left = left;
    }

    public Constraint Empty => new OrConstraint(this.left, new EmptyConstraint());

    public Constraint InstanceOf<T>() => new OrConstraint(this.left, new InstanceOfConstraint(typeof(T)));
}

public sealed class OrConstraint : Constraint
{
    private readonly Constraint left;
    private readonly Constraint right;

    public OrConstraint(Constraint left, Constraint right)
    {
        this.left = left;
        this.right = right;
    }

    public override bool Matches(object actual)
    {
        return this.left.Matches(actual) || this.right.Matches(actual);
    }

    public override string Describe(object actual)
    {
        return $"Neither branch of OR constraint matched value '{actual ?? "null"}'.";
    }
}

public sealed class BooleanConstraint : Constraint
{
    private readonly bool expected;

    public BooleanConstraint(bool expected)
    {
        this.expected = expected;
    }

    public override bool Matches(object actual)
    {
        return actual is bool b && b == this.expected;
    }
}

public sealed class NullConstraint : Constraint
{
    public override bool Matches(object actual)
    {
        return actual is null;
    }
}

public sealed class NotConstraint : Constraint
{
    private readonly Constraint inner;

    public NotConstraint(Constraint inner)
    {
        this.inner = inner;
    }

    public override bool Matches(object actual)
    {
        return !this.inner.Matches(actual);
    }
}

public sealed class EmptyConstraint : Constraint
{
    public override bool Matches(object actual)
    {
        if (actual is null)
        {
            return false;
        }

        if (actual is string s)
        {
            return string.IsNullOrEmpty(s);
        }

        if (actual is ICollection collection)
        {
            return collection.Count == 0;
        }

        return false;
    }
}

public sealed class InstanceOfConstraint : Constraint
{
    private readonly Type type;

    public InstanceOfConstraint(Type type)
    {
        this.type = type;
    }

    public override bool Matches(object actual)
    {
        return actual is not null && this.type.IsInstanceOfType(actual);
    }

    public override string Describe(object actual)
    {
        return $"Value '{actual ?? "null"}' is not an instance of '{this.type.FullName}'.";
    }
}

public sealed class GreaterThanConstraint : Constraint
{
    private readonly IComparable expected;

    public GreaterThanConstraint(IComparable expected)
    {
        this.expected = expected;
    }

    public override bool Matches(object actual)
    {
        return actual is IComparable comparable && comparable.CompareTo(this.expected) > 0;
    }
}

public sealed class EqualConstraint : Constraint
{
    private readonly object expected;
    private bool hasTolerance;
    private double tolerance;

    public EqualConstraint(object expected)
    {
        this.expected = expected;
    }

    public EqualConstraint Within(double withinTolerance)
    {
        this.hasTolerance = true;
        this.tolerance = withinTolerance;
        return this;
    }

    public override bool Matches(object actual)
    {
        if (this.hasTolerance && TryToDouble(actual, out var actualDouble) && TryToDouble(this.expected, out var expectedDouble))
        {
            return Math.Abs(actualDouble - expectedDouble) <= this.tolerance;
        }

        if (TryToDecimal(actual, out var actualDecimal) && TryToDecimal(this.expected, out var expectedDecimal))
        {
            return actualDecimal == expectedDecimal;
        }

        return Equals(actual, this.expected);
    }

    public override string Describe(object actual)
    {
        if (this.hasTolerance)
        {
            return $"Expected '{actual ?? "null"}' to be within {this.tolerance} of '{this.expected ?? "null"}'.";
        }

        return $"Expected '{this.expected ?? "null"}', but found '{actual ?? "null"}'.";
    }

    private static bool TryToDouble(object value, out double result)
    {
        result = default;
        if (value is null)
        {
            return false;
        }

        if (value is IConvertible)
        {
            try
            {
                result = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
            catch (InvalidCastException)
            {
                return false;
            }
        }

        return false;
    }

    private static bool TryToDecimal(object value, out decimal result)
    {
        result = default;
        if (value is null)
        {
            return false;
        }

        if (value is IConvertible)
        {
            try
            {
                result = Convert.ToDecimal(value, CultureInfo.InvariantCulture);
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
            catch (InvalidCastException)
            {
                return false;
            }
        }

        return false;
    }
}

public sealed class NotOperator
{
    public Constraint Null => new NotConstraint(new NullConstraint());
}

/// <summary>
/// Compatibility entry point for NUnit-like fluent constraints.
/// </summary>
public static class Is
{
    public static Constraint True => new BooleanConstraint(true);

    public static Constraint False => new BooleanConstraint(false);

    public static NotOperator Not => new NotOperator();

    public static EqualConstraint EqualTo(object expected) => new EqualConstraint(expected);

    public static GreaterThanConstraint GreaterThan(IComparable expected) => new GreaterThanConstraint(expected);

    public static InstanceOfConstraint InstanceOf<T>() => new InstanceOfConstraint(typeof(T));
}

/// <summary>
/// NUnit-style assert compatibility methods backed by xUnit assertions.
/// </summary>
public static class Assert
{
    public static void Fail(string message)
    {
        throw new XunitException(message);
    }

    public static void True(bool condition)
    {
        global::Xunit.Assert.True(condition);
    }

    public static void True(bool condition, string message)
    {
        global::Xunit.Assert.True(condition, message);
    }

    public static void False(bool condition)
    {
        global::Xunit.Assert.False(condition);
    }

    public static void False(bool condition, string message)
    {
        global::Xunit.Assert.True(!condition, message);
    }

    public static void Null(object value)
    {
        global::Xunit.Assert.Null(value);
    }

    public static void Null(object value, string message)
    {
        if (value is not null)
        {
            Fail(message);
        }
    }

    public static void NotNull(object value)
    {
        global::Xunit.Assert.NotNull(value);
    }

    public static void NotNull(object value, string message)
    {
        if (value is null)
        {
            Fail(message);
        }
    }

    public static void IsNull(object value)
    {
        Null(value);
    }

    public static void IsNull(object value, string message)
    {
        Null(value, message);
    }

    public static void IsNotNull(object value)
    {
        NotNull(value);
    }

    public static void IsNotNull(object value, string message)
    {
        NotNull(value, message);
    }

    public static void IsTrue(bool condition)
    {
        True(condition);
    }

    public static void IsTrue(bool condition, string message)
    {
        True(condition, message);
    }

    public static void IsFalse(bool condition)
    {
        False(condition);
    }

    public static void IsFalse(bool condition, string message)
    {
        False(condition, message);
    }

    public static void AreEqual(object expected, object actual)
    {
        if (!AreObjectsEqual(expected, actual))
        {
            throw new XunitException($"Expected: {expected ?? "null"}{Environment.NewLine}Actual:   {actual ?? "null"}");
        }
    }

    public static void AreEqual(object expected, object actual, string message)
    {
        if (!AreObjectsEqual(expected, actual))
        {
            Fail(message);
        }
    }

    public static void AreEqual(double expected, double actual, double tolerance)
    {
        global::Xunit.Assert.True(Math.Abs(actual - expected) <= tolerance);
    }

    public static void AreEqual(double expected, double actual, double tolerance, string message)
    {
        global::Xunit.Assert.True(Math.Abs(actual - expected) <= tolerance, message);
    }

    public static void AreNotEqual(object expected, object actual)
    {
        if (AreObjectsEqual(expected, actual))
        {
            throw new XunitException($"Did not expect: {actual ?? "null"}");
        }
    }

    public static void AreNotEqual(object expected, object actual, string message)
    {
        if (AreObjectsEqual(expected, actual))
        {
            Fail(message);
        }
    }

    public static void GreaterOrEqual<T>(T actual, T expected, string message)
        where T : IComparable<T>
    {
        global::Xunit.Assert.True(actual.CompareTo(expected) >= 0, message);
    }

    public static void Greater<T>(T actual, T expected, string message = null)
        where T : IComparable<T>
    {
        if (message is null)
        {
            global::Xunit.Assert.True(actual.CompareTo(expected) > 0);
        }
        else
        {
            global::Xunit.Assert.True(actual.CompareTo(expected) > 0, message);
        }
    }

    public static void LessOrEqual<T>(T actual, T expected, string message = null)
        where T : IComparable<T>
    {
        if (message is null)
        {
            global::Xunit.Assert.True(actual.CompareTo(expected) <= 0);
        }
        else
        {
            global::Xunit.Assert.True(actual.CompareTo(expected) <= 0, message);
        }
    }

    public static void Less<T>(T actual, T expected, string message = null)
        where T : IComparable<T>
    {
        if (message is null)
        {
            global::Xunit.Assert.True(actual.CompareTo(expected) < 0);
        }
        else
        {
            global::Xunit.Assert.True(actual.CompareTo(expected) < 0, message);
        }
    }

    public static void NotZero(double value)
    {
        global::Xunit.Assert.True(value != 0d);
    }

    public static Exception Throws<TException>(TestDelegate code)
        where TException : Exception
    {
        return global::Xunit.Assert.Throws<TException>(() => code());
    }

    public static void DoesNotThrow(TestDelegate code)
    {
        try
        {
            code();
        }
        catch (Exception ex)
        {
            throw new XunitException($"Expected no exception, but got {ex.GetType().Name}: {ex.Message}");
        }
    }

    public static void That(bool condition)
    {
        global::Xunit.Assert.True(condition);
    }

    public static void That(bool condition, string message)
    {
        global::Xunit.Assert.True(condition, message);
    }

    public static void That(object actual, Constraint constraint)
    {
        That(actual, constraint, null);
    }

    public static void That(object actual, Constraint constraint, string message)
    {
        if (!constraint.Matches(actual))
        {
            Fail(message ?? constraint.Describe(actual));
        }
    }

    public static void That(TestDelegate code, ThrowsNothingConstraint _)
    {
        That(code, _, null);
    }

    public static void That(TestDelegate code, ThrowsNothingConstraint _, string message)
    {
        try
        {
            code();
        }
        catch (Exception ex)
        {
            Fail(message ?? $"Expected no exception, but got {ex.GetType().Name}: {ex.Message}");
        }
    }

    private static bool AreObjectsEqual(object expected, object actual)
    {
        if (ReferenceEquals(expected, actual))
        {
            return true;
        }

        if (expected is null || actual is null)
        {
            return false;
        }

        if (TryToDecimalStrict(expected, out var expectedDecimal) &&
            TryToDecimalStrict(actual, out var actualDecimal))
        {
            return expectedDecimal == actualDecimal;
        }

        return Equals(expected, actual);
    }

    private static bool TryToDecimalStrict(object value, out decimal result)
    {
        result = default;
        if (value is null)
        {
            return false;
        }

        switch (Type.GetTypeCode(value.GetType()))
        {
            case TypeCode.Byte:
            case TypeCode.SByte:
            case TypeCode.Int16:
            case TypeCode.UInt16:
            case TypeCode.Int32:
            case TypeCode.UInt32:
            case TypeCode.Int64:
            case TypeCode.UInt64:
            case TypeCode.Single:
            case TypeCode.Double:
            case TypeCode.Decimal:
                result = Convert.ToDecimal(value, CultureInfo.InvariantCulture);
                return true;
            default:
                return false;
        }
    }
}
