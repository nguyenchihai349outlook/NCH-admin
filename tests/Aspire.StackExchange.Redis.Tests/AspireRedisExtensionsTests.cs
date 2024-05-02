// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using Xunit;

namespace Aspire.StackExchange.Redis.Tests;

public class AspireRedisExtensionsTests
{
    [ConditionalFact]
    public void AllowsConfigureConfigurationOptions()
    {
        AspireRedisHelpers.SkipIfCanNotConnectToServer();

        var builder = Host.CreateEmptyApplicationBuilder(null);
        AspireRedisHelpers.PopulateConfiguration(builder.Configuration);

        builder.AddRedis("redis");

        builder.Services.Configure<ConfigurationOptions>(options =>
        {
            options.User = "aspire-test-user";
        });

        var host = builder.Build();
        var connection = host.Services.GetRequiredService<IConnectionMultiplexer>();

        Assert.Contains("aspire-test-user", connection.Configuration);
    }

    [ConditionalTheory]
    [InlineData(true)]
    [InlineData(false)]
    public void ReadsFromConnectionStringsCorrectly(bool useKeyed)
    {
        AspireRedisHelpers.SkipIfCanNotConnectToServer();

        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:myredis", AspireRedisHelpers.TestingEndpoint)
        ]);

        if (useKeyed)
        {
            builder.AddKeyedRedis("myredis");
        }
        else
        {
            builder.AddRedis("myredis");
        }

        var host = builder.Build();
        var connection = useKeyed ?
            host.Services.GetRequiredKeyedService<IConnectionMultiplexer>("myredis") :
            host.Services.GetRequiredService<IConnectionMultiplexer>();

        Assert.Contains(AspireRedisHelpers.TestingEndpoint, connection.Configuration);
    }

    [ConditionalTheory]
    [InlineData(true)]
    [InlineData(false)]
    public void ConnectionStringCanBeSetInCode(bool useKeyed)
    {
        AspireRedisHelpers.SkipIfCanNotConnectToServer();

        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>("ConnectionStrings:redis", "unused")
        ]);

        static void SetConnectionString(StackExchangeRedisSettings settings) => settings.ConnectionString = AspireRedisHelpers.TestingEndpoint;
        if (useKeyed)
        {
            builder.AddKeyedRedis("redis", SetConnectionString);
        }
        else
        {
            builder.AddRedis("redis", SetConnectionString);
        }

        var host = builder.Build();
        var connection = useKeyed ?
            host.Services.GetRequiredKeyedService<IConnectionMultiplexer>("redis") :
            host.Services.GetRequiredService<IConnectionMultiplexer>();

        Assert.Contains(AspireRedisHelpers.TestingEndpoint, connection.Configuration);
        // the connection string from config should not be used since code set it explicitly
        Assert.DoesNotContain("unused", connection.Configuration);
    }

    [ConditionalTheory]
    [InlineData(true)]
    [InlineData(false)]
    public void ConnectionNameWinsOverConfigSection(bool useKeyed)
    {
        AspireRedisHelpers.SkipIfCanNotConnectToServer();

        var builder = Host.CreateEmptyApplicationBuilder(null);

        var key = useKeyed ? "redis" : null;
        builder.Configuration.AddInMemoryCollection([
            new KeyValuePair<string, string?>(ConformanceTests.CreateConfigKey("Aspire:StackExchange:Redis", key, "ConnectionString"), "unused"),
            new KeyValuePair<string, string?>("ConnectionStrings:redis", AspireRedisHelpers.TestingEndpoint)
        ]);

        if (useKeyed)
        {
            builder.AddKeyedRedis("redis");
        }
        else
        {
            builder.AddRedis("redis");
        }

        var host = builder.Build();
        var connection = useKeyed ?
            host.Services.GetRequiredKeyedService<IConnectionMultiplexer>("redis") :
            host.Services.GetRequiredService<IConnectionMultiplexer>();

        Assert.Contains(AspireRedisHelpers.TestingEndpoint, connection.Configuration);
        // the connection string from config should not be used since it was found in ConnectionStrings
        Assert.DoesNotContain("unused", connection.Configuration);
    }

    public static IEnumerable<object[]> AbortOnConnectFailData =>
    [
        [true, GetDefaultConfiguration(), false],
        [false, GetDefaultConfiguration(), false],

        [true, GetSetsTrueConfig(true), true],
        [false, GetSetsTrueConfig(false), true],

        [true, GetConnectionString(abortConnect: true), true],
        [false, GetConnectionString(abortConnect: true), true],
        [true, GetConnectionString(abortConnect: false), false],
        [false, GetConnectionString(abortConnect: false), false],
    ];

    private static IEnumerable<KeyValuePair<string, string?>> GetDefaultConfiguration() =>
    [
        new KeyValuePair<string, string?>("ConnectionStrings:redis", AspireRedisHelpers.TestingEndpoint)
    ];

    private static IEnumerable<KeyValuePair<string, string?>> GetSetsTrueConfig(bool useKeyed) =>
    [
        new KeyValuePair<string, string?>("ConnectionStrings:redis", AspireRedisHelpers.TestingEndpoint),
        new KeyValuePair<string, string?>(ConformanceTests.CreateConfigKey("Aspire:StackExchange:Redis", useKeyed ? "redis" : null, "ConfigurationOptions:AbortOnConnectFail"), "true")
    ];

    private static IEnumerable<KeyValuePair<string, string?>> GetConnectionString(bool abortConnect) =>
    [
        new KeyValuePair<string, string?>("ConnectionStrings:redis", $"{AspireRedisHelpers.TestingEndpoint},abortConnect={(abortConnect ? "true" : "false")}")
    ];

    [Theory]
    [MemberData(nameof(AbortOnConnectFailData))]
    public void AbortOnConnectFailDefaults(bool useKeyed, IEnumerable<KeyValuePair<string, string?>> configValues, bool expectedAbortOnConnect)
    {
        var builder = Host.CreateEmptyApplicationBuilder(null);
        builder.Configuration.AddInMemoryCollection(configValues);

        if (useKeyed)
        {
            builder.AddKeyedRedis("redis");
        }
        else
        {
            builder.AddRedis("redis");
        }

        var host = builder.Build();
        var options = useKeyed ?
            host.Services.GetRequiredService<IOptionsMonitor<ConfigurationOptions>>().Get("redis") :
            host.Services.GetRequiredService<IOptions<ConfigurationOptions>>().Value;

        Assert.Equal(expectedAbortOnConnect, options.AbortOnConnectFail);
    }
}
