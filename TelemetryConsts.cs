namespace OpenTelemetryWithJaeger;

public class TelemetryConsts
{
    public const string ActivitySource = "OpenTelemetry.Demo.Jaeger";
    public const string ServiceName = "TestApp";
    public const string ServiceVersion = "1.0.0";
    public const string OtplUri = "http://localhost:4317/";
    public const string ParentActivity = "JaegerDemo";
    public const string Activity = "RunActivity";
    public const string EndpointWithSleep = "https://httpstat.us/200?sleep=1000";
    public const string Endpoint = "https://httpstat.us/301";
}