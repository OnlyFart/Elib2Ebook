using System.Text;

namespace Elib2EbookWeb.Misc;

public class SbLogger : ILogger {
    private readonly StringBuilder _builder;
    private readonly Action _action;

    public SbLogger(StringBuilder builder, Action action) {
        _builder = builder;
        _action = action;
    }
    
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => default!;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) {
        _builder.AppendLine($"{formatter(state, exception)}");
        _action();
    }
}