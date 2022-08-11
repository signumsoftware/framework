using System.Threading;
using System.Collections.Specialized;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Http.Extensions;
using System.Net;
using Microsoft.AspNetCore.Http;

namespace Signum.Entities.Basics;

[EntityKind(EntityKind.System, EntityData.Transactional), TicksColumn(false)]
public class ExceptionEntity : Entity
{
    public const string ExceptionDataKey = "exceptionEntity";

#pragma warning disable CS8618 // Non-nullable field is uninitialized.
    public ExceptionEntity() { }

    public ExceptionEntity(ClientExceptionModel clientException, HttpContext httpContext) 
    {
        if (httpContext != null)
        {
            var req = httpContext.Request;
            var connFeature = httpContext.Features.Get<IHttpConnectionFeature>()!;

            this.ExceptionType = clientException.ExceptionType;
            this.ExceptionMessage = clientException.TypeErrorMessage;
            this.StackTrace = new BigStringEmbedded(clientException.TypeErrorStack);
            this.ThreadId = -1;
            this.UserAgent = Try(300, () => req.Headers["User-Agent"].FirstOrDefault());
            this.RequestUrl = Try(int.MaxValue, () => req.GetDisplayUrl());
            this.UrlReferer = Try(int.MaxValue, () => req.Headers["Referer"].ToString());
            this.UserHostAddress = Try(100, () => connFeature.RemoteIpAddress?.ToString());
            this.UserHostName = Try(100, () => connFeature.RemoteIpAddress == null ? null : Dns.GetHostEntry(connFeature.RemoteIpAddress).HostName);

            this.MachineName = System.Environment.MachineName;
            this.ApplicationName = AppDomain.CurrentDomain.FriendlyName;
            this.IsClientSide = true;

            this.Form = new BigStringEmbedded();
            this.QueryString = new BigStringEmbedded();
            this.Session = new BigStringEmbedded();
            this.Data = new BigStringEmbedded();
        }
    }

    public ExceptionEntity(Exception ex)
    {
        this.ExceptionType = ex.GetType().Name;
        this.ExceptionMessage = ex.Message!;
        this.StackTrace = new BigStringEmbedded(ex.StackTrace!);
        this.ThreadId = Thread.CurrentThread.ManagedThreadId;
        ex.Data[ExceptionDataKey] = this;
        this.MachineName = System.Environment.MachineName;
        this.ApplicationName = AppDomain.CurrentDomain.FriendlyName;
    }
#pragma warning restore CS8618 // Non-nullable field is uninitialized.

    public DateTime CreationDate { get; private set; } = Clock.Now;

    [ForceNotNullable, DbType(Size = 100)]
    public string? ExceptionType { get; set; }

    [DbType(Size = int.MaxValue)]
    string exceptionMessage;
    public string ExceptionMessage
    {
        get { return exceptionMessage; }
        set
        {
            if (Set(ref exceptionMessage, value))
                ExceptionMessageHash = value?.GetHashCode() ?? 0;
        }
    }

    public int ExceptionMessageHash { get; private set; }

    BigStringEmbedded stackTrace = new BigStringEmbedded();
    [NotifyChildProperty]
    public BigStringEmbedded StackTrace
    {
        get { return stackTrace; }
        set
        {
            if (Set(ref stackTrace, value))
                StackTraceHash = value?.Text?.GetHashCode() ?? 0;
        }
    }

    public int StackTraceHash { get; private set; }

    public int ThreadId { get; set; }

    public Lite<IUserEntity>? User { get; set; }

    [DbType(Size = 100)]
    public string? Environment { get; set; }

    [DbType(Size = 100)]
    public string? Version { get; set; }

    [DbType(Size = 300)]
    public string? UserAgent { get; set; }

    [DbType(Size = int.MaxValue)]
    public string? RequestUrl { get; set; }

    [DbType(Size = 100)]
    public string? ControllerName { get; set; }

    [DbType(Size = 100)]
    public string? ActionName { get; set; }

    [DbType(Size = int.MaxValue)]
    public string? UrlReferer { get; set; }

    [DbType(Size = 100)]
    public string? MachineName { get; set; }

    [DbType(Size = 100)]
    public string? ApplicationName { get; set; }

