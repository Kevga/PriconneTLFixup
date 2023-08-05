using System.Diagnostics;
using System.Runtime.CompilerServices;
using BepInEx.Logging;

namespace PriconneTLFixup;

public class Log
{
    internal static ManualLogSource? BieLogger;

#if DEBUG
    private static void _Log(string message, LogLevel logLevel, string filePath, string member, int line)
    {
        var pathParts = filePath.Split('\\');
        var className = pathParts[^1].Replace(".cs", "");
        var caller = new StackFrame(3, true).GetMethod()?.Name;
        var prefix = $"[{caller}->{className}.{member}:{line}]: ";
        BieLogger.Log(logLevel, $"{prefix}{message}");
    }

    public static void Debug(string message, [CallerFilePath] string filePath = "",
        [CallerMemberName] string member = "", [CallerLineNumber] int line = 0)
    {
        _Log(message, LogLevel.Debug, filePath, member, line);
    }

    public static void Info(string message, [CallerFilePath] string filePath = "",
        [CallerMemberName] string member = "", [CallerLineNumber] int line = 0)
    {
        _Log(message, LogLevel.Info, filePath, member, line);
    }

    public static void Warn(string message, [CallerFilePath] string filePath = "",
        [CallerMemberName] string member = "", [CallerLineNumber] int line = 0)
    {
        _Log(message, LogLevel.Warning, filePath, member, line);
    }

    public static void Error(string message, [CallerFilePath] string filePath = "",
        [CallerMemberName] string member = "", [CallerLineNumber] int line = 0)
    {
        _Log(message, LogLevel.Error, filePath, member, line);
    }
#else
		private static void _Log(string message, LogLevel logLevel)
		{
            BieLogger?.Log(logLevel, message);
		}

		public static void Debug(string message, bool evenInReleaseBuild)
		{
			_Log(message, LogLevel.Debug);
		}

		public static void Info(string message)
		{
			_Log(message, LogLevel.Info);
		}

		public static void Warn(string message)
		{
			_Log(message, LogLevel.Warning);
		}

		public static void Error(string message)
		{
			_Log(message, LogLevel.Error);
		}
#endif
    [Conditional("DEBUG")]
    public static void Debug(Exception exception)
    {
        BieLogger?.Log(LogLevel.Debug, exception);
    }

    [Conditional("DEBUG")]
    public static void Debug(string message)
    {
        BieLogger?.Log(LogLevel.Debug, message);
    }

    public static void Info(Exception exception)
    {
        BieLogger?.Log(LogLevel.Info, exception);
    }

    public static void Warn(Exception exception)
    {
        BieLogger?.Log(LogLevel.Warning, exception);
    }

    public static void Error(Exception exception)
    {
        BieLogger?.Log(LogLevel.Error, exception);
    }
}