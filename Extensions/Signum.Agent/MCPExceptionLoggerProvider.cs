using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ModelContextProtocol.Server;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace Signum.Agent;

public class MCPExceptionLoggerProvider : ILoggerProvider
{
    private readonly ConcurrentDictionary<string, SignumExceptionLogger> _loggers = new();

    public ILogger CreateLogger(string categoryName)
    {
        if (categoryName == typeof(McpServerTool).FullName)
            return _loggers.GetOrAdd(categoryName, name => new SignumExceptionLogger(name));

        return NullLogger.Instance;
    }

    public void Dispose() { }
}

public class SignumExceptionLogger : ILogger
{
    private readonly string _name;
    public List<string> Errors { get; } = new();

    public SignumExceptionLogger(string name)
    {
        _name = name;
    }

    public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Error;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (IsEnabled(logLevel) && exception != null)
        {
            exception.LogException(e =>
            {
                e.ControllerName = this._name;
            });
        }
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
}
