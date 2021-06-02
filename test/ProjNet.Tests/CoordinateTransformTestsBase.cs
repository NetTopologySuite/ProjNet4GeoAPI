using System;
using System.Globalization;
using NUnit.Framework;
using ProjNet.CoordinateSystems;
using ProjNet.CoordinateSystems.Transformations;

namespace ProjNET.Tests
{
    public class CoordinateTransformTestsBase
    {
        protected readonly CoordinateSystemFactory CoordinateSystemFactory = new CoordinateSystemFactory();
        protected readonly CoordinateTransformationFactory CoordinateTransformationFactory = new CoordinateTransformationFactory();
        protected readonly Random Random = new Random();

        protected bool Verbose { get; set; }

        protected bool ToleranceLessThan(double[] p1, double[] p2, double tolerance)
        {
            double d0 = Math.Abs(p1[0] - p2[0]);
            double d1 = Math.Abs(p1[1] - p2[1]);
            if (p1.Length > 2 && p2.Length > 2)
            {
                double d2 = Math.Abs(p1[2] - p2[2]);
                if (Verbose)
                    Console.WriteLine("Allowed Tolerance {3}; got dx: {0}, dy: {1}, dz {2}", d0, d1, d2, tolerance);
                return d0 < tolerance && d1 < tolerance && d2 < tolerance;
            }
            Console.WriteLine();
            if (Verbose)
                Console.WriteLine("Allowed tolerance {2}; got dx: {0}, dy: {1}", d0, d1, tolerance);
            return d0 < tolerance && d1 < tolerance;
        }

        protected string TransformationError(string projection, double[] pExpected, double[] pResult, bool reverse = false)
        {
            return string.Format(CultureInfo.InvariantCulture,
                                 "{6} {7} transformation outside tolerance!\n\tExpected [{0}, {1}],\n\tgot      [{2}, {3}],\n\tdelta    [{4}, {5}]",
                                 pExpected[0], pExpected[1], 
                                 pResult[0], pResult[1], 
                                 pExpected[0]-pResult[0], pExpected[1]-pResult[1],
                                 projection, reverse ? "reverse" : "forward");
        }

        public void Test(string title, CoordinateSystem source, CoordinateSystem target, 
                         double[] testPoint, double[] expectedPoint,
                         double tolerance, double reverseTolerance = double.NaN)
        {
            var ct = CoordinateTransformationFactory.CreateFromCoordinateSystems(source, target);

            double[] forwardResult = ct.MathTransform.Transform(testPoint);
            double[] reverseResult = double.IsNaN(reverseTolerance)
                                    ? testPoint
                                    : ct.MathTransform.Inverse().Transform(forwardResult);

            bool forward = ToleranceLessThan(forwardResult, expectedPoint, tolerance);

            bool reverse = double.IsNaN(reverseTolerance) || 
                          ToleranceLessThan(reverseResult, testPoint, reverseTolerance);

            if (!forward)
                TransformationError(title, expectedPoint, forwardResult);
            if (!reverse)
                TransformationError(title, testPoint, reverseResult, true);

            Assert.IsTrue(forward && reverse);


        }
    }
}
