namespace EventBooking.ConsoleApp.Ui;

/// <summary>
/// The terminal, as the screens see it. Screens depend on this rather than on
/// <see cref="System.Console"/> so that not one of them contains an untestable direct call to the
/// outside world - and so the whole presentation layer could be swapped for another shell.
/// </summary>
public interface IUserInterface
{
    void Header(string title);

    void Section(string title);

    void Write(string text);

    void Blank();

    void Muted(string text);

    void Success(string text);

    void Warning(string text);

    void Error(string text);

    /// <summary>A label/value pair, aligned into a column.</summary>
    void Bullet(string label, string value);

    void Table(IReadOnlyList<string> headers, IReadOnlyList<IReadOnlyList<string>> rows);

    /// <summary>
    /// True once standard input has reached its end - the app is being piped or scripted rather than
    /// typed into, and there is nobody left to answer a prompt.
    /// </summary>
    bool InputClosed { get; }

    /// <summary>Reads a line. Returns <see langword="null"/> when the user just pressed Enter.</summary>
    string? Ask(string prompt);

    string? AskRequired(string prompt);

    int? AskInteger(string prompt, int minimum = int.MinValue, int maximum = int.MaxValue);

    decimal? AskDecimal(string prompt);

    bool Confirm(string prompt);

    /// <summary>Numbered picker. Returns <see langword="default"/> when the user backs out.</summary>
    T? Choose<T>(string prompt, IReadOnlyList<T> options, Func<T, string> describe);

    void Pause();

    void Clear();

    /// <summary>Runs an operation, reporting a refused request as a message rather than a crash.</summary>
    bool Try(Action action);
}
