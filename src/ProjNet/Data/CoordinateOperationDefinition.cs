// <copyright file="CoordinateOperationDefinition.cs" company="NetTopologySuite - Team">
// Copyright (c) NetTopologySuite - Team. All rights reserved.
// </copyright>

namespace ProjNet.Data
{
    /// <summary>
    /// Defines the operation kind represented by a catalog entry.
    /// </summary>
    internal enum CoordinateOperationKind : byte
    {
        /// <summary>
        /// A direct transformation between source and target coordinate systems.
        /// </summary>
        Transformation = 0,

        /// <summary>
        /// A chained operation composed from multiple individual operations.
        /// </summary>
        ConcatenatedOperation = 1,

        /// <summary>
        /// A time-dependent point motion operation.
        /// </summary>
        PointMotionOperation = 2,
    }

    /// <summary>
    /// Represents a coordinate operation catalog definition.
    /// </summary>
    internal sealed class CoordinateOperationDefinition
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CoordinateOperationDefinition"/> class.
        /// </summary>
        /// <param name="operationKind">The operation kind.</param>
        /// <param name="operationCode">The operation code.</param>
        /// <param name="sourceSrid">The source spatial reference identifier.</param>
        /// <param name="targetSrid">The target spatial reference identifier.</param>
        /// <param name="accuracy">The expected operation accuracy.</param>
        /// <param name="methodName">The method name.</param>
        /// <param name="parameterFileName">The optional parameter file name.</param>
        internal CoordinateOperationDefinition(
            CoordinateOperationKind operationKind,
            int operationCode,
            int sourceSrid,
            int targetSrid,
            double accuracy,
            string methodName,
            string parameterFileName)
        {
            this.OperationKind = operationKind;
            this.OperationCode = operationCode;
            this.SourceSrid = sourceSrid;
            this.TargetSrid = targetSrid;
            this.Accuracy = accuracy;
            this.MethodName = methodName;
            this.ParameterFileName = parameterFileName;
        }

        /// <summary>
        /// Gets the expected operation accuracy.
        /// </summary>
        internal double Accuracy { get; }

        /// <summary>
        /// Gets the operation method name.
        /// </summary>
        internal string MethodName { get; }

        /// <summary>
        /// Gets the operation code.
        /// </summary>
        internal int OperationCode { get; }

        /// <summary>
        /// Gets the operation kind.
        /// </summary>
        internal CoordinateOperationKind OperationKind { get; }

        /// <summary>
        /// Gets the optional parameter file name.
        /// </summary>
        internal string ParameterFileName { get; }

        /// <summary>
        /// Gets the source SRID.
        /// </summary>
        internal int SourceSrid { get; }

        /// <summary>
        /// Gets the target SRID.
        /// </summary>
        internal int TargetSrid { get; }
    }
}
