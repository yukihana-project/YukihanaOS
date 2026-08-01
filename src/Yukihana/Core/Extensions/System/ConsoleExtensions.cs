// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

using System.Text;
using Yukihana.Debug;

namespace Yukihana.Core.Extensions.System;

public static class ConsoleExtensions
{
    extension(Console)
    {
        public static string? ReadLineHidden()
        {
            StringBuilder sb = new();

            while(true)
            {
                ConsoleKeyInfo keyInfo = Console.ReadKey(intercept: true);

                if (keyInfo.Key == ConsoleKey.Enter)
                {
                    Console.WriteLine();
                    break;
                }

                if (keyInfo.Key == ConsoleKey.Backspace && sb.Length > 0)
                {
                    sb.Remove(sb.Length - 1, 1);
                }
                else if (keyInfo.KeyChar != '\0')
                {
                    sb.Append(keyInfo.KeyChar);
                }
            }

            string result = sb.ToString();

            Logger.GlobalLogger.Trace($"ReadLineHidden -> {result}");

            return string.IsNullOrEmpty(result) ? null : result;
        }
    }
}