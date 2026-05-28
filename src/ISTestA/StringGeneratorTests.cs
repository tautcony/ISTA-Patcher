// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2026 TautCony

namespace ISTestA;

using ISTAlter.Utils;

public class StringGeneratorTests
{
    /// <summary>
    /// Constant spline: all coefficients are [c, 0, 0, 0], so Generate() always returns c.
    /// </summary>
    [Test]
    public void Generate_ConstantSpline_YieldsConstantValues()
    {
        var gen = new StringGenerator([[42, 0, 0, 0]]);
        var result = gen.Generate().Take(5).ToArray();
        Assert.That(result, Is.All.EqualTo(42));
    }

    /// <summary>
    /// Linear spline: [0, 1, 0, 0] → result(x) = x.
    /// </summary>
    [Test]
    public void Generate_LinearSpline_YieldsIncreasingValues()
    {
        var gen = new StringGenerator([[0, 1, 0, 0]]);
        var result = gen.Generate().Take(6).ToArray();

        // x=0 → dx=0 → 0; x=1 → dx=0 (clamped to segment 0) → 1; etc.
        Assert.That(result[0], Is.EqualTo(0));
        Assert.That(result[1], Is.EqualTo(1));
        Assert.That(result[2], Is.EqualTo(2));
    }

    /// <summary>
    /// Two-segment spline: segment transition changes the base index i,
    /// verifying that x >= n uses the last segment clamped.
    /// </summary>
    [Test]
    public void Generate_TwoSegments_ClampsToBoundary()
    {
        // segment 0: [10, 0, 0, 0], segment 1: [20, 0, 0, 0]
        // x=0 → i=0, dx=0 → 10
        // x=1 → i=1, dx=0 → 20
        // x=2 → i=min(1,1)=1, dx=1 → 20 + 0 = 20  (clamped to last segment)
        var gen = new StringGenerator([[10, 0, 0, 0], [20, 0, 0, 0]]);
        var result = gen.Generate().Take(3).ToArray();

        Assert.That(result[0], Is.EqualTo(10));
        Assert.That(result[1], Is.EqualTo(20));
        Assert.That(result[2], Is.EqualTo(20));
    }

    /// <summary>
    /// Cubic spline segment: [a, b, c, d] → result = a + b*dx + c*dx^2 + d*dx^3.
    /// With one segment, x=0 → dx=0, x=k → dx=k.
    /// </summary>
    [Test]
    public void Generate_CubicSpline_AppliesPolynomial()
    {
        // coefficients: [1, 2, 3, 4]  → result(dx) = 1 + 2*dx + 3*dx² + 4*dx³
        var gen = new StringGenerator([[1, 2, 3, 4]]);
        var result = gen.Generate().Take(4).ToArray();

        // x=0: dx=0 → 1
        Assert.That(result[0], Is.EqualTo(1));
        // x=1: dx=1 → 1+2+3+4=10
        Assert.That(result[1], Is.EqualTo(10));
        // x=2: dx=2 → 1+4+12+32=49
        Assert.That(result[2], Is.EqualTo(49));
        // x=3: dx=3 → 1+6+27+108=142
        Assert.That(result[3], Is.EqualTo(142));
    }
}
