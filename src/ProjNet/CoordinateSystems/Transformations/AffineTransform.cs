// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2005-2009 Morten Nielsen <www.sharpgis.net>
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.CoordinateSystems.Transformations;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

/// <summary>
/// Represents an affine math transform that transforms input coordinates to target coordinates using an affine transformation matrix. Dimensionality may change.
/// </summary>
/// <remarks>
/// If the transform's input dimension is M, and output dimension is N, then the
/// matrix has size <c>[N+1][M+1]</c>. The extra row and column encode the affine
/// translation terms in homogeneous coordinates. Inverse creation uses standard
/// LUP decomposition with partial pivoting to solve for the inverse matrix.
/// </remarks>
public class AffineTransform : MathTransform
{
    /// <summary>
    /// Dimension of source points - it's related to number of transformation matrix rows.
    /// </summary>
    private readonly int dimSource;

    /// <summary>
    /// Dimension of output points - it's related to number of columns.
    /// </summary>
    private readonly int dimTarget;

    /// <summary>
    /// Represents transform matrix of this affine transformation from input points to output ones using dimensionality defined within the affine transform
    /// Number of rows = dimTarget + 1
    /// Number of columns = dimSource + 1.
    /// </summary>
    private readonly double[,] transformMatrix;

    /// <summary>
    /// Saved inverse transform.
    /// </summary>
    private MathTransform? inverse;

