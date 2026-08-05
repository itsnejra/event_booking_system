using System.Globalization;
using EventBooking.Domain.Exceptions;

namespace EventBooking.ConsoleApp.Ui;

/// <summary>
/// <see cref="IUserInterface"/> on top of <see cref="Console"/>. The only type in the solution that
/// talks to the terminal.
/// </summary>
public sealed class ConsoleUi : IUserInterface
{
    private const int MenuIndent = 2;

    public bool InputClosed { get; private set; }

    public void Header(string title)
    {
        ArgumentNullException.ThrowIfNull(title);

        Console.WriteLine();
        WriteInColour(ConsoleColor.Cyan, title.ToUpperInvariant());
        WriteInColour(ConsoleColor.DarkCyan, new string('=', title.Length));
    }

    public void Section(string title)
    {
        Console.WriteLine();
        WriteInColour(ConsoleColor.White, title);
    }

    public void Write(string text) => Console.WriteLine(text);

    public void Blank() => Console.WriteLine();

    public void Muted(string text) => WriteInColour(ConsoleColor.DarkGray, text);

    public void Success(string text) => WriteInColour(ConsoleColor.Green, text);

    public void Warning(string text) => WriteInColour(ConsoleColor.Yellow, text);

    public void Error(string text) => WriteInColour(ConsoleColor.Red, text);

    public void Bullet(string label, string value) => Console.WriteLine($"  {label,-22}{value}");

    public void Table(IReadOnlyList<string> headers, IReadOnlyList<IReadOnlyList<string>> rows)
    {
        ArgumentNullException.ThrowIfNull(headers);
        ArgumentNullException.ThrowIfNull(rows);

        // Each column is as wide as its widest cell, header included.
        var widths = headers
            .Select((header, index) => rows
                .Select(row => row[index].Length)
                .Append(header.Length)
                .Max())
            .ToArray();

        WriteInColour(ConsoleColor.DarkGray, Row(headers, widths));
        WriteInColour(ConsoleColor.DarkGray, "  " + string.Join("-+-", widths.Select(width => new string('-', width))));

        foreach (var row in rows)
        {
            Console.WriteLine(Row(row, widths));
        }
    }

    public string? Ask(string prompt)
    {
        Console.Write($"{prompt}: ");
        var answer = ReadLine()?.Trim();
        return string.IsNullOrEmpty(answer) ? null : answer;
    }

    public string? AskRequired(string prompt)
    {
        for (var attempt = 0; attempt < 2; attempt++)
        {
            var answer = Ask(prompt);
            if (answer is not null)
            {
                return answer;
            }

            Warning("  This one is required - press Enter again to go back.");
        }

        return null;
    }

    public int? AskInteger(string prompt, int minimum = int.MinValue, int maximum = int.MaxValue)
    {
        var answer = Ask(prompt);
        if (answer is null)
        {
            return null;
        }

        if (!int.TryParse(answer, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
        {
            Error($"  '{answer}' is not a whole number.");
            return null;
        }

        if (value < minimum || value > maximum)
        {
            Error($"  Please enter a number between {Format.Number(minimum)} and {Format.Number(maximum)}.");
            return null;
        }

        return value;
    }

    public decimal? AskDecimal(string prompt)
    {
        var answer = Ask(prompt);
        if (answer is null)
        {
            return null;
        }

        // Both 12.50 and 12,50 are natural to type here, so accept either.
        var normalised = answer.Replace(',', '.');
        if (!decimal.TryParse(normalised, NumberStyles.Number, CultureInfo.InvariantCulture, out var value)
            || value < 0m)
        {
            Error($"  '{answer}' is not a valid amount.");
            return null;
        }

        return value;
    }

    public bool Confirm(string prompt)
    {
        Console.Write($"{prompt} [y/N]: ");
        var answer = ReadLine()?.Trim();
        return string.Equals(answer, "y", StringComparison.OrdinalIgnoreCase);
    }

    public T? Choose<T>(string prompt, IReadOnlyList<T> options, Func<T, string> describe)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(describe);

        if (options.Count == 0)
        {
            Muted("  Nothing to choose from.");
            return default;
        }

        for (var index = 0; index < options.Count; index++)
        {
            Console.WriteLine($"{new string(' ', MenuIndent)}{Format.Number(index + 1),2}. {describe(options[index])}");
        }

        var choice = AskInteger($"{prompt} (Enter to go back)", 1, options.Count);
        return choice is null ? default : options[choice.Value - 1];
    }

    public void Pause()
    {
        if (InputClosed)
        {
            return;
        }

        Blank();
        Muted("Press Enter to continue...");
        ReadLine();
    }

    public void Clear()
    {
        try
        {
            Console.Clear();
        }
        catch (IOException)
        {
            // Output is redirected, so there is no screen to clear. Not worth failing over.
            Blank();
        }
    }

    public bool Try(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);

        try
        {
            action();
            return true;
        }
        catch (DomainException exception)
        {
            // The domain refused on purpose, and its message is written for the person reading it.
            Error($"  {exception.Message}");
            return false;
        }
        catch (ArgumentException exception)
        {
            Error($"  Invalid input: {exception.Message}");
            return false;
        }
        catch (InvalidOperationException exception)
        {
            Error($"  Unexpected state: {exception.Message}");
            return false;
        }
    }

    /// <summary>
    /// <see cref="Console.ReadLine"/> returns <see langword="null"/> at end of input. Without
    /// noticing that, a menu loop that keeps re-prompting would spin forever the moment the app is
    /// piped or scripted instead of typed into.
    /// </summary>
    private string? ReadLine()
    {
        var line = Console.ReadLine();
        if (line is null)
        {
            InputClosed = true;
        }

        return line;
    }

    private static string Row(IReadOnlyList<string> cells, int[] widths) =>
        "  " + string.Join(" | ", cells.Select((cell, index) => cell.PadRight(widths[index])));

    private static void WriteInColour(ConsoleColor colour, string text)
    {
        var previous = Console.ForegroundColor;
        Console.ForegroundColor = colour;
        Console.WriteLine(text);
        Console.ForegroundColor = previous;
    }
}
