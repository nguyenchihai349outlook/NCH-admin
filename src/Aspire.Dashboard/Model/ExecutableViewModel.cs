// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;

namespace Aspire.Dashboard.Model;

/// <summary>
/// Immutable snapshot of executable state at a point in time.
/// </summary>
public class ExecutableViewModel : ResourceViewModel
{
    // IMPORTANT! Be sure to reflect any property changes here in the Equals method below

    public override string ResourceType => "Executable";

    public required int? ProcessId { get; init; }
    public required string? ExecutablePath { get; init; }
    public required string? WorkingDirectory { get; init; }
    public required ImmutableArray<string>? Arguments { get; init; }

    public override bool Equals(ResourceViewModel? other)
    {
        return other is ExecutableViewModel executable
            && ProcessId == executable.ProcessId
            && StringComparer.Ordinal.Equals(ExecutablePath, executable.ExecutablePath)
            && StringComparer.Ordinal.Equals(WorkingDirectory, executable.WorkingDirectory)
            && Arguments is null == executable.Arguments is null
            && (Arguments is null || Arguments.Value.SequenceEqual(executable.Arguments!.Value))
            && base.Equals(other);
    }
}

