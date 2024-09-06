namespace Elib2EbookWeb.Misc;

public class ActionLogger : ILogger {
    private readonly Action<string?> _action;

    public ActionLogger(Action<string?> action) {
        _action = action;
    }
    
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => default!;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) {
        _action($"{formatter(state, exception)}");
    }
}