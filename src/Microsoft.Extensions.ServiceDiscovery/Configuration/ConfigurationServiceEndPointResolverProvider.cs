// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Microsoft.Extensions.ServiceDiscovery.Abstractions;

/// <summary>
/// <see cref="IServiceEndPointResolverProvider"/> implementation that resolves services using <see cref="IConfiguration"/>.
/// </summary>
/// <param name="configuration">The configuration.</param>
/// <param name="options">The options.</param>
public class ConfigurationServiceEndPointResolverProvider(
    IConfiguration configuration,
    IOptions<ConfigurationServiceEndPointResolverOptions> options) : IServiceEndPointResolverProvider
{
    private readonly IConfiguration _configuration = configuration;
    private readonly IOptions<ConfigurationServiceEndPointResolverOptions> _options = options;

    /// <inheritdoc/>
    public bool TryCreateResolver(string serviceName, [NotNullWhen(true)] out IServiceEndPointResolver? resolver)
    {
        resolver = new ConfigurationServiceEndPointResolver(serviceName, _configuration, _options);
        return true;
    }
}
