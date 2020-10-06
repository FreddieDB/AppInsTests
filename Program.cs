using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.DependencyCollector;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Extensibility.W3C;
using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace FdB.AppInsightsTesting
{
	class Program
    {
        static void Main(string[] args)
        {
            string appInsightsInstrKey = "XXX"; // <---- Change this!
            
            if (appInsightsInstrKey.Equals("XXX")) throw new Exception("Change the hard coded App Insights Instrumentation Key to a valid and configured AppInsights instrumentation key");

            Console.WriteLine("Diagnostic App Insights Test starting...");

            // Config
            TelemetryConfiguration configuration = TelemetryConfiguration.CreateDefault();
            configuration.InstrumentationKey = appInsightsInstrKey;
            configuration.TelemetryInitializers.Add(new HttpDependenciesParsingTelemetryInitializer());
           //configuration.TelemetryInitializers.Add(new OperationCorrelationTelemetryInitializer());

            DependencyTrackingTelemetryModule module = new DependencyTrackingTelemetryModule();
            module.ExcludeComponentCorrelationHttpHeadersOnDomains.Add("core.windows.net");
            module.Initialize(configuration);

            // Client
            var telemetryClient = new TelemetryClient(configuration);
            telemetryClient.Context.User.Id = "test@test7789.testdomain.com";
            telemetryClient.Context.Cloud.RoleName = "AppInsights FdBTests";
            telemetryClient.Context.Component.Version = "vTest1.01.02";
            

            // Dependency Tel
            DependencyTelemetry dt = new DependencyTelemetry();
            dt.Properties.Add("XX", "YY");
            dt.Name = "DT FdB 47";
            IOperationHolder<DependencyTelemetry> dtOp = telemetryClient.StartOperation<DependencyTelemetry>(dt);

            // Event Tel
            EventTelemetry et = new EventTelemetry("FdB.AppInsights.UnitTest v202W3COff");
            et.Properties.Add("Site", "FDY");
            et.Properties.Add("Version", "FDBTest111");
            et.Context.Operation.ParentId = dt.Context.Operation.Id;
            telemetryClient.TrackEvent(et);
            

            // Auto dependency tracking happening here
            using (var httpClient = new HttpClient())
            {
                // Http dependency is automatically tracked!
                HttpResponseMessage msg = httpClient.GetAsync("https://www.bing.com/").GetAwaiter().GetResult();

                if (msg.RequestMessage.Headers.Contains("traceparent"))
                {
                    Console.WriteLine("W3C Traceparent found in Request Message Headers!");
                }
                else
                {
                    Console.WriteLine("W3C Traceparent NOT found in Request Message Headers!");
                }

                if (msg.RequestMessage.Headers.Contains("Request-Id"))
                {
                    Console.WriteLine("Legacy Request-Id found in Request Message Headers!");
                }
                else
                {
                    Console.WriteLine("Legacy Request-Id NOT found in Request Message Headers!");
                }
            }

            // Track random exception     
            telemetryClient.TrackException(new Exception("Test exception FdB 3"));

            // Cleanup
            telemetryClient.StopOperation(dtOp);
            telemetryClient.Flush();

            Console.ReadLine();
        }
    }
}
