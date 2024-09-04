using Microsoft.Extensions.Logging;

namespace Elib2EbookCli;

public class ConsoleLogger : ILogger {
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) {
        Console.WriteLine($"{formatter(state, exception)}");
    }

    bool ILogger.IsEnabled(LogLevel logLevel) {
        return true;
    }

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => default!;
}