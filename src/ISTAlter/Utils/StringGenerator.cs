// SPDX-License-Identifier: GPL-3.0-or-later
// SPDX-FileCopyrightText: Copyright 2025 TautCony

namespace ISTAlter.Utils;

public class StringGenerator(int[][] coefficients)
{
    public IEnumerable<int> Generate()
    {
        var n = coefficients.Length;
        long x = 0;

        while (true)
        {
            var i = Math.Min(Math.Max(0, x < n ? (int)x : n - 1), n - 1);
            var dx = x - i;

            var dx2 = dx * dx;
            var dx3 = dx * dx * dx;

            var result = coefficients[i][0] +
                         (coefficients[i][1] * dx) +
                         (coefficients[i][2] * dx2) +
                         (coefficients[i][3] * dx3);

            yield return (int)result;
            x++;
        }
    }
}
