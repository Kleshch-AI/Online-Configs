using System;
using System.Diagnostics;
using Object = UnityEngine.Object;
using UDebug = UnityEngine.Debug;

namespace Utils
{
    public class Debug
    {
        [Conditional("DEBUG_ENABLE_LOG")]
        public static void Assert(bool condition, object message = null)
        {
            UDebug.Assert(condition, message);
        }

        [Conditional("DEBUG_ENABLE_LOG")]
        public static void Log(object message, Object obj = null)
        {
            UDebug.Log(message, obj);
        }

        [Conditional("DEBUG_ENABLE_LOG")]
        public static void LogFormat(string format, params object[] args)
        {
            UDebug.LogFormat(format, args);
        }

        [Conditional("DEBUG_ENABLE_LOG")]
        public static void LogAssertion(object message, Object obj = null)
        {
            UDebug.LogAssertion(message, obj);
        }

        [Conditional("DEBUG_ENABLE_LOG")]
        public static void LogWarning(object message, Object obj = null)
        {
            UDebug.LogWarning(message, obj);
        }

        [Conditional("DEBUG_ENABLE_LOG")]
        public static void LogWarningFormat(string format, params object[] args)
        {
            UDebug.LogWarningFormat(format, args);
        }

        [Conditional("DEBUG_ENABLE_LOG")]
        public static void LogError(object message, Object obj = null)
        {
            UDebug.LogError(message, obj);
        }

        [Conditional("DEBUG_ENABLE_LOG")]
        public static void LogErrorFormat(string format, params object[] args)
        {
            UDebug.LogErrorFormat(format, args);
        }

        [Conditional("DEBUG_ENABLE_LOG")]
        public static void LogException(Exception exception, Object obj = null)
        {
            UDebug.LogException(exception, obj);
        }

        public static void Break()
        {
            UDebug.Break();
        }
    }
}