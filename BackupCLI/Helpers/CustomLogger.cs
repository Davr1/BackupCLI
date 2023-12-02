using Microsoft.Extensions.Logging;

namespace BackupCLI.Helpers;

public class CustomLogger(string path) : ILogger
{
    private readonly StreamWriter logFile = new(path, append: true) { AutoFlush = true };

    public IDisposable BeginScope<TState>(TState state) where TState : notnull => default!;

    public bool IsEnabled(LogLevel logLevel) => true;

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
    {
        string message = $"[{DateTime.Now}] [{logLevel}] {formatter(state, exception)}";

        Console.WriteLine(message);

        logFile.WriteLine(message);
    }

    public void Dispose() => logFile.Dispose();

    public void Error(Exception e) 
        => this.LogError(0, e, e.Message);

    public void Info(string message)
        => this.LogInformation(message);
}