// SPDX-License-Identifier: LGPL-2.1-or-later
// SPDX-FileCopyrightText: 2026 Martin Karing / TKI mbH, Chemnitz, Germany

namespace ProjNet.Tests;

using System.Diagnostics.CodeAnalysis;
using Xunit;

/// <summary>
/// Defines a non-parallel test collection for tests that mutate process-wide environment variables or static resolver state.
/// </summary>
[SuppressMessage("Performance", "CA1515:Consider making public types internal", Justification = "xUnit requires collection definition classes to be public.")]
[CollectionDefinition("Global environment tests", DisableParallelization = true)]
public sealed class GlobalEnvironmentTestIsolation
{
    /// <summary>
    /// The shared collection name for environment-sensitive tests.
    /// </summary>
    public const string Name = "Global environment tests";
}
