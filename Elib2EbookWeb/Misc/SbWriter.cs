using System.Text;

namespace Elib2EbookWeb.Misc;

public class SbLogger : ILogger {
    public readonly StringBuilder Builder;
    private readonly Action _action;

    public SbLogger(Action action) : this(action, new StringBuilder()) {

    }
    
    public SbLogger(Action action, StringBuilder builder) {
        _action = action;
        Builder = builder;
    }
    
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => default!;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) {
        Builder.AppendLine($"{formatter(state, exception)}");
        _action();
    }
}