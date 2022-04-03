#if UNSAFE

using System.Linq.Expressions;
using System.Text;
using AtraBase.Toolkit.Reflection;
using Microsoft.Xna.Framework.Graphics;

namespace AtraShared.Utils;

/// <summary>
/// Handles methods for dealing with strings.
/// </summary>
internal static class StringUtils
{
    private static readonly Lazy<Func<SpriteFont, char, int>> GetGlyphLazy = new(() =>
    {
        ParameterExpression? spriteinstance = Expression.Variable(typeof(SpriteFont));
        ParameterExpression? charinstance = Expression.Variable(typeof(char));
        MethodCallExpression? call = Expression.Call(
            spriteinstance,
            typeof(SpriteFont).InstanceMethodNamed("GetGlyphIndexOrDefault"),
            charinstance);
        return (Func<SpriteFont, char, int>)Expression.Lambda(call, spriteinstance, charinstance).Compile();
    });

    private static Func<SpriteFont, char, int> GetGlyph => GetGlyphLazy.Value;

    /// <summary>
    /// Parses and wraps text, defaulting to Game1.dialogueFont and Game1.dialogueWidth.
    /// </summary>
    /// <param name="text">Text to process.</param>
    /// <returns>String with wrapped text.</returns>
    /// <remarks>This is meant to be a more performant Game1.parseText.</remarks>
    internal static string ParseAndWrapText(string? text)
        => text is null ? string.Empty : ParseAndWrapText(text, Game1.dialogueFont, Game1.dialogueWidth);

    /// <summary>
    /// Parses and wraps text.
    /// </summary>
    /// <param name="text">Text to process.</param>
    /// <param name="whichFont">Font to use.</param>
    /// <param name="width">Maximum width.</param>
    /// <returns>String with wrapped text.</returns>
    /// <remarks>This is meant to be a more performant Game1.parseText.</remarks>
    internal static string ParseAndWrapText(string? text, SpriteFont whichFont, float width)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }
        if (text.IndexOf(Dialogue.genderDialogueSplitCharacter) is int genderseperator && genderseperator > 0)
        {
            text = Game1.player.IsMale ? text[..genderseperator] : text[(genderseperator + 1)..];
        }
        switch (LocalizedContentManager.CurrentLanguageCode)
        {
            case LocalizedContentManager.LanguageCode.ja:
            case LocalizedContentManager.LanguageCode.zh:
            case LocalizedContentManager.LanguageCode.th:
            case LocalizedContentManager.LanguageCode.mod when Game1.dialogueFont.Glyphs.Length > 4000:
                return WrapTextByChar(text, whichFont, width);
            default:
                return WrapTextByWords(text, whichFont, width);
        }
    }

    /// <summary>
    /// Wraps text, using spaces as word boundaries.
    /// </summary>
    /// <param name="text">Text to wrap.</param>
    /// <param name="whichFont">Which font to use.</param>
    /// <param name="width">Maximum width.</param>
    /// <returns>Wrapped text.</returns>
    internal static string WrapTextByWords(string text, SpriteFont whichFont, float width)
    {
        StringBuilder sb = new();
        float spacewidth = whichFont.MeasureString(" ").X + whichFont.Spacing;
        string[] paragraphs = text.Split(new string[] { "\n", "\r\n", "\r" }, StringSplitOptions.None);
        foreach (string? paragraph in paragraphs)
        {
            if (string.IsNullOrEmpty(paragraph))
            {
                sb.AppendLine();
                continue;
            }
            string[] split = paragraph.Split(' ');
            float current_width = -whichFont.Spacing;
            foreach (string word in split)
            {
                if (LocalizedContentManager.CurrentLanguageCode is LocalizedContentManager.LanguageCode.fr && word.StartsWith("\n-"))
                {
                    current_width = -whichFont.Spacing;
                    sb.AppendLine();
                    continue;
                }
                float wordwidth = whichFont.MeasureString(word).X + spacewidth;
                current_width += whichFont.Spacing + wordwidth;
                if (current_width > width)
                {
                    sb.AppendLine();
                    current_width = wordwidth;
                }
                sb.Append(word).Append(' ');
            }
            sb.AppendLine();
        }

        // remove the last newline, that wasn't necessary.
        sb.Remove(sb.Length - Environment.NewLine.Length, Environment.NewLine.Length);
        return sb.ToString();
    }

    /// <summary>
    /// Wraps text one character at time.
    /// </summary>
    /// <param name="text">Text to wrap.</param>
    /// <param name="whichFont">Which font to use.</param>
    /// <param name="width">Maximum width.</param>
    /// <returns>Wrapped text.</returns>
    internal static unsafe string WrapTextByChar(string text, SpriteFont whichFont, float width)
    {
        StringBuilder sb = new();
        float current_width = -whichFont.Spacing;
        float charwidth = 0;
        float proposedcharwidth = 0;
        fixed (SpriteFont.Glyph* pointerToGlyphs = whichFont.Glyphs)
        {
            foreach (char ch in text)
            {
                switch (ch)
                {
                    case '\r':
                        continue;
                    case '\n':
                        current_width = -whichFont.Spacing;
                        sb.Append(Environment.NewLine);
                        break;
                    default:
                        int glyph = GetGlyph(whichFont, ch);
                        if (glyph > 0)
                        {
                            SpriteFont.Glyph* pWhichGlyph = pointerToGlyphs + glyph;
                            charwidth = pWhichGlyph->LeftSideBearing + pWhichGlyph->Width + pWhichGlyph->RightSideBearing;
                            proposedcharwidth = pWhichGlyph->RightSideBearing < 0
                                ? pWhichGlyph->LeftSideBearing + pWhichGlyph->Width
                                : charwidth;
                            if (current_width + proposedcharwidth + whichFont.Spacing > width)
                            {
                                sb.AppendLine();
                                current_width = charwidth;
                            }
                            sb.Append(ch);
                            current_width += charwidth + whichFont.Spacing;
                        }
                        break;
                }
            }
        }
        return sb.ToString();
    }
}

#endif