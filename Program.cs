using System.Diagnostics;
using Microsoft.Extensions.Logging;

#if NETFRAMEWORK
using System.Net.Http;
#endif
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Formatting.Json;

namespace OpenTelemetryWithJaeger
{
    public class Program
    {
        private static readonly ActivitySource MyActivitySource = new(TelemetryConsts.ActivitySource);

        public static async Task Main()
        {
            var openTelemetryLoggerOptions = new OpenTelemetryLoggerOptions
            {
                IncludeFormattedMessage = true,
                IncludeScopes = true,
                ParseStateValues = true
            };

            Log.Logger = new LoggerConfiguration()
                //.WriteTo.Console(new JsonFormatter())
                .WriteTo.OpenTelemetry(options => 
                {
                    options.Endpoint = TelemetryConsts.OtplUri;
                    options.Protocol = Serilog.Sinks.OpenTelemetry.OtlpProtocol.Grpc;
                })
                .CreateLogger();

            var openTelemetryLoggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddOpenTelemetry(options =>
                {
                    options.IncludeFormattedMessage = true;
                    options.IncludeScopes = true;
                    options.ParseStateValues = true;
                    options.AddOtlpExporter(o =>
                    {
                        o.Endpoint = new Uri(TelemetryConsts.OtplUri);
                        o.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
                    });
                });
            });

            var logger = openTelemetryLoggerFactory.CreateLogger<Program>();

            using var tracerProvider = Sdk.CreateTracerProviderBuilder()
                .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService(
                    serviceName: TelemetryConsts.ServiceName,
                    serviceVersion: TelemetryConsts.ServiceVersion))
                .AddSource(TelemetryConsts.ActivitySource)
                .AddHttpClientInstrumentation()
                //.AddConsoleExporter()
                .AddOtlpExporter(o =>
                {
                    o.Endpoint = new Uri(TelemetryConsts.OtplUri);
                    o.Protocol = OpenTelemetry.Exporter.OtlpExportProtocol.Grpc;
                })
                .Build();

            using var parent = MyActivitySource.StartActivity(TelemetryConsts.ParentActivity);

            using (var client = new HttpClient())
            {
                using (var slow = MyActivitySource.StartActivity(TelemetryConsts.Activity + "_1"))
                {
                    logger.LogInformation("Starting slow request...");
                    await client.GetStringAsync(TelemetryConsts.EndpointWithSleep);
                    await client.GetStringAsync(TelemetryConsts.EndpointWithSleep);
                    logger.LogInformation("Slow request completed.");
                }

                using (var fast = MyActivitySource.StartActivity(TelemetryConsts.Activity + "_2"))
                {
                    logger.LogInformation("Starting fast request...");
                    await client.GetStringAsync(TelemetryConsts.Endpoint);
                    logger.LogInformation("Fast request completed.");
                }
            }
        }
    }
}

