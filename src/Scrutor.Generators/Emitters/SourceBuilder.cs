using System;
using System.Text;

namespace Scrutor.Generators.Emitters;

/// <summary>
/// Helper for building source code with proper indentation.
/// </summary>
internal sealed class SourceBuilder
{
    private readonly StringBuilder _builder = new();
    private int _indentLevel;
    private const string IndentString = "    ";

    public SourceBuilder AppendLine()
    {
        _builder.AppendLine();
        return this;
    }

    public SourceBuilder AppendLine(string line)
    {
        AppendIndent();
        _builder.AppendLine(line);
        return this;
    }

    public SourceBuilder Append(string text)
    {
        _builder.Append(text);
        return this;
    }

    public SourceBuilder AppendIndent()
    {
        for (var i = 0; i < _indentLevel; i++)
        {
            _builder.Append(IndentString);
        }
        return this;
    }

    public SourceBuilder OpenBrace()
    {
        AppendLine("{");
        _indentLevel++;
        return this;
    }

    public SourceBuilder CloseBrace(bool withSemicolon = false)
    {
        _indentLevel--;
        AppendLine(withSemicolon ? "};" : "}");
        return this;
    }

    public SourceBuilder CloseBraceNoNewLine()
    {
        _indentLevel--;
        AppendIndent();
        _builder.Append("}");
        return this;
    }

    public IDisposable BeginBlock()
    {
        OpenBrace();
        return new BlockScope(this);
    }

    public IDisposable Indent()
    {
        _indentLevel++;
        return new IndentScope(this);
    }

    public SourceBuilder Unindent()
    {
        if (_indentLevel > 0)
        {
            _indentLevel--;
        }
        return this;
    }

    public override string ToString() => _builder.ToString();

    private sealed class BlockScope : IDisposable
    {
        private readonly SourceBuilder _builder;

        public BlockScope(SourceBuilder builder) => _builder = builder;

        public void Dispose() => _builder.CloseBrace();
    }

    private sealed class IndentScope : IDisposable
    {
        private readonly SourceBuilder _builder;

        public IndentScope(SourceBuilder builder) => _builder = builder;

        public void Dispose() => _builder._indentLevel--;
    }
}
