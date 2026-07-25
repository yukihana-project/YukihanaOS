// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

using System.Security;
using System.Text;

namespace Yukihana.BuildConfig.SourceGenerators;

internal sealed class TargetsFileGenerator
{
    private readonly List<TargetDefinition> _targets = [];

    private TargetsFileGenerator()
    {

    }

    public static TargetsFileGenerator Create() => new();

    public TargetDefinition AddTarget(string name)
    {
        var target = new TargetDefinition(name);
        _targets.Add(target);
        return target;
    }

    public string Generate()
    {
        var sb = new StringBuilder();

        sb.AppendLine(@"<Project>");

        foreach (TargetDefinition target in _targets)
        {
            target.Write(sb, 1);
        }

        sb.AppendLine("</Project>");

        return sb.ToString();
    }

    public enum Importance
    {
        Low,
        Normal,
        High
    }

    public sealed record MessageDefinition(string Text, Importance Importance);

    public sealed class TargetDefinition(string name)
    {
        private readonly List<MessageDefinition> _messages = [];
        private readonly List<string> _compileIncludes = [];
        private readonly List<string> _compileRemoves = [];
        private readonly List<string> _defines = [];

        public string Name { get; } = name;
        public string? BeforeTargets { get; private set; }
        public string? AfterTargets { get; private set; }

        public TargetDefinition Before(IEnumerable<string> targets)
        {
            BeforeTargets = string.Join(';', targets);
            return this;
        }

        public TargetDefinition Before(params string[] targets)
            => Before((IEnumerable<string>)targets);


        public TargetDefinition After(IEnumerable<string> targets)
        {
            AfterTargets = string.Join(';', targets);
            return this;
        }

        public TargetDefinition After(params string[] targets)
            => After((IEnumerable<string>)targets);

        public TargetDefinition Message(string text, Importance importance = Importance.Normal)
        {
            _messages.Add(new MessageDefinition(text, importance));
            return this;
        }

        public TargetDefinition IncludeCompile(IEnumerable<string> items)
        {
            _compileIncludes.AddRange(items);
            return this;
        }

        public TargetDefinition IncludeCompile(params string[] items)
            => IncludeCompile((IEnumerable<string>)items);


        public TargetDefinition ExcludeCompile(IEnumerable<string> items)
        {
            _compileRemoves.AddRange(items);
            return this;
        }

        public TargetDefinition ExcludeCompile(params string[] items)
            => ExcludeCompile((IEnumerable<string>)items);


        public TargetDefinition DefineConstants(IEnumerable<string> constants)
        {
            _defines.AddRange(constants);
            return this;
        }

        public TargetDefinition DefineConstants(params string[] constants)
            => DefineConstants((IEnumerable<string>)constants);


        internal void Write(StringBuilder sb, int indent)
        {
            WriteIndent(sb, indent);

            sb.Append("<Target Name=\"");
            sb.Append(Escape(Name));
            sb.Append('"');

            if (!string.IsNullOrWhiteSpace(BeforeTargets))
            {
                sb.Append(" BeforeTargets=\"");
                sb.Append(Escape(BeforeTargets));
                sb.Append('"');
            }

            if (!string.IsNullOrWhiteSpace(AfterTargets))
            {
                sb.Append(" AfterTargets=\"");
                sb.Append(Escape(AfterTargets));
                sb.Append('"');
            }

            sb.AppendLine(">");

            foreach (MessageDefinition message in _messages)
            {
                WriteIndent(sb, indent + 1);
                sb.Append("<Message Text=\"");
                sb.Append(Escape(message.Text));
                sb.Append("\" Importance=\"");
                sb.Append(message.Importance);
                sb.AppendLine("\" />");
            }


            if (_compileIncludes.Count != 0 || _compileRemoves.Count != 0)
            {
                WriteIndent(sb, indent + 1);
                sb.AppendLine("<ItemGroup>");

                foreach (string item in _compileRemoves)
                {
                    WriteIndent(sb, indent + 2);
                    sb.Append("<Compile Remove=\"");
                    sb.Append(Escape(item));
                    sb.AppendLine("\" />");
                }

                foreach (string item in _compileIncludes)
                {
                    WriteIndent(sb, indent + 2);
                    sb.Append("<Compile Include=\"");
                    sb.Append(Escape(item));
                    sb.AppendLine("\" />");
                }

                WriteIndent(sb, indent + 1);
                sb.AppendLine("</ItemGroup>");
            }

            if (_defines.Count != 0)
            {
                WriteIndent(sb, indent + 1);
                sb.AppendLine("<PropertyGroup>");

                WriteIndent(sb, indent + 2);
                sb.Append("<DefineConstants>$(DefineConstants)");

                foreach (string? define in _defines.Distinct())
                {
                    sb.Append(';');
                    sb.Append(Escape(define));
                }

                sb.AppendLine("</DefineConstants>");

                WriteIndent(sb, indent + 1);
                sb.AppendLine("</PropertyGroup>");
            }

            WriteIndent(sb, indent);
            sb.AppendLine("</Target>");
        }

        private static void WriteIndent(StringBuilder sb, int level)
        {
            sb.Append(' ', level * 2);
        }

        private static string Escape(string value)
        {
            return SecurityElement.Escape(value) ?? "";
        }
    }
}