    [DbType(Size = 100)]
    public string? UserHostAddress { get; set; }

    [DbType(Size = 100)]
    public string? UserHostName { get; set; }

    [NotifyChildProperty]
    public BigStringEmbedded Form { get; set; } = new BigStringEmbedded();

    [NotifyChildProperty]
    public BigStringEmbedded QueryString { get; set; } = new BigStringEmbedded();

    [NotifyChildProperty]
    public BigStringEmbedded Session { get; set; } = new BigStringEmbedded();

    [NotifyChildProperty]
    public BigStringEmbedded Data { get; set; } = new BigStringEmbedded();

    public int HResult { get; internal set; }

    public bool Referenced { get; set; }

    public bool IsClientSide { get; set; }

    public override string ToString()
    {
        return "{0}: {1}".FormatWith(ExceptionType, exceptionMessage).Etc(200);
    }

    public static string Dump(NameValueCollection nameValueCollection)
    {
        return nameValueCollection.Cast<string>().ToString(key => key + ": " + nameValueCollection[key], "\r\n");
    }

    private static string? Try(int size, Func<string?> getValue)
    {
        try
        {
            return getValue()?.TryStart(size);
        }
        catch (Exception e)
        {
            return (e.GetType().Name + ":" + e.Message).TryStart(size);
        }
    }
}


public class DeleteLogParametersEmbedded : EmbeddedEntity
{
    [PreserveOrder]
    [NoRepeatValidator]
    public MList<DeleteLogsTypeOverridesEmbedded> DeleteLogs { get; set; } = new MList<DeleteLogsTypeOverridesEmbedded>();

    public DateTime? GetDateLimitDelete(TypeEntity type)
    {
        var moreThan = DeleteLogs.SingleOrDefaultEx(a => a.Type.Is(type))?.DeleteLogsOlderThan;

        if (moreThan == null)
            return null;

        return moreThan == 0 ? Clock.Now.TrimToHours() : Clock.Now.Date.AddDays(-moreThan.Value);
    }

    public DateTime? GetDateLimitDeleteWithExceptions(TypeEntity type)
    {
        var moreThan = DeleteLogs.SingleOrDefaultEx(a => a.Type.Is(type))?.DeleteLogsWithExceptionsOlderThan;

        if (moreThan == null)
            return null;

        return moreThan.Value == 0 ? Clock.Now.TrimToHours() : Clock.Now.Date.AddDays(-moreThan.Value);
    }

    public int ChunkSize { get; set; } = 1000;

    public int MaxChunks { get; set; } = 20;

    [Unit("ms")]
    public int? PauseTime { get; set; } = 5000;
}

#pragma warning disable CS8618 // Non-nullable field is uninitialized.
public class DeleteLogsTypeOverridesEmbedded : EmbeddedEntity
{
    public Lite<TypeEntity> Type { get; set; }

    [Unit("Days"), NumberIsValidator(ComparisonType.GreaterThanOrEqualTo, 0)]
    public int? DeleteLogsOlderThan { get; set; } = 30 * 6;

    [Unit("Days"), NumberIsValidator(ComparisonType.GreaterThanOrEqualTo, 0)]
    public int? DeleteLogsWithExceptionsOlderThan { get; set; } = 30 * 2;

    protected internal override string? PropertyValidation(PropertyInfo pi)
    {
        if (pi.Name == nameof(DeleteLogsOlderThan))
        {
            if (DeleteLogsOlderThan.HasValue && DeleteLogsWithExceptionsOlderThan.HasValue && DeleteLogsOlderThan.Value < DeleteLogsWithExceptionsOlderThan.Value)
                return ValidationMessage._0ShouldBeGreaterThan1.NiceToString(pi.NiceName(), NicePropertyName(() => DeleteLogsWithExceptionsOlderThan));
        }

        return base.PropertyValidation(pi);
    }
}

public class ClientExceptionModel : ModelEntity
{
    public string TypeErrorMessage { get; set; }

    public string? TypeErrorStack { get; set; }

    public string? TypeErrorName { get; set; }

    public string? ExceptionType { get; set; }
}


#pragma warning restore CS8618 // Non-nullable field is uninitialized.
