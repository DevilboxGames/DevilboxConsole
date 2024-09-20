using System;

namespace DevilboxGames.DebugConsole.extensions;

public static class StringExtensions
{
    public static string GetCommonStart(this string a, string b)
    {
        int length = 0;

        if (a.Length == 0 || b.Length == 0 || a[0] != b[0])
        {
            return "";
        }

        while (length + 1 < Math.Min(a.Length, b.Length) && a[length + 1] == b[length + 1])
        {
            length++;
        }

        return b.Substring(0, length + 1);
    }
}