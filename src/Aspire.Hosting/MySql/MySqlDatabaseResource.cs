// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Aspire.Hosting.Publishing;

namespace Aspire.Hosting.ApplicationModel;

/// <summary>
/// A resource that represents a MySQL database. This is a child resource of a <see cref="MySqlServerResource"/>.
/// </summary>
/// <param name="name">The name of the resource.</param>
/// <param name="parent">The MySQL parent resource associated with this database.</param>
public class MySqlDatabaseResource(string name, MySqlServerResource parent) : Resource(name), IResourceWithParent<MySqlServerResource>, IResourceWithConnectionString
{
    /// <summary>
    /// Gets the parent MySQL container resource.
    /// </summary>
    public MySqlServerResource Parent { get; } = parent;

    /// <summary>
    /// Gets the connection string expression for the MySQL database.
    /// </summary>
    public string ConnectionStringExpression =>
        $"{{{Parent.Name}.connectionString}};Database={Name}";

    /// <summary>
    /// Gets the connection string for the MySQL database.
    /// </summary>
    /// <returns>A connection string for the MySQL database.</returns>
    public string? GetConnectionString()
    {
        if (Parent.GetConnectionString() is { } connectionString)
        {
            return $"{connectionString};Database={Name}";
        }
        else
        {
            throw new DistributedApplicationException("Parent resource connection string was null.");
        }
    }

    internal void WriteToManifest(ManifestPublishingContext context)
    {
        context.Writer.WriteString("type", "value.v0");
        context.WriteConnectionString(this);
    }
}
