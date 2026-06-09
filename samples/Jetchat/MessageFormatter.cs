using System;
using System.Text.RegularExpressions;
using AndroidX.Compose.Material3;
using ComposeNet;

namespace ComposeNet.Samples.Jetchat;

/// <summary>
/// Format a message following Markdown-lite syntax:
/// <list type="bullet">
///   <item>@username — bold, primary color, clickable element</item>
///   <item>http(s)://… — clickable link, opens in the browser</item>
///   <item>*bold* — bold</item>
///   <item>_italic_ — italic</item>
///   <item>~strikethrough~ — strikethrough</item>
///   <item>`MyClass.myMethod` — inline code styling</item>
/// </list>
/// </summary>
public static class MessageFormatter
{
    // Regex containing the syntax tokens
    static readonly Regex SymbolPattern = new(
        @"(https?://[^\s\t\n]+)|(`[^`]+`)|(@\w+)|(\*[\w]+\*)|(_[\w]+_)|(~[\w]+~)",
        RegexOptions.Compiled);

    // Tag used by addStringAnnotation for @mention click targets.
    const string PersonTag = "PERSON";

    /// <summary>
    /// Parse <paramref name="text"/> and return the styled
    /// <see cref="AnnotatedString"/>.
    /// </summary>
    public static AnnotatedString Format(
        string          text,
        bool            primary,
        ColorScheme     scheme,
        Action<string>? onAuthorClick = null)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(scheme);

        var builder = new AnnotatedStringBuilder();
        var codeSnippetBackground = new Color(primary ? scheme.Secondary : scheme.Surface);
        var accent                = new Color(primary ? scheme.InversePrimary : scheme.Primary);

        int cursor = 0;
        bool any = false;
        foreach (Match token in SymbolPattern.Matches(text))
        {
            any = true;
            if (token.Index > cursor)
                builder.Append(text.Substring(cursor, token.Index - cursor));

            AppendSymbolAnnotation(builder, token.Value, accent, codeSnippetBackground, onAuthorClick);
            cursor = token.Index + token.Length;
        }

        if (any)
        {
            if (cursor < text.Length)
                builder.Append(text.Substring(cursor));
        }
        else
        {
            builder.Append(text);
        }

        return builder.ToAnnotatedString();
    }

    static void AppendSymbolAnnotation(
        AnnotatedStringBuilder builder,
        string                 value,
        Color                  accent,
        Color                  codeSnippetBackground,
        Action<string>?        onAuthorClick)
    {
        switch (value[0])
        {
            case '@':
                {
                    var style = new SpanStyle { Color = accent, FontWeight = FontWeight.Bold };
                    int start = builder.Length;
                    if (onAuthorClick is not null)
                    {
                        int idx = builder.PushLink(LinkAnnotation.Clickable(value.Substring(1), onAuthorClick, style));
                        builder.Append(value);
                        builder.Pop(idx);
                    }
                    else
                    {
                        int idx = builder.PushStyle(style);
                        builder.Append(value);
                        builder.Pop(idx);
                    }
                    builder.AddStringAnnotation(PersonTag, value.Substring(1), start, builder.Length);
                    break;
                }

            case '*':
                AppendStyled(builder, value.Trim('*'), new SpanStyle { FontWeight = FontWeight.Bold });
                break;

            case '_':
                AppendStyled(builder, value.Trim('_'), new SpanStyle { FontStyle = FontStyle.Italic });
                break;

            case '~':
                AppendStyled(builder, value.Trim('~'), new SpanStyle { Decoration = TextDecoration.LineThrough });
                break;

            case '`':
                AppendStyled(builder, value.Trim('`'), new SpanStyle
                {
                    FontFamily = FontFamily.Monospace,
                    Background = codeSnippetBackground,
                });
                break;

            case 'h':
                {
                    var style = new SpanStyle { Color = accent, Decoration = TextDecoration.Underline };
                    int idx = builder.PushLink(LinkAnnotation.Url(value, style));
                    builder.Append(value);
                    builder.Pop(idx);
                    break;
                }

            default:
                builder.Append(value);
                break;
        }
    }

    static void AppendStyled(AnnotatedStringBuilder builder, string text, SpanStyle style)
    {
        int idx = builder.PushStyle(style);
        builder.Append(text);
        builder.Pop(idx);
    }
}
