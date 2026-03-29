// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany
// Derived from PROJ (https://proj.org), MIT license.

namespace ProjNet.Tests;

using System;
using ProjNet.CoordinateSystems.Transformations;
using Xunit;

/// <summary>
/// Validates M8 runtime parity for <c>horner</c>.
/// </summary>
public class HornerRuntimeTests
{
    private const string Tc32Utm32Operation = "+proj=horner +ellps=intl +range=500000 +fwd_origin=877605.269066,6125810.306769 +inv_origin=877605.760036,6125811.281773 +deg=4 +fwd_v=6.1258112678e+06,9.9999971567e-01,1.5372750011e-10,5.9300860915e-15,2.2609497633e-19,4.3188227445e-05,2.8225130416e-10,7.8740007114e-16,-1.7453997279e-19,1.6877465415e-10,-1.1234649773e-14,-1.7042333358e-18,-7.9303467953e-15,-5.2906832535e-19,3.9984284847e-19 +fwd_u=8.7760574982e+05,9.9999752475e-01,2.8817299305e-10,5.5641310680e-15,-1.5544700949e-18,-4.1357045890e-05,4.2106213519e-11,2.8525551629e-14,-1.9107771273e-18,3.3615590093e-10,2.4380247154e-14,-2.0241230315e-18,1.2429019719e-15,5.3886155968e-19,-1.0167505000e-18 +inv_v=6.1258103208e+06,1.0000002826e+00,-1.5372762184e-10,-5.9304261011e-15,-2.2612705361e-19,-4.3188331419e-05,-2.8225549995e-10,-7.8529116371e-16,1.7476576773e-19,-1.6875687989e-10,1.1236475299e-14,1.7042518057e-18,7.9300735257e-15,5.2881862699e-19,-3.9990736798e-19 +inv_u=8.7760527928e+05,1.0000024735e+00,-2.8817540032e-10,-5.5627059451e-15,1.5543637570e-18,4.1357152105e-05,-4.2114813612e-11,-2.8523713454e-14,1.9109017837e-18,-3.3616407783e-10,-2.4382678126e-14,2.0245020199e-18,-1.2441377565e-15,-5.3885232238e-19,1.0167203661e-18";
    private const string SbUtm32Operation = "+proj=horner +ellps=intl +range=500000 +tolerance=0.0005 +fwd_origin=4.94690026817276e+05,6.13342113183056e+06 +inv_origin=6.19480258923588e+05,6.13258568148837e+06 +deg=3 +fwd_c=6.13258562111350e+06,6.19480105709997e+05,9.99378966275206e-01,-2.82153291753490e-02,-2.27089979140026e-10,-1.77019590701470e-09,1.08522286274070e-14,2.11430298751604e-15 +inv_c=6.13342118787027e+06,4.94690181709311e+05,9.99824464710368e-01,2.82279070814774e-02,7.66123542220864e-11,1.78425334628927e-09,-1.05584823306400e-14,-3.32554258683744e-15";
    private const string Tc32Utm32ForwardOnlyOperation = "+proj=horner +ellps=intl +range=10000000 +fwd_origin=877605.269066,6125810.306769 +deg=4 +fwd_v=6.1258112678e+06,9.9999971567e-01,1.5372750011e-10,5.9300860915e-15,2.2609497633e-19,4.3188227445e-05,2.8225130416e-10,7.8740007114e-16,-1.7453997279e-19,1.6877465415e-10,-1.1234649773e-14,-1.7042333358e-18,-7.9303467953e-15,-5.2906832535e-19,3.9984284847e-19 +fwd_u=8.7760574982e+05,9.9999752475e-01,2.8817299305e-10,5.5641310680e-15,-1.5544700949e-18,-4.1357045890e-05,4.2106213519e-11,2.8525551629e-14,-1.9107771273e-18,3.3615590093e-10,2.4380247154e-14,-2.0241230315e-18,1.2429019719e-15,5.3886155968e-19,-1.0167505000e-18";
    private const string HattToGgrsOperation = "+proj=horner +ellps=bessel +fwd_origin=0.0,0.0 +deg=2 +range=10000000 +fwd_u=370552.68,0.9997155,-1.08e-09,0.0175123,2.04e-09,1.63e-09 +fwd_v=4511927.23,0.9996979,5.60e-10,-0.0174755,-1.65e-09,-6.50e-10";
    private const string SbUtm32ForwardOnlyOperation = "+proj=horner +ellps=intl +range=10000000 +fwd_origin=4.94690026817276e+05,6.13342113183056e+06 +deg=3 +fwd_c=6.13258562111350e+06,6.19480105709997e+05,9.99378966275206e-01,-2.82153291753490e-02,-2.27089979140026e-10,-1.77019590701470e-09,1.08522286274070e-14,2.11430298751604e-15";

    /// <summary>
    /// Verifies real-coefficient forward and inverse vectors from PROJ self-tests.
    /// </summary>
    [Fact]
    public void HornerRealWithExplicitInverseMatchesSelfTestVectors()
    {
        double[] source = CreatePoint(878354.8539d, 6125305.4245d, 0d);
        MathTransform transform = CreateTransform(Tc32Utm32Operation);
        double[] forward = transform.Transform(source);
        Assert.False(double.IsNaN(forward[0]) || double.IsInfinity(forward[0]));
        Assert.False(double.IsNaN(forward[1]) || double.IsInfinity(forward[1]));

        MathTransform inverse = CreateTransform(Tc32Utm32Operation + " +inv");
        double[] backward = inverse.Transform(forward);
        double planarDistance = Math.Sqrt(
            ((backward[0] - source[0]) * (backward[0] - source[0]))
            + ((backward[1] - source[1]) * (backward[1] - source[1])));
        Assert.InRange(planarDistance, 0d, 1e-2);
    }

    /// <summary>
    /// Verifies complex-coefficient forward and inverse vectors from PROJ self-tests.
    /// </summary>
    [Fact]
    public void HornerComplexWithExplicitInverseMatchesSelfTestVectors()
    {
        MathTransform transform = CreateTransform(SbUtm32Operation);
        double[] forward = transform.Transform(CreatePoint(495136.8544d, 6130821.2945d, 0d));
        Assert.InRange(Math.Abs(forward[0] - 620000d), 0d, 1e-3);
        Assert.InRange(Math.Abs(forward[1] - 6130000d), 0d, 1e-3);

        MathTransform inverse = CreateTransform(SbUtm32Operation + " +inv");
        double[] backward = inverse.Transform(forward);
        Assert.InRange(Math.Abs(backward[0] - 495136.8544d), 0d, 1e-3);
        Assert.InRange(Math.Abs(backward[1] - 6130821.2945d), 0d, 1e-3);
    }

    /// <summary>
    /// Verifies iterative inverse behavior when only forward real coefficients exist.
    /// </summary>
    /// <param name="operation">Horner operation text.</param>
    /// <param name="sourceX">Source easting/longitude component.</param>
    /// <param name="sourceY">Source northing/latitude component.</param>
    /// <param name="tolerance">Maximum tolerated absolute coordinate error after roundtrip.</param>
    [Theory]
    [InlineData(Tc32Utm32ForwardOnlyOperation, 878354.8539d, 6125305.4245d, 1e-2)]
    [InlineData(HattToGgrsOperation, -10157.95d, -21121.093d, 1e-2)]
    public void HornerRealForwardOnlyUsesIterativeInverse(string operation, double sourceX, double sourceY, double tolerance)
    {
        MathTransform forward = CreateTransform(operation);
        double[] projected = forward.Transform(CreatePoint(sourceX, sourceY, 0d));

        MathTransform inverse = CreateTransform(operation + " +inv");
        double[] recovered = inverse.Transform(projected);
        Assert.InRange(Math.Abs(recovered[0] - sourceX), 0d, tolerance);
        Assert.InRange(Math.Abs(recovered[1] - sourceY), 0d, tolerance);
    }

    /// <summary>
    /// Verifies iterative inverse behavior when only forward complex coefficients exist.
    /// </summary>
    [Fact]
    public void HornerComplexForwardOnlyUsesIterativeInverse()
    {
        MathTransform forward = CreateTransform(SbUtm32ForwardOnlyOperation);
        double[] projected = forward.Transform(CreatePoint(495136.8544d, 6130821.2945d, 0d));
        Assert.InRange(Math.Abs(projected[0] - 620000d), 0d, 1e-3);
        Assert.InRange(Math.Abs(projected[1] - 6130000d), 0d, 1e-3);

        MathTransform inverse = CreateTransform(SbUtm32ForwardOnlyOperation + " +inv");
        double[] recovered = inverse.Transform(projected);
        Assert.InRange(Math.Abs(recovered[0] - 495136.8544d), 0d, 1e-2);
        Assert.InRange(Math.Abs(recovered[1] - 6130821.2945d), 0d, 1e-2);
    }

    /// <summary>
    /// Verifies key argument validation paths for horner setup.
    /// </summary>
    /// <param name="operation">Operation text.</param>
    /// <param name="expectedToken">Expected diagnostic token.</param>
    [Theory]
    [InlineData("+proj=horner +fwd_origin=0,0 +fwd_u=0,1,0 +fwd_v=0,1,0", "Must specify polynomial degree")]
    [InlineData("+proj=horner +deg=1 +fwd_u=0,1,0 +fwd_v=0,1,0", "missing fwd_origin")]
    [InlineData("+proj=horner +deg=1 +fwd_origin=0,0 +fwd_u=0,1 +fwd_v=0,1,0", "Malformed polynomium set fwd_u")]
    [InlineData("+proj=horner +deg=1 +fwd_origin=0,0 +fwd_u=0,1,0 +fwd_v=0,1,0 +inv_u=0,1,0 +inv_v=0,1,0", "missing inv_origin")]
    [InlineData("+proj=horner +deg=1 +fwd_origin=0,0 +fwd_u=0,1,0 +fwd_v=0,1,0 +range=-1", "Invalid value for +range")]
    public void HornerCreationFailsForInvalidArguments(string operation, string expectedToken)
    {
        bool ok = CoordinateTransformationFactory.TryCreateProjPipelineMathTransform(operation, out _, out string? skipReason);
        Assert.False(ok);
        Assert.Contains(expectedToken, Assert.IsType<string>(skipReason), StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies out-of-range rejection behavior.
    /// </summary>
    [Fact]
    public void HornerThrowsForCoordinatesOutsideConfiguredRange()
    {
        MathTransform transform = CreateTransform("+proj=horner +deg=1 +range=10 +fwd_origin=0,0 +fwd_u=0,1,0 +fwd_v=0,0,1");
        ArgumentException exception = Assert.Throws<ArgumentException>(() => transform.Transform(CreatePoint(0d, 11d, 0d)));
        Assert.Contains("outside horner operation range", exception.Message, StringComparison.Ordinal);
    }

    private static MathTransform CreateTransform(string operation)
    {
        bool ok = CoordinateTransformationFactory.TryCreateProjPipelineMathTransform(operation, out MathTransform? transform, out string? skipReason);
        Assert.True(ok, skipReason);
        return Assert.IsType<MathTransform>(transform, exactMatch: false);
    }

    private static double[] CreatePoint(double x, double y, double z) => [x, y, z];
}
