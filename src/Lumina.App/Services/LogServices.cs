using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Microsoft.Extensions.Logging;
using System.IO.Compression;
using System.Text;

namespace Lumina.App.Services;

public interface ILogFileService
{
    string LogsDirectory { get; }
    string CurrentLogFilePath { get; }
    Microsoft.Extensions.Logging.LogLevel MinimumLevel { get; set; }
    void EnsureLogFileExists();
    void WriteLine(string message);
}

public sealed class LogFileService : ILogFileService
{
    private readonly object _lock = new();
    private StreamWriter? _writer;

    public LogFileService(string? baseDirectory = null)
    {
        var root = baseDirectory ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Lumina");

        LogsDirectory = Path.Combine(root, "Logs");
        var date = DateTime.Now.ToString("yyyy-MM-dd");
        CurrentLogFilePath = Path.Combine(LogsDirectory, $"lumina-{date}.log");
        MinimumLevel = Microsoft.Extensions.Logging.LogLevel.Information;
    }

    public string LogsDirectory { get; }

    public string CurrentLogFilePath { get; }

    public Microsoft.Extensions.Logging.LogLevel MinimumLevel { get; set; }

    public void EnsureLogFileExists()
    {
        Directory.CreateDirectory(LogsDirectory);
        if (!File.Exists(CurrentLogFilePath))
        {
            using var _ = File.Create(CurrentLogFilePath);
        }
    }

    public void WriteLine(string message)
    {
        lock (_lock)
        {
            Directory.CreateDirectory(LogsDirectory);
            _writer ??= new StreamWriter(
                new FileStream(
                    CurrentLogFilePath,
                    FileMode.Append,
                    FileAccess.Write,
                    FileShare.ReadWrite),
                new UTF8Encoding(true))
            { AutoFlush = true };
            _writer.WriteLine(message);
        }
    }
}

public sealed class LogFileLoggerProvider : ILoggerProvider
{
    private readonly LogFileService _logService;

    public LogFileLoggerProvider(LogFileService logService)
    {
        _logService = logService;
    }

    public ILogger CreateLogger(string categoryName) => new LogFileLogger(_logService, categoryName);

    public void Dispose()
    {
    }
}

public interface ILogExportService
{
    Task ExportLogsAsync(
        DateTimeOffset? startDate,
        DateTimeOffset? endDate,
        CancellationToken cancellationToken = default);
}

public sealed class LogExportService : ILogExportService
{
    private readonly ILogFileService _logService;

    public LogExportService(ILogFileService logService)
    {
        _logService = logService;
    }

    public async Task ExportLogsAsync(
        DateTimeOffset? startDate,
        DateTimeOffset? endDate,
        CancellationToken cancellationToken = default)
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            return;

        var window = desktop.MainWindow;
        if (window is null)
            return;

        _logService.EnsureLogFileExists();

        var start = startDate?.Date;
        var end = endDate?.Date;
        if (start is null && end is not null)
            start = end;
        if (end is null && start is not null)
            end = start;
        if (start is not null && end is not null && start > end)
            (start, end) = (end, start);

        var logFiles = Directory.Exists(_logService.LogsDirectory)
            ? Directory.GetFiles(_logService.LogsDirectory, "*.log", SearchOption.TopDirectoryOnly)
            : [];

        var selectedLogs = new List<(DateTime Date, string Path)>();
        foreach (var path in logFiles)
        {
            if (!TryGetLogDate(path, out var logDate))
                continue;

            if (start is null && end is null)
            {
                selectedLogs.Add((logDate, path));
                continue;
            }

            if (start is not null && end is not null && logDate >= start.Value && logDate <= end.Value)
                selectedLogs.Add((logDate, path));
        }

        if (selectedLogs.Count == 0)
            return;

        selectedLogs.Sort((left, right) => left.Date.CompareTo(right.Date));

        if (start is not null && end is not null && start.Value == end.Value)
        {
            var saveFile = await window.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                SuggestedFileName = $"Lumina-log-{start:yyyyMMdd}.log",
                FileTypeChoices =
                [
                    new FilePickerFileType("Log") { Patterns = ["*.log"] }
                ]
            });

            if (saveFile is null)
                return;

            await using var output = await saveFile.OpenWriteAsync();
            await using var input = new FileStream(selectedLogs[0].Path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            await input.CopyToAsync(output, cancellationToken);
            return;
        }

        var suggestedName = start is not null && end is not null
            ? $"Lumina-logs-{start:yyyyMMdd}-{end:yyyyMMdd}.zip"
            : $"Lumina-logs-{DateTime.Now:yyyyMMdd-HHmmss}.zip";

        var saveZip = await window.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            SuggestedFileName = suggestedName,
            FileTypeChoices =
            [
                new FilePickerFileType("Zip") { Patterns = ["*.zip"] }
            ]
        });

        if (saveZip is null)
            return;

        var tempPath = Path.Combine(Path.GetTempPath(), $"lumina-logs-{Guid.NewGuid():N}.zip");
        try
        {
            using (var tempStream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
            using (var archive = new ZipArchive(tempStream, ZipArchiveMode.Create))
            {
                foreach (var (_, path) in selectedLogs)
                {
                    var entry = archive.CreateEntry(Path.GetFileName(path), CompressionLevel.Optimal);
                    await using var entryStream = entry.Open();
                    await using var fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    await fileStream.CopyToAsync(entryStream, cancellationToken);
                }
            }

            await using var output = await saveZip.OpenWriteAsync();
            await using var input = new FileStream(tempPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            await input.CopyToAsync(output, cancellationToken);
        }
        finally
        {
            if (File.Exists(tempPath))
                File.Delete(tempPath);
        }
    }

    private static bool TryGetLogDate(string path, out DateTime date)
    {
        var name = Path.GetFileNameWithoutExtension(path);
        const string prefix = "lumina-";
        if (!name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            date = default;
            return false;
        }

        var datePart = name[prefix.Length..];
        return DateTime.TryParseExact(datePart, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date);
    }
}

internal sealed class LogFileLogger : ILogger
{
    private readonly LogFileService _logService;
    private readonly string _categoryName;

    public LogFileLogger(LogFileService logService, string categoryName)
    {
        _logService = logService;
        _categoryName = categoryName;
    }

    public IDisposable BeginScope<TState>(TState state) where TState : notnull => NullScope.Instance;

    public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => logLevel >= _logService.MinimumLevel;

    public void Log<TState>(
        Microsoft.Extensions.Logging.LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
            return;

        var message = formatter(state, exception);
        var line = $"{DateTimeOffset.Now:O} [{logLevel}] {_categoryName} {message}";
        if (exception is not null)
            line = $"{line}{Environment.NewLine}{exception}";

        _logService.WriteLine(line);
    }
}

internal sealed class NullScope : IDisposable
{
    public static NullScope Instance { get; } = new();

    public void Dispose()
    {
    }
}
