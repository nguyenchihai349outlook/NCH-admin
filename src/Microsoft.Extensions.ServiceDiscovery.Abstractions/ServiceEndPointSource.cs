// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Primitives;

namespace Microsoft.Extensions.ServiceDiscovery;

/// <summary>
/// Represents a collection of service endpoints.
/// </summary>
[DebuggerDisplay("{ToString(),nq}")]
[DebuggerTypeProxy(typeof(ServiceEndPointCollectionDebuggerView))]
public sealed class ServiceEndPointSource
{
    private readonly List<ServiceEndPoint>? _endpoints;

    /// <summary>
    /// Initializes a new <see cref="ServiceEndPointSource"/> instance.
    /// </summary>
    /// <param name="endpoints">The endpoints.</param>
    /// <param name="changeToken">The change token.</param>
    /// <param name="features">The feature collection.</param>
    public ServiceEndPointSource(List<ServiceEndPoint>? endpoints, IChangeToken changeToken, IFeatureCollection features)
    {
        ArgumentNullException.ThrowIfNull(changeToken);

        _endpoints = endpoints;
        Features = features;
        ChangeToken = changeToken;
    }

    /// <summary>
    /// Gets the endpoints.
    /// </summary>
    public IReadOnlyList<ServiceEndPoint> EndPoints => _endpoints ?? (IReadOnlyList<ServiceEndPoint>)[];

    /// <summary>
    /// Gets the change token which indicates when this collection should be refreshed.
    /// </summary>
    public IChangeToken ChangeToken { get; }

    /// <summary>
    /// Gets the feature collection.
    /// </summary>
    public IFeatureCollection Features { get; }

    /// <inheritdoc/>
    public override string ToString()
    {
        if (_endpoints is not { } eps)
        {
            return "[]";
        }

        return $"[{string.Join(", ", eps)}]";
    }

    private sealed class ServiceEndPointCollectionDebuggerView(ServiceEndPointSource value)
    {
        public IChangeToken ChangeToken => value.ChangeToken;

        public IFeatureCollection Features => value.Features;

        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        public ServiceEndPoint[] EndPoints => value.EndPoints.ToArray();
    }
}
