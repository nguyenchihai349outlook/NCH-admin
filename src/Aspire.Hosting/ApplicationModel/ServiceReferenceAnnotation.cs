// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.ObjectModel;

namespace Aspire.Hosting.ApplicationModel;

internal sealed class ServiceReferenceAnnotation(IDistributedApplicationResource resource) : IDistributedApplicationResourceAnnotation
{
    public IDistributedApplicationResource Resource { get; } = resource;
    public bool UseAllBindings { get; set; }
    public Collection<string> BindingNames { get; } = new();
}
