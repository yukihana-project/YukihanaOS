// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

using Yukihana.Core.Extensions.System;

namespace Yukihana.Security;

public static class PasswordPrompt
{
    public static string ReadNew()
    {
        while (true)
        {
            Console.Write("New password: ");
            string? first = Console.ReadLineHidden();

            Console.Write("Retype password: ");
            string? second = Console.ReadLineHidden();

            if (string.IsNullOrEmpty(first))
            {
                Console.WriteLine("Password must not be empty");
                continue;
            }

            if (first != second)
            {
                Console.WriteLine("Passwords do not match");
                continue;
            }

            return first;
        }
    }
}
