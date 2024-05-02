// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Options;

namespace Aspire.Dashboard.Configuration;

public sealed class ValidateDashboardOptions : IValidateOptions<DashboardOptions>
{
    public ValidateOptionsResult Validate(string? name, DashboardOptions options)
    {
        var errorMessages = new List<string>();

        if (!options.Frontend.TryParseOptions(out var frontendParseErrorMessage))
        {
            errorMessages.Add(frontendParseErrorMessage);
        }

        if (!options.Otlp.TryParseOptions(out var otlpParseErrorMessage))
        {
            errorMessages.Add(otlpParseErrorMessage);
        }

        if (!options.ResourceServiceClient.TryParseOptions(out var resourceServiceClientParseErrorMessage))
        {
            errorMessages.Add(resourceServiceClientParseErrorMessage);
        }

        switch (options.Otlp.AuthMode)
        {
            case OtlpAuthMode.Unsecured:
                break;
            case OtlpAuthMode.ApiKey:
                if (string.IsNullOrEmpty(options.Otlp.PrimaryApiKey))
                {
                    errorMessages.Add("PrimaryApiKey is required when OTLP authentication mode is API key. Specify a Dashboard:Otlp:PrimaryApiKey value.");
                }
                break;
            case OtlpAuthMode.ClientCertificate:
                break;
            case null:
                errorMessages.Add($"OTLP endpoint authentication is not configured. Either specify DOTNET_DASHBOARD_INSECURE_ALLOW_ANONYMOUS with a value of true, or specify Dashboard:Otlp:AuthMode. Possible values: {string.Join(", ", typeof(OtlpAuthMode).GetEnumNames())}");
                break;
            default:
                errorMessages.Add($"Unexpected OTLP authentication mode: {options.Otlp.AuthMode}");
                break;
        }

        if (options.ResourceServiceClient.GetUri() != null)
        {
            switch (options.ResourceServiceClient.AuthMode)
            {
                case ResourceClientAuthMode.Unsecured:
                    break;
                case ResourceClientAuthMode.Certificate:

                    switch (options.ResourceServiceClient.ClientCertificates.Source)
                    {
                        case DashboardClientCertificateSource.File:
                            if (string.IsNullOrEmpty(options.ResourceServiceClient.ClientCertificates.FilePath))
                            {
                                errorMessages.Add("Dashboard:ResourceServiceClient:ClientCertificate:Source is \"File\", but no Dashboard:ResourceServiceClient:ClientCertificate:FilePath is configured.");
                            }
                            break;
                        case DashboardClientCertificateSource.KeyStore:
                            if (string.IsNullOrEmpty(options.ResourceServiceClient.ClientCertificates.Subject))
                            {
                                errorMessages.Add("Dashboard:ResourceServiceClient:ClientCertificate:Source is \"KeyStore\", but no Dashboard:ResourceServiceClient:ClientCertificate:Subject is configured.");
                            }
                            break;
                        default:
                            errorMessages.Add($"Unexpected resource service client certificate source: {options.Otlp.AuthMode}");
                            break;
                    }
                    break;
                default:
                    errorMessages.Add($"Unexpected resource service client authentication mode: {options.Otlp.AuthMode}");
                    break;
            }
        }

        return errorMessages.Count > 0
            ? ValidateOptionsResult.Fail(errorMessages)
            : ValidateOptionsResult.Success;
    }
}
