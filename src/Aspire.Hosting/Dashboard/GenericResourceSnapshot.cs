// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.ApplicationModel;
using Google.Protobuf.WellKnownTypes;

namespace Aspire.Hosting.Dashboard;

internal class GenericResourceSnapshot(IResource r) : ResourceSnapshot
{
    public override string ResourceType => r.GetType().Name.Replace("Resource", "");

    protected override IEnumerable<(string Key, Value Value)> GetProperties()
    {
        if (r.TryGetLastAnnotation<DashboardPropertiesAnnotation>(out var properties))
        {
            foreach (var (key, value) in properties.Properties)
            {
                yield return (key, Value.ForString(value));
            }
        }

        yield break;
    }

    internal async IAsyncEnumerable<IReadOnlyList<(string Content, bool IsErrorMessage)>> GetLogsEnumerable()
    {
        if (!r.TryGetLastAnnotation<DashboardLoggerAnnotation>(out var annotation))
        {
            yield return [("No logs available", false)];
            yield break;
        }

        yield return annotation.Backlog;

        await foreach (var entry in annotation.LogStream.Reader.ReadAllAsync())
        {
            annotation.Backlog.Add(entry);
            yield return [entry];
        }
    }
}
