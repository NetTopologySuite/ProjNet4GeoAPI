// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.IO.Wkt;

using System;
using System.Runtime.Serialization;

/// <summary>
/// The exception that is thrown when Well-Known Text (WKT) cannot be parsed structurally.
/// </summary>
[Serializable]
public sealed class WktParseException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WktParseException"/> class.
    /// </summary>
    public WktParseException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WktParseException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public WktParseException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WktParseException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public WktParseException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WktParseException"/> class with serialized data.
    /// </summary>
    /// <param name="info">The object that holds the serialized object data.</param>
    /// <param name="context">The contextual information about the source or destination.</param>
#if NET8_0_OR_GREATER
    [Obsolete("Formatter-based serialization is obsolete and should not be used.", DiagnosticId = "SYSLIB0051")]
#endif
    private WktParseException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }
}
