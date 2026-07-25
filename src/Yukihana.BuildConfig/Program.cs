// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

using System.CommandLine;
using System.CommandLine.Help;
using System.CommandLine.Parsing;
using Serilog;
using Serilog.Core;
using Serilog.Sinks.Spectre;
using Yukihana.BuildConfig.CommandHandlers;
using Yukihana.BuildConfig.Menu;

namespace Yukihana.BuildConfig;

internal class Program
{
    private const string MUTUAL_EXCLUSIVE_MSG = "Mutually exclusive options used. Use either '{0}' or '{1}'";

    private static Func<ParseResult, int> Wrap(Func<ParseResult, int> inner)
    {
        return result =>
        {
            RootCommandHandler.Handle(result);

            return  inner(result);
        };
    }

    private static int Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.ControlledBy(Globals.LevelSwitch)
            .WriteTo.Spectre("[{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        foreach (Option option in Globals.Args.RootCmd.Options)
        {
            if (option is HelpOption defaultHelpOption)
            {
                defaultHelpOption.Action = new CustomHelpAction((HelpAction)defaultHelpOption.Action!);
                break;
            }
        }

        //public static Option<bool> FeaturesOption { get; set; } = new("--features");
        //public static Option<bool> GroupsOption { get; set; } = new("--groups");
        //public static Option<bool> PresetsOption { get; set; } = new("--presets");
        //public static Option<bool> EnabledOption { get; set; } = new("--enabled");
        //public static Option<bool> FisabledOption { get; set; } = new("--disabled");

        Globals.Args.listArgumnet.AcceptOnlyFromAmong(["features", "groups", "presets", "enabled", "disabled"]);

        Globals.Args.FeaturesPathOption.DefaultValueFactory = _ => "./Build/Features/";
        Globals.Args.GeneratedPathOption.DefaultValueFactory = _ => "./Build/Generated/";
        Globals.Args.ConfigsPathOption.DefaultValueFactory = _ => "./Build/Configs/";
        Globals.Args.ManifestPathOption.DefaultValueFactory = _ => "./Build/Manifest.toml";

        Globals.Args.ConfigureCommand.SetAction(Wrap(ConfigureManager.Handle));
        Globals.Args.MenuCommand.SetAction(Wrap(MenuCommandHandler.Handle));
        // check handler
        Globals.Args.ValidateCommand.SetAction(Wrap(ValidateHandler.Handle));
        // clean handler
        Globals.Args.ListCommand.SetAction(Wrap(ListCommandHandler.Handle));
        // TODO: preset handler
        // TODO: feature handler
        // TODO: info handler
        // TODO: graph handler
        Globals.Args.InitCommand.SetAction(Wrap(InitCommandHandler.Handle));

        Globals.Args.RootCmd.Validators.Add(result =>
        {
            var options = result.Children
                .OfType<OptionResult>()
                .Where(or => or.Option == Globals.Args.VerboseOption ||
                            or.Option == Globals.Args.QuietOption)
                .ToList();
            
            if (options.Count == 0)
            {
                return;
            }

            if (options.Count == 1)
            {
                return;
            }

            result.AddError(string.Format(MUTUAL_EXCLUSIVE_MSG, options[0].IdentifierToken, options[1].IdentifierToken));
        });

        ParseResult result = Globals.Args.RootCmd.Parse(args);

        if (args.Length > 0 && result.Errors.Count > 0)
        {
            string fullCmdLine = string.Join(" ", args);

            Log.Fatal("Syntax error parsing command line:");
            Log.Fatal($"> {fullCmdLine}");

            ParseError firstError = result.Errors[0];

            string? invalidToken = result.Tokens
                .Select(t => t.Value)
                .FirstOrDefault(v => firstError.Message.Contains($"'{v}'") || firstError.Message.Contains(v))
                ?? args.FirstOrDefault();

            if (string.IsNullOrEmpty(invalidToken))
            {
                Log.Fatal($"Error: {firstError.Message}");
                return 1;
            }

            int idx = fullCmdLine.IndexOf(invalidToken);
            if (idx >= 0)
            {
                string spaces = new(' ', idx + 2);
                string underline = '^' + new string('~', Math.Max(0, invalidToken.Length - 1));
                Log.Fatal($"{spaces}{underline}");
                Log.Fatal($"Error: {firstError.Message}");
            }

            return 1;
        }

        return result.Invoke();
    }
}
