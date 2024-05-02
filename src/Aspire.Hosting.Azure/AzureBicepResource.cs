// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Publishing;

namespace Aspire.Hosting.Azure;

/// <summary>
/// Represents an Azure Bicep resource.
/// </summary>
/// <param name="name">Name of the resource. This will be the name of the deployment.</param>
/// <param name="templateFile">The path to the bicep file.</param>
/// <param name="templateString">A bicep snippet.</param>
/// <param name="templateResouceName">The name of an embedded resource that represents the bicep file.</param>
public class AzureBicepResource(string name, string? templateFile = null, string? templateString = null, string? templateResouceName = null) :
    Resource(name),
    IAzureResource
{
    internal string? TemplateFile { get; } = templateFile;

    internal string? TemplateString { get; } = templateString;

    internal string? TemplateResourceName { get; } = templateResouceName;

    /// <summary>
    /// Parameters that will be passed into the bicep template.
    /// </summary>
    public Dictionary<string, object?> Parameters { get; } = [];

    /// <summary>
    /// Outputs that will be generated by the bicep template.
    /// </summary>
    public Dictionary<string, string?> Outputs { get; } = [];

    /// <summary>
    /// Secret outputs that will be generated by the bicep template.
    /// </summary>
    public Dictionary<string, string?> SecretOutputs { get; } = [];

    /// <summary>
    /// Gets the path to the bicep file. If the template is a string or embedded resource, it will be written to a temporary file.
    /// </summary>
    /// <param name="directory">The directory where the bicep file will be written to (if it's a temporary file)</param>
    /// <param name="deleteTemporaryFileOnDispose">A boolean that determines if the file should be deleted on disposal of the <see cref="BicepTemplateFile"/>.</param>
    /// <returns>A <see cref="BicepTemplateFile"/> that represents the bicep file.</returns>
    /// <exception cref="InvalidOperationException"></exception>
    public BicepTemplateFile GetBicepTemplateFile(string? directory = null, bool deleteTemporaryFileOnDispose = true)
    {
        // Throw if multiple template sources are specified
        if (TemplateFile is not null && (TemplateString is not null || TemplateResourceName is not null))
        {
            throw new InvalidOperationException("Multiple template sources are specified.");
        }

        var path = TemplateFile;
        var isTempFile = false;

        if (path is null)
        {
            isTempFile = directory is null;

            path = Path.GetTempFileName() + ".bicep";

            if (TemplateResourceName is null)
            {
                // REVIEW: Consider making users specify a name for the template
                File.WriteAllText(path, TemplateString);
            }
            else
            {
                path = directory is null
                    ? path
                    : Path.Combine(directory, $"{TemplateResourceName.ToLowerInvariant()}");

                // REVIEW: We should allow the user to specify the assembly where the resources reside.
                using var resourceStream = GetType().Assembly.GetManifestResourceStream(TemplateResourceName)
                    ?? throw new InvalidOperationException($"Could not find resource {TemplateResourceName} in assembly {GetType().Assembly}");

                using var fs = File.OpenWrite(path);
                resourceStream.CopyTo(fs);
            }
        }

        return new(path, isTempFile && deleteTemporaryFileOnDispose);
    }

    /// <summary>
    /// Get the bicep template as a string. Does not write to disk.
    /// </summary>
    public string GetBicepTemplateString()
    {
        if (TemplateString is not null)
        {
            return TemplateString;
        }

        if (TemplateResourceName is not null)
        {
            using var resourceStream = GetType().Assembly.GetManifestResourceStream(TemplateResourceName)
                ?? throw new InvalidOperationException($"Could not find resource {TemplateResourceName} in assembly {GetType().Assembly}");

            using var reader = new StreamReader(resourceStream, Encoding.UTF8);
            return reader.ReadToEnd();
        }

        if (TemplateFile is null)
        {
            throw new InvalidOperationException("No template source specified.");
        }

        return File.ReadAllText(TemplateFile);
    }

    // TODO: Make the name bicep safe
    /// <summary>
    /// TODO: Doc Comments
    /// </summary>
    /// <returns></returns>
    public string CreateBicepResourceName() => Name.ToLower();

    /// <summary>
    /// Writes the resource to the manifest.
    /// </summary>
    /// <param name="context">The <see cref="ManifestPublishingContext"/>.</param>
    public virtual void WriteToManifest(ManifestPublishingContext context)
    {
        context.Writer.WriteString("type", "azure.bicep.v0");

        using var template = GetBicepTemplateFile(Path.GetDirectoryName(context.ManifestPath), deleteTemporaryFileOnDispose: false);
        var path = template.Path;

        // Write a connection string if it exists.
        context.WriteConnectionString(this);

        // REVIEW: Consider multiple files.
        context.Writer.WriteString("path", context.GetManifestRelativePath(path));

        if (Parameters.Count > 0)
        {
            context.Writer.WriteStartObject("params");
            foreach (var input in Parameters)
            {
                // Used for deferred evaluation of parameter.
                object? inputValue = input.Value is Func<object?> f ? f() : input.Value;

                if (inputValue is JsonNode || inputValue is IEnumerable<string>)
                {
                    context.Writer.WritePropertyName(input.Key);
                    // Write JSON objects to the manifest for JSON node parameters
                    JsonSerializer.Serialize(context.Writer, inputValue);
                    continue;
                }

                var value = input.Value switch
                {
                    IResourceBuilder<ParameterResource> p => p.Resource.ValueExpression,
                    IResourceBuilder<IResourceWithConnectionString> p => p.Resource.ConnectionStringReferenceExpression,
                    BicepOutputReference output => output.ValueExpression,
                    object obj => obj.ToString(),
                    null => ""
                };

                context.Writer.WriteString(input.Key, value);
            }
            context.Writer.WriteEndObject();
        }
    }

    /// <summary>
    /// Known parameters that will be filled in automatically by the host environment.
    /// </summary>
    public static class KnownParameters
    {
        /// <summary>
        /// The principal id of the current user or managed identity.
        /// </summary>
        public static readonly string PrincipalId = "principalId";

        /// <summary>
        /// The principal name of the current user or managed identity.
        /// </summary>
        public static readonly string PrincipalName = "principalName";

        /// <summary>
        /// The principal type of the current user or managed identity. Either 'User' or 'ServicePrincipal'.
        /// </summary>
        public static readonly string PrincipalType = "principalType";

        /// <summary>
        /// The name of the key vault resource used to store secret outputs.
        /// </summary>
        public static readonly string KeyVaultName = "keyVaultName";

        /// <summary>
        /// The location of the resource. This is required for all resources.
        /// </summary>
        public static readonly string Location = "location";

        /// <summary>
        /// The resource id of the log analytics workspace.
        /// </summary>
        public static readonly string LogAnalyticsWorkspaceId = "logAnalyticsWorkspaceId";
    }
}

