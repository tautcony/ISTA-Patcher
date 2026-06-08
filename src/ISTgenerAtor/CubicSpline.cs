// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2025-2026 TautCony

namespace ISTgenerAtor;

using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Natural cubic spline interpolation over evenly spaced samples. Produces the per-segment
/// polynomial coefficients (a, b, c, d) used to evaluate the curve at integer positions.
/// </summary>
internal static class CubicSpline
{
    public static List<int[]> CalculateCoefficients(byte[] data)
    {
        var n = data.Length;
        var x = Enumerable.Range(0, n).Select(i => (double)i).ToArray();
        var y = data.Select(b => (double)b).ToArray();
        var h = new double[x.Length - 1];
        for (var i = 0; i < x.Length - 1; i++)
        {
            h[i] = x[i + 1] - x[i];
        }

        var a = new double[n, n];
        var b = new double[n];

        a[0, 0] = 1;
        a[n - 1, n - 1] = 1;

        for (var i = 1; i < n - 1; i++)
        {
            a[i, i - 1] = h[i - 1];
            a[i, i] = 2 * (h[i - 1] + h[i]);
            a[i, i + 1] = h[i];
            b[i] = 3 * (((y[i + 1] - y[i]) / h[i]) - ((y[i] - y[i - 1]) / h[i - 1]));
        }

        var c = SolveLinearSystem(a, b);

        var coefficients = new List<int[]>();
        for (var i = 0; i < n - 1; i++)
        {
            var ai = Convert.ToInt32(y[i]);
            var bi = Convert.ToInt32(((y[i + 1] - y[i]) / h[i]) - (h[i] * ((2 * c[i]) + c[i + 1]) / 3));
            var ci = Convert.ToInt32(c[i]);
            var di = Convert.ToInt32((c[i + 1] - c[i]) / (3 * h[i]));
            coefficients.Add([ai, bi, ci, di]);
        }

        return coefficients;
    }

    private static double[] SolveLinearSystem(double[,] a, double[] b)
    {
        const double epsilon = 1e-12;
        var n = b.Length;
        var x = new double[n];

        for (var i = 1; i < n; i++)
        {
            if (Math.Abs(a[i - 1, i - 1]) < epsilon)
            {
                throw new InvalidOperationException($"Zero pivot at position ({i - 1}, {i - 1}) in tridiagonal system.");
            }

            var m = a[i, i - 1] / a[i - 1, i - 1];
            a[i, i] -= m * a[i - 1, i];
            b[i] -= m * b[i - 1];
        }

        if (Math.Abs(a[n - 1, n - 1]) < epsilon)
        {
            throw new InvalidOperationException($"Zero pivot at position ({n - 1}, {n - 1}) in tridiagonal system.");
        }

        x[n - 1] = b[n - 1] / a[n - 1, n - 1];
        for (var i = n - 2; i >= 0; i--)
        {
            if (Math.Abs(a[i, i]) < epsilon)
            {
                throw new InvalidOperationException($"Zero pivot at position ({i}, {i}) in tridiagonal system.");
            }

            x[i] = (b[i] - (a[i, i + 1] * x[i + 1])) / a[i, i];
        }

        return x;
    }
}
