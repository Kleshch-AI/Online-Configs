using UnityEngine;

namespace Utils
{
    public static class DebugUtils
    {
        public static void SplitLog(string longStr)
        {
            var logSize = 16000;
            var length = longStr.Length;
            var count = length / logSize;
            var leftoverSize = length % logSize;
            for (var i = 0; i <= count; i++)
                Debug.Log(longStr.Substring(i * logSize, Mathf.Min(logSize, leftoverSize)) + "\n");
        }
    }
}