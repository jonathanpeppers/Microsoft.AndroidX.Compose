using System;
using System.Text.RegularExpressions;
using AndroidX.Compose.Material3;
using ComposeNet;

namespace ComposeNet.Samples.Jetchat;

/// <summary>
/// Port of upstream Jetchat's <c>MessageFormatter.kt</c>. Scans a chat
/// message body for inline markup tokens — <c>*bold*</c>, <c>_italic_</c>,
/// <c>~strike~</c>, <c>`code`</c>, <c>@mention</c>, and <c>https?://...</c>
/// URLs — and produces a styled <see cref="AnnotatedString"/> with click
/// handlers attached to mentions and links.
/// </summary>
public static class MessageFormatter
{
    // Same alternation order as upstream: longest-first so an @mention
    // inside a URL is consumed by the URL match.
    static readonly Regex SymbolPattern = new(
        @"(https?://[^\s\t\n]+)|(`[^`]+`)|(@\w+)|(\*[\w]+\*)|(_[\w]+_)|(~[\w]+~)",
        RegexOptions.Compiled);

    /// <summary>
    /// Parse <paramref name="text"/> and emit a styled
    /// <see cref="AnnotatedString"/> matching Jetchat's bubble formatting.
    /// </summary>
    /// <param name="text">Raw message body.</param>
    /// <param name="isMe">
    /// <see langword="true"/> for messages authored by the local user
    /// (selects the inverse-primary code-block background), otherwise the
    /// surface-container variant is used.
    /// </param>
    /// <param name="scheme">Active Material 3 color scheme.</param>
    /// <param name="onAuthorClick">
    /// Optional handler invoked when the user taps an <c>@mention</c>; the
    /// argument is the matched author tag (e.g. <c>"@aliconors"</c>).
    /// Pass <see langword="null"/> to leave mentions non-interactive.
    /// </param>
    public static AnnotatedString Format(
        string       text,
        bool         isMe,
        ColorScheme  scheme,
        Action<string>? onAuthorClick = null)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(scheme);

        var builder = new AnnotatedStringBuilder();
        var accent  = new Color(isMe ? scheme.InversePrimary : scheme.Primary);
        var codeBg  = new Color(isMe ? scheme.InversePrimary : scheme.SurfaceContainer);

        int cursor = 0;
        foreach (Match match in SymbolPattern.Matches(text))
        {
            // Plain text between the previous match and this one.
            if (match.Index > cursor)
                builder.Append(text.Substring(cursor, match.Index - cursor));

            string token = match.Value;
            switch (token[0])
            {
                case 'h': // https?://...
                    AppendUrl(builder, token, accent);
                    break;
                case '`':
                    AppendCode(builder, token, codeBg);
                    break;
                case '@':
                    AppendMention(builder, token, accent, onAuthorClick);
                    break;
                case '*':
                    AppendStyled(builder, token, new SpanStyle { FontWeight = FontWeight.Bold });
                    break;
                case '_':
                    AppendStyled(builder, token, new SpanStyle { FontStyle = FontStyle.Italic });
                    break;
                case '~':
                    AppendStyled(builder, token, new SpanStyle { Decoration = TextDecoration.LineThrough });
                    break;
                default:
                    builder.Append(token);
                    break;
            }

            cursor = match.Index + match.Length;
        }

        if (cursor < text.Length)
            builder.Append(text.Substring(cursor));

        return builder.ToAnnotatedString();
    }

    static void AppendUrl(AnnotatedStringBuilder builder, string url, Color accent)
    {
        var style = new SpanStyle
        {
            Color      = accent,
            Decoration = TextDecoration.Underline,
        };
        int idx = builder.PushLink(LinkAnnotation.Url(url, style));
        builder.Append(url);
        builder.Pop(idx);
    }

    static void AppendCode(AnnotatedStringBuilder builder, string token, Color codeBg)
    {
        // Strip the surrounding backticks.
        string inner = token.Substring(1, token.Length - 2);
        int idx = builder.PushStyle(new SpanStyle
        {
            FontFamily = FontFamily.Monospace,
            Background = codeBg,
        });
        builder.Append(inner);
        builder.Pop(idx);
    }

    static void AppendMention(
        AnnotatedStringBuilder builder,
        string                 mention,
        Color                  accent,
        Action<string>?        onAuthorClick)
    {
        var style = new SpanStyle
        {
            Color      = accent,
            FontWeight = FontWeight.Bold,
        };

        int start = builder.Length;
        if (onAuthorClick is not null)
        {
            int idx = builder.PushLink(LinkAnnotation.Clickable("profile", onAuthorClick, style));
            builder.Append(mention);
            builder.Pop(idx);
        }
        else
        {
            int idx = builder.PushStyle(style);
            builder.Append(mention);
            builder.Pop(idx);
        }
        builder.AddStringAnnotation("profile", mention, start, builder.Length);
    }

    static void AppendStyled(AnnotatedStringBuilder builder, string token, SpanStyle style)
    {
        // Strip the surrounding marker character (*, _, or ~).
        string inner = token.Substring(1, token.Length - 2);
        int idx = builder.PushStyle(style);
        builder.Append(inner);
        builder.Pop(idx);
    }
}