/// <summary>
/// Represents a bicep template file.
/// </summary>
/// <param name="path">The path to the bicep file.</param>
/// <param name="deleteFileOnDispose">Determines if the file should be deleted on disposal.</param>
public readonly struct BicepTemplateFile(string path, bool deleteFileOnDispose) : IDisposable
{
    /// <summary>
    /// The path to the bicep file.
    /// </summary>
    public string Path { get; } = path;

    /// <summary>
    /// Releases the resources used by the current instance of <see cref="BicepTemplateFile" />.
    /// </summary>
    public void Dispose()
    {
        if (deleteFileOnDispose)
        {
            File.Delete(Path);
        }
    }
}

/// <summary>
/// A reference to a secret output from a bicep template.
/// </summary>
/// <param name="name">The name of the secret output.</param>
/// <param name="resource">The <see cref="AzureBicepResource"/>.</param>
public class BicepSecretOutputReference(string name, AzureBicepResource resource)
{
    /// <summary>
    /// Name of the output.
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// The instance of the bicep resource.
    /// </summary>
    public AzureBicepResource Resource { get; } = resource;

    /// <summary>
    /// The value of the output.
    /// </summary>
    public string? Value
    {
        get
        {
            if (!Resource.SecretOutputs.TryGetValue(Name, out var value))
            {
                throw new InvalidOperationException($"No secret output for {Name}");
            }
            return value;
        }
    }

    /// <summary>
    /// The expression used in the manifest to reference the value of the secret output.
    /// </summary>
    public string ValueExpression => $"{{{Resource.Name}.secretOutputs.{Name}}}";
}

/// <summary>
/// A reference to an output from a bicep template.
/// </summary>
/// <param name="name">The name of the output</param>
/// <param name="resource">The <see cref="AzureBicepResource"/>.</param>
public class BicepOutputReference(string name, AzureBicepResource resource)
{
    /// <summary>
    /// Name of the output.
    /// </summary>
    public string Name { get; } = name;

    /// <summary>
    /// The instance of the bicep resource.
    /// </summary>
    public AzureBicepResource Resource { get; } = resource;

    /// <summary>
    /// The value of the output.
    /// </summary>
    public string? Value
    {
        get
        {
            if (!Resource.Outputs.TryGetValue(Name, out var value))
            {
                throw new InvalidOperationException($"No output for {Name}");
            }

            return value;
        }
    }

    /// <summary>
    /// The expression used in the manifest to reference the value of the output.
    /// </summary>
    public string ValueExpression => $"{{{Resource.Name}.outputs.{Name}}}";
}
