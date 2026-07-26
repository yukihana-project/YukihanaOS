// Yukihana OS 2026 Yukihana OS Contributors
// Licensed under the Apache License, Version 2.0. See LICENSE for details.

using System.Text;

namespace Yukihana.BuildConfig.SourceGenerators;

internal sealed class CSharpFileGenerator
{
    private readonly List<string> _comments = [];
    private readonly HashSet<string> _usings = [];
    private readonly List<ClassBuilder> _classes = [];

    private string? _namespace;

    private CSharpFileGenerator()
    {
    }

    public static CSharpFileGenerator Create() => new();

    public CSharpFileGenerator Comment(string comment)
    {
        _comments.Add(comment);
        return this;
    }

    public CSharpFileGenerator Comments(IEnumerable<string> comments)
    {
        _comments.AddRange(comments);
        return this;
    }

    public CSharpFileGenerator Using(string @namespace)
    {
        _usings.Add(@namespace);
        return this;
    }

    public CSharpFileGenerator Usings(IEnumerable<string> namespaces)
    {
        foreach (string ns in namespaces)
        {
            _usings.Add(ns);
        }

        return this;
    }

    public CSharpFileGenerator Usings(params string[] namespaces)
        => Usings((IEnumerable<string>)namespaces);

    public CSharpFileGenerator Namespace(string @namespace)
    {
        _namespace = @namespace;
        return this;
    }

    public CSharpFileGenerator Class(string name, Action<ClassBuilder> configure)
    {
        var builder = new ClassBuilder(name);
        configure(builder);
        _classes.Add(builder);
        return this;
    }

    public ClassBuilder Class(string name)
    {
        var builder = new ClassBuilder(name);
        _classes.Add(builder);
        return builder;
    }

    public string Generate()
    {
        var sb = new CodeWriter();

        foreach (string comment in _comments)
        {
            sb.Line("// " + comment);
        }

        if (_comments.Count != 0)
        {
            sb.Line();
        }

        foreach (string? u in _usings.OrderBy(x => x))
        {
            sb.Line($"using {u};");
        }

        if (_usings.Count != 0)
        {
            sb.Line();
        }

        if (!string.IsNullOrWhiteSpace(_namespace))
        {
            sb.Line($"namespace {_namespace};");
            sb.Line();
        }

        foreach (var cls in _classes)
        {
            cls.Write(sb);
            sb.Line();
        }

        return sb.ToString();
    }
}

internal sealed class ClassBuilder
{
    private readonly List<string> _comments = [];
    private readonly List<ConstBoolBuilder> _consts = [];

    private string _visibility = "internal";

    private bool _static;
    private bool _partial;
    private bool _sealed;
    private bool _abstract;

    internal ClassBuilder(string name)
    {
        Name = name;
    }

    public string Name { get; }

    public ClassBuilder Comment(string comment)
    {
        _comments.Add(comment);
        return this;
    }

    public ClassBuilder Public()
    {
        _visibility = "public";
        return this;
    }

    public ClassBuilder Internal()
    {
        _visibility = "internal";
        return this;
    }

    public ClassBuilder Static()
    {
        _static = true;
        return this;
    }

    public ClassBuilder Partial()
    {
        _partial = true;
        return this;
    }

    public ClassBuilder Sealed()
    {
        _sealed = true;
        return this;
    }

    public ClassBuilder Abstract()
    {
        _abstract = true;
        return this;
    }

    public ClassBuilder ConstBool(string name, bool value)
    {
        _consts.Add(new ConstBoolBuilder(name, value));
        return this;
    }

    public ClassBuilder ConstBool(
        string name,
        bool value,
        string xmlSummary)
    {
        _consts.Add(new ConstBoolBuilder(name, value)
            .Summary(xmlSummary));

        return this;
    }

    internal void Write(CodeWriter writer)
    {
        foreach (string comment in _comments)
        {
            writer.Line("/// <summary>");
            writer.Line($"/// {comment}");
            writer.Line("/// </summary>");
        }

        var modifiers = new List<string>
        {
            _visibility
        };

        if (_static)
        {
            modifiers.Add("static");
        }

        if (_abstract)
        {
            modifiers.Add("abstract");
        }

        if (_sealed)
        {
            modifiers.Add("sealed");
        }

        if (_partial)
        {
            modifiers.Add("partial");
        }

        writer.Line($"{string.Join(" ", modifiers)} class {Name}");
        writer.Line("{");

        writer.Indent();

        foreach (var field in _consts)
        {
            field.Write(writer);
        }

        writer.Unindent();

        writer.Line("}");
    }
}

public sealed class ConstBoolBuilder
{
    private readonly string _name;
    private readonly bool _value;

    private string? _summary;

    internal ConstBoolBuilder(string name, bool value)
    {
        _name = name;
        _value = value;
    }

    public ConstBoolBuilder Summary(string text)
    {
        _summary = text;
        return this;
    }

    internal void Write(CodeWriter writer)
    {
        if (!string.IsNullOrWhiteSpace(_summary))
        {
            writer.Line("/// <summary>");
            writer.Line($"/// {_summary}");
            writer.Line("/// </summary>");
        }

        writer.Line($"public const bool {_name} = {(_value ? "true" : "false")};");
        writer.Line();
    }
}

internal sealed class CodeWriter
{
    private readonly StringBuilder _builder = new();

    private int _indent;

    public void Indent()
    {
        _indent++;
    }

    public void Unindent()
    {
        _indent--;
    }

    public void Line(string text = "")
    {
        if (text.Length != 0)
        {
            _builder.Append(new string(' ', _indent * 4));
        }

        _builder.AppendLine(text);
    }

    public override string ToString()
    {
        return _builder.ToString();
    }
}
