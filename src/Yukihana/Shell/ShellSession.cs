// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

using Yukihana.Security;
using Yukihana.Shell.Execution;

namespace Yukihana.Shell;

public sealed class ShellSession
{
    public required User User { get; set; }
    public string CurrentDirectory { get; set; } = "/";
    //TODO
    //public ShellEnvironment Environment { get; } = new();
    //public ShellHistory History { get; } = new();
    public ShellJobManager Jobs { get; } = new();
    public bool ExitRequested { get; set; }

    public static string BaseName(string path) => path[(path.LastIndexOf('/') + 1)..];

    public static string Parent(string path)
    {
        int slash = path.LastIndexOf('/');
        return slash <= 0 ? "/" : path[..slash];
    }

    public static string Child(string directory, string name) => $"{directory.TrimEnd('/')}/{name}";

    public string ResolvePath(string path)
    {
        string full = path.StartsWith('/') ? path : $"{CurrentDirectory}/{path}";
        List<string> parts = [];

        foreach (string part in full.Split('/', StringSplitOptions.RemoveEmptyEntries))
        {
            if (part == "..")
            {
                if (parts.Count > 0)
                {
                    parts.RemoveAt(parts.Count - 1);
                }
            }
            else if (part != ".")
            {
                parts.Add(part);
            }
        }

        return "/" + string.Join('/', parts);
    }
}
