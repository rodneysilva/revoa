using System.Diagnostics;
using Serilog.Core;
using Serilog.Events;

namespace Revoa.Api.Observability;

// Correlaciona eventos de log do Serilog com spans do OpenTelemetry: adiciona
// TraceId/SpanId (W3C) do Activity atual a cada evento de log, permitindo cruzar
// logs e traces no backend (ex.: Loki/Tempo, Jaeger). Sem dependência extra.
internal sealed class ActivityLogEnricher : ILogEventEnricher
{
    public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
    {
        var activity = Activity.Current;
        if (activity is null)
        {
            return;
        }

        if (!activity.TraceId.Equals(default))
        {
            logEvent.AddPropertyIfAbsent(
                propertyFactory.CreateProperty("TraceId", activity.TraceId.ToHexString()));
        }

        if (!activity.SpanId.Equals(default))
        {
            logEvent.AddPropertyIfAbsent(
                propertyFactory.CreateProperty("SpanId", activity.SpanId.ToHexString()));
        }
    }
}