    /// <summary>
    /// Initializes a new instance of the <see cref="AffineTransform"/> class with a 2D affine transform.
    /// </summary>
    /// <param name="m00">Value for row 0, column 0 - AKA ScaleX.</param>
    /// <param name="m01">Value for row 0, column 1 - AKA ShearX.</param>
    /// <param name="m02">Value for row 0, column 2 - AKA Translate X.</param>
    /// <param name="m10">Value for row 1, column 0 - AKA Shear Y.</param>
    /// <param name="m11">Value for row 1, column 1 - AKA Scale Y.</param>
    /// <param name="m12">Value for row 1, column 2 - AKA Translate Y.</param>
    public AffineTransform(double m00, double m01, double m02, double m10, double m11, double m12)
    {
        // fill dimensionlity
        this.dimSource = 2;
        this.dimTarget = 2;

        // create matrix - 2D affine transform uses 3x3 matrix (3rd row is the special one)
        this.transformMatrix = new[,]
        {
            { m00, m01, m02 },
            { m10, m11, m12 },
            { 0, 0, 1 },
        };
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AffineTransform"/> class using the specified transformation matrix.
    /// </summary>
    /// <remarks>
    /// If the transform's input dimension is M, and output dimension is N, then
    /// the matrix has size <c>[N+1][M+1]</c>. The inverse matrix is obtained
    /// with the same LUP decomposition with partial pivoting used by
    /// <see cref="Inverse()"/>.
    /// </remarks>
    ///
    /// <param name="matrix">Matrix used to create the affine transform.</param>
    public AffineTransform(double[,] matrix)
    {
        // check validity
        matrix = ArgumentGuard.ThrowIfNull(matrix, nameof(matrix));
        if (matrix.GetLength(0) <= 1)
        {
            ArgumentGuard.ThrowArgument("Transformation matrix must have at least 2 rows.");
        }

        if (matrix.GetLength(1) <= 1)
        {
            ArgumentGuard.ThrowArgument("Transformation matrix must have at least 2 columns.");
        }

        // fill dimensionlity - dimension is M, and output dimension is N, then the matrix will have size [N+1][M+1].
        this.dimSource = matrix.GetLength(1) - 1;
        this.dimTarget = matrix.GetLength(0) - 1;

        // use specified matrix
        this.transformMatrix = matrix;
    }

    /// <summary>
    /// Gets a Well-Known Text representation of this affine math transformation.
    /// </summary>
    public override string WKT
    {
        get
        {
            // PARAM_MT["Affine",
            //    PARAMETER["num_row",3],
            //    PARAMETER["num_col",3],
            //    PARAMETER["elt_0_1",1],
            //    PARAMETER["elt_0_2",2],
            //    PARAMETER["elt 1 2",3]]
            var sb = new StringBuilder();

            sb.Append("PARAM_MT[\"Affine\"");

            // append parameters
            foreach (ProjectionParameter param in this.GetParameterValues())
            {
                sb.Append(',');
                sb.Append(param.WKT);
            }

            sb.Append(']');
            return sb.ToString();
        }
    }

    /// <summary>
    /// Gets an XML representation of this affine transformation.
    /// </summary>
    public override string XML => throw new NotImplementedException("The method or operation is not implemented.");

    /// <inheritdoc />
    public override int DimSource => this.dimSource;

    /// <inheritdoc />
    public override int DimTarget => this.dimTarget;

    /// <summary>
    /// Returns the inverse of this affine transformation.
    /// </summary>
    /// <returns>IMathTransform that is the reverse of the current affine transformation.</returns>
    public override MathTransform Inverse()
    {
        if (this.inverse is null)
        {
            // find the inverse transformation matrix - use cloned matrix array
            // remarks about dimensionality: if input dimension is M, and output dimension is N, then the matrix will have size [N+1][M+1].
            double[,] invMatrix = InvertMatrix((double[,])this.transformMatrix.Clone());
            this.inverse = new AffineTransform(invMatrix);
        }

        return this.inverse;
    }

    /// <inheritdoc />
    public override void Transform(ref double x, ref double y, ref double z)
    {
        (x, y, z) = this.TransformAffine(x, y, z);
    }

    /// <summary>
    /// Reverses the transformation.
    /// </summary>
    public override void Invert()
    {
        throw new NotSupportedException("The method or operation is not supported.");
    }

    /// <summary>
    /// Returns this affine transform as a cloned transformation matrix.
    /// </summary>
    /// <returns>A copy of the internal transformation matrix with dimensions [<see cref="DimTarget"/>+1][<see cref="DimSource"/>+1].</returns>
    public double[,] GetMatrix()
    {
        return (double[,])this.transformMatrix.Clone();
    }

    /// <summary>
    /// Return affine transformation matrix as group of parameter values that maiy be used for retrieving WKT of this affine transform.
    /// </summary>
    /// <returns>List of string pairs NAME VALUE.</returns>
    private List<ProjectionParameter> GetParameterValues()
    {
        int rowCnt = this.transformMatrix.GetLength(0);
        int colCnt = this.transformMatrix.GetLength(1);
        List<ProjectionParameter> pInfo =
        [
            new("num_row", rowCnt),
            new("num_col", colCnt),
        ];

        // fill matrix values
        for (int row = 0; row < rowCnt; row++)
        {
            for (int col = 0; col < colCnt; col++)
            {
                string name = FormattableString.Invariant($"elt_{row}_{col}");
                pInfo.Add(new ProjectionParameter(name, this.transformMatrix[row, col]));
            }
        }

        return pInfo;
    }

    /// <summary>
    /// Given L, U, P and b, solves for x using forward and back substitution.
    /// Input the L and U matrices as a single combined LU matrix.
    /// Returns the solution as a <see cref="double"/> array.
    /// LU will be a n+1 x m+1 matrix where the first row and columns are zero.
    /// This is for ease of computation and consistency with Cormen et al. pseudocode.
    /// The π array represents the permutation matrix.
    /// </summary>
    /// <param name="lu">The lu parameter.</param>
    /// <param name="pi">The pi parameter.</param>
    /// <param name="b">The b parameter.</param>
    /// <param name="solution">Destination span for the computed solution vector.</param>
    private static void LUPSolve(double[,] lu, int[] pi, ReadOnlySpan<double> b, Span<double> solution)
    {
        int n = lu.GetLength(0) - 1;
        int dimension = n + 1;
        if (b.Length < dimension)
        {
            ArgumentGuard.ThrowArgument("Input vector is too short.", nameof(b));
        }

        if (solution.Length < dimension)
        {
            ArgumentGuard.ThrowArgument("Solution buffer is too short.", nameof(solution));
        }

        Span<double> xSpan = solution[..dimension];
        Span<double> yBuffer = dimension <= 128 ? stackalloc double[128] : new double[dimension];
        Span<double> ySpan = yBuffer[..dimension];

        // Solve for y using formward substitution
        for (int i = 0; i <= n; i++)
        {
            double suml = 0;
            for (int j = 0; j <= i - 1; j++)
            {
                // Since we've taken L and U as a singular matrix as an input
                // the value for L at index i and j will be 1 when i equals j, not LU[i][j], since
                // the diagonal values are all 1 for L.
                double lij;
                if (i == j)
                {
                    lij = 1;
                }
                else
                {
                    lij = lu[i, j];
                }

                suml += lij * ySpan[j];
            }

            ySpan[i] = b[pi[i]] - suml;
        }

        // Solve for x by using back substitution
        for (int i = n; i >= 0; i--)
        {
            double sumu = 0;
            for (int j = i + 1; j <= n; j++)
            {
                sumu += lu[i, j] * xSpan[j];
            }

            xSpan[i] = (ySpan[i] - sumu) / lu[i, i];
        }
    }

    /// <summary>
    /// Performs LUP decomposition on matrix A in-place and returns the permutation array.
    /// The first row and first column of A are expected to be zero (1-based indexing convention).
    /// </summary>
    /// <param name="a">The a parameter.</param>
    /// <returns>The transformation result.</returns>
    private static int[] LUPDecomposition(double[,] a)
    {
        int n = a.GetLength(0) - 1;

        // pi represents the permutation matrix.  We implement it as an array
        // whose value indicates which column the 1 would appear.  We use it to avoid
        // dividing by zero or small numbers.
        int[] pi = new int[n + 1];
        int kp = 0;

        // Initialize the permutation matrix, will be the identity matrix
        for (int j = 0; j <= n; j++)
        {
            pi[j] = j;
        }

        for (int k = 0; k <= n; k++)
        {
            // In finding the permutation matrix p that avoids dividing by zero
            // we take a slightly different approach.  For numerical stability
            // We find the element with the largest
            // absolute value of those in the current first column (column k).  If all elements in
            // the current first column are zero then the matrix is singluar and throw an
            // error.
            double p = 0;
            for (int i = k; i <= n; i++)
            {
                if (Math.Abs(a[i, k]) > p)
                {
                    p = Math.Abs(a[i, k]);
                    kp = i;
                }
            }

            if (p == 0)
            {
                throw new InvalidOperationException("singular matrix");
            }

            // These lines update the pivot array (which represents the pivot matrix)
            // by exchanging pi[k] and pi[kp].
            int pik = pi[k];
            int pikp = pi[kp];
            pi[k] = pikp;
            pi[kp] = pik;

            // Exchange rows k and kpi as determined by the pivot
            for (int i = 0; i <= n; i++)
            {
                double aki = a[k, i];
                double akpi = a[kp, i];
                a[k, i] = akpi;
                a[kp, i] = aki;
            }

            // Compute the Schur complement
            for (int i = k + 1; i <= n; i++)
            {
                a[i, k] = a[i, k] / a[k, k];
                for (int j = k + 1; j <= n; j++)
                {
                    a[i, j] = a[i, j] - (a[i, k] * a[k, j]);
                }
            }
        }

        return pi;
    }

    /// <summary>
    /// Given an n×n matrix A, solves n linear equations to find the inverse of A using LUP decomposition.
    /// </summary>
    /// <param name="a">The a parameter.</param>
    /// <returns>The transformation result.</returns>
    private static double[,] InvertMatrix(double[,] a)
    {
        int n = a.GetLength(0);
        int m = a.GetLength(1);

        // x will hold the inverse matrix to be returned
        double[,] x = new double[n, m];

        // Get the LU matrix and P matrix (as an array)
        int[] p = LUPDecomposition(a);
        double[,] lU = a;
        Span<double> eBuffer = m <= 128 ? stackalloc double[128] : new double[m];
        Span<double> solveBuffer = m <= 128 ? stackalloc double[128] : new double[m];
        Span<double> e = eBuffer[..m];
        Span<double> solve = solveBuffer[..m];

        // Solve AX = e for each column ei of the identity matrix using LUP decomposition
        for (int i = 0; i < n; i++)
        {
            // e will represent each column in the identity matrix
            e.Clear();
            e[i] = 1;
            LUPSolve(lU, p, e, solve);
            for (int j = 0; j < solve.Length; j++)
            {
                x[j, i] = solve[j];
            }
        }

        return x;
    }

    /// <summary>
    /// Transforms a coordinate point. The passed parameter point should not be modified.
    /// </summary>
    /// <param name="x">The x-ordinate value.</param>
    /// <param name="y">The y-ordinate value.</param>
    /// <param name="z">The z-ordinate value.</param>
    /// <returns>The converted x-, y- and z-ordinate tuple.</returns>
    private (double X, double Y, double Z) TransformAffine(double x, double y, double z)
    {
        // check source dimensionality - allow coordinate clipping, if source dimensionality is greater then expected source dimensionality of affine transformation
        Span<double> point = [x, y, z];
        if (this.dimSource > 3 || this.dimTarget > 3)
        {
            throw new NotSupportedException();
        }

        // use transformation matrix to create output points that has dimTarget dimensionality
        Span<double> transformed = stackalloc double[this.dimTarget];

        // count each target dimension using the apropriate row
        for (int row = 0; row < this.dimTarget; row++)
        {
            // start with the last value which is in fact multiplied by 1
            double dimVal = this.transformMatrix[row, this.dimSource];
            for (int col = 0; col < this.dimSource; col++)
            {
                dimVal += this.transformMatrix[row, col] * point[col];
            }

            transformed[row] = dimVal;
        }

        (double X, double Y, double Z) ret = default;
        if (transformed.Length > 2)
        {
            ret.Z = transformed[2];
        }

        if (transformed.Length > 1)
        {
            ret.Y = transformed[1];
        }

        if (transformed.Length > 0)
        {
            ret.X = transformed[0];
        }

        return ret;
    }
}
