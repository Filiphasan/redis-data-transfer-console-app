namespace RedisKeyMover.Helpers;

public static class ConsoleHelper
{
    private static readonly ConsoleColor DefaultConsoleColor = Console.ForegroundColor;
    private const string LineSeperator = "----------------------------------";

    private const ConsoleColor Error = ConsoleColor.Red;
    private const ConsoleColor Success = ConsoleColor.Green;
    private const ConsoleColor Warning = ConsoleColor.Yellow;
    private const ConsoleColor Info = ConsoleColor.Cyan;

    public static string? ReadLine(string message, bool newLine = true)
    {
        if (newLine)
        {
            Console.WriteLine(message);
        }
        else
        {
            Console.Write(message);
        }
        
        return Console.ReadLine();
    }
    
    public static bool ReadLineAsAccept(string message, bool newLine = true)
    {
        const string optionsSuffix = " (E/H) veya (Y/N)";
        var input = ReadLine(message + optionsSuffix, newLine);
        return input?.ToUpper() == "E" || input?.ToUpper() == "Y";
    }

    public static void WriteError(string message, bool newLine = true)
    {
        Console.ForegroundColor = Error;
        if (newLine)
        {
            Console.WriteLine(message);
        }
        else
        {
            Console.Write(message);
        }

        Console.ForegroundColor = DefaultConsoleColor;
    }

    public static void WriteSuccess(string message, bool newLine = true)
    {
        Console.ForegroundColor = Success;
        if (newLine)
        {
            Console.WriteLine(message);
        }
        else
        {
            Console.Write(message);
        }

        Console.ForegroundColor = DefaultConsoleColor;
    }

    public static void WriteWarning(string message, bool newLine = true)
    {
        Console.ForegroundColor = Warning;
        if (newLine)
        {
            Console.WriteLine(message);
        }
        else
        {
            Console.Write(message);
        }

        Console.ForegroundColor = DefaultConsoleColor;
    }

    public static void WriteInfo(string message, bool newLine = true)
    {
        Console.ForegroundColor = Info;
        if (newLine)
        {
            Console.WriteLine(message);
        }
        else
        {
            Console.Write(message);
        }

        Console.ForegroundColor = DefaultConsoleColor;
    }

    public static void Write(string message, bool newLine = true)
    {
        if (newLine)
        {
            Console.WriteLine(message);
        }
        else
        {
            Console.Write(message);
        }
    }

    public static void WriteLineWithSeperator(string message, bool isAfter = true)
    {
        if (!isAfter)
        {
            Console.WriteLine(LineSeperator);
        }

        Console.WriteLine("Redis Veri Taşıma Uygulaması");

        if (isAfter)
        {
            Console.WriteLine(LineSeperator);
        }
    }
}