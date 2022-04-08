#if UNSAFE

using System.Linq.Expressions;
using System.Text;
using AtraBase.Toolkit.Reflection;
using Microsoft.Xna.Framework.Graphics;

namespace AtraShared.Utils;

/*********
 * TODO
 *
 * 1. Handle the case where the **word** is longer than the box. (Done, I think?)
 * 2. Handle the max height param. (Done, I think, except still need to handle adding ...).
 * 3. Figure out Span? Am I allocating by passing around strings here?
 * 4. This probably could do with unit tests.
 * 5. Hyphens are valid word-splitting characters
 * *********/

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
        return Expression.Lambda<Func<SpriteFont, char, int>>(call, spriteinstance, charinstance).Compile();
    });

    private static Func<SpriteFont, char, int> GetGlyph => GetGlyphLazy.Value;

    private static IMonitor? Monitor { get; set; }

    /// <summary>
    /// Attaches the monitor to the StringUtils.
    /// </summary>
    /// <param name="monitor">Monitor to attach.</param>
    internal static void Initialize(IMonitor monitor)
        => Monitor = monitor;

    /// <summary>
    /// Parses and wraps text, defaulting to Game1.dialogueFont and Game1.dialogueWidth.
    /// </summary>
    /// <param name="text">Text to process.</param>
    /// <param name="height">Max height of text.</param>
    /// <returns>String with wrapped text.</returns>
    /// <remarks>This is meant to be a more performant Game1.parseText.</remarks>
    internal static string ParseAndWrapText(string? text, float? height = null)
        => text is null ? string.Empty : ParseAndWrapText(text, Game1.dialogueFont, Game1.dialogueWidth, height);

    /// <summary>
    /// Parses and wraps text.
    /// </summary>
    /// <param name="text">Text to process.</param>
    /// <param name="whichFont">Font to use.</param>
    /// <param name="width">Maximum width.</param>
    /// <param name="height">Max height.</param>
    /// <returns>String with wrapped text.</returns>
    /// <remarks>This is meant to be a more performant Game1.parseText.</remarks>
    internal static string ParseAndWrapText(string? text, SpriteFont whichFont, float width, float? height = null)
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
                return WrapTextByChar(text, whichFont, width, height);
            default:
                return WrapTextByWords(text, whichFont, width, height);
        }
    }

    /// <summary>
    /// Wraps text, using spaces as word boundaries.
    /// </summary>
    /// <param name="text">Text to wrap.</param>
    /// <param name="whichFont">Which font to use.</param>
    /// <param name="width">Maximum width.</param>
    /// <returns>Wrapped text.</returns>
    internal static string WrapTextByWords(string text, SpriteFont whichFont, float width, float? height = null)
    {
        int maxlines = height is null ? 1000 : (int)height / whichFont.LineSpacing;
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
            string replacement_word = string.Empty;
            bool use_replacement_word = false;
            foreach (string word in split)
            {
                if (LocalizedContentManager.CurrentLanguageCode is LocalizedContentManager.LanguageCode.fr && word.StartsWith("\n-"))
                { // This is from vanilla code, I dunno why French is special.
                    if (--maxlines <= 0)
                    {
                        return sb.ToString();
                    }
                    current_width = -whichFont.Spacing;
                    sb.AppendLine();
                    continue;
                }
                float wordwidth = whichFont.MeasureString(word).X + spacewidth;
                if (wordwidth > width)
                { // if the word itself is **longer** than the width, we must truncate. It'll get its own line.
                    (replacement_word, wordwidth) = TruncateWord(word, whichFont, width);
                    use_replacement_word = true;
                }
                current_width += whichFont.Spacing + wordwidth;
                if (current_width > width)
                {
                    if (--maxlines <= 0)
                    {
                        return sb.ToString();
                    }
                    sb.AppendLine();
                    current_width = wordwidth;
                }
                sb.Append(use_replacement_word ? replacement_word : word).Append(' ');
                use_replacement_word = false;
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
    /// <param name="height">Maximum height.</param>
    /// <returns>Wrapped text.</returns>
    internal static unsafe string WrapTextByChar(string text, SpriteFont whichFont, float width, float? height = null)
    {
        int maxlines = height is null ? 1000 : (int)height / whichFont.LineSpacing;
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
                        if (--maxlines <= 0)
                        {
                            return sb.ToString();
                        }
                        current_width = -whichFont.Spacing;
                        sb.Append(Environment.NewLine);
                        break;
                    default:
                        int glyph = GetGlyph(whichFont, ch);
                        if (glyph > -1 && glyph < whichFont.Glyphs.Length)
                        {
                            SpriteFont.Glyph* pWhichGlyph = pointerToGlyphs + glyph;
                            charwidth = pWhichGlyph->LeftSideBearing + pWhichGlyph->Width + pWhichGlyph->RightSideBearing;
                            proposedcharwidth = pWhichGlyph->RightSideBearing < 0
                                ? pWhichGlyph->LeftSideBearing + pWhichGlyph->Width
                                : charwidth;
                            if (current_width + proposedcharwidth + whichFont.Spacing > width)
                            {
                                if (--maxlines <= 0)
                                {
                                    return sb.ToString();
                                }
                                sb.AppendLine();
                                current_width = charwidth;
                            }
                            sb.Append(ch);
                            current_width += charwidth + whichFont.Spacing;
                        }
                        else
                        {
                            Monitor?.Log($"Glyph {ch} not accounted for!", LogLevel.Error);
                        }
                        break;
                }
            }
        }
        return sb.ToString();
    }

    /// <summary>
    /// Truncates a word to a given length, replacing the rest with "...".
    /// </summary>
    /// <param name="word">Word to truncate.</param>
    /// <param name="whichFont">Which font to use.</param>
    /// <param name="width">Width to wrap to.</param>
    /// <param name="trunchars">Characters to use to truncate with.</param>
    /// <returns>Truncated string + width.</returns>
    private static unsafe (string trucword, float width) TruncateWord(string word, SpriteFont whichFont, float width, string trunchars = "...")
    {
        StringBuilder sb = new();
        float current_width = -whichFont.Spacing + whichFont.MeasureString(trunchars).X;
        float charwidth = 0;
        float proposedcharwidth = 0;
        fixed (SpriteFont.Glyph* pointerToGlyphs = whichFont.Glyphs)
        {
            foreach (char ch in word)
            {
                int glyph = GetGlyph(whichFont, ch);
                if (glyph > -1 && glyph < whichFont.Glyphs.Length)
                {
                    SpriteFont.Glyph* pWhichGlyph = pointerToGlyphs + glyph;
                    charwidth = pWhichGlyph->LeftSideBearing + pWhichGlyph->Width + pWhichGlyph->RightSideBearing;
                    proposedcharwidth = pWhichGlyph->RightSideBearing < 0
                        ? pWhichGlyph->LeftSideBearing + pWhichGlyph->Width
                        : charwidth;
                    if (current_width + proposedcharwidth + whichFont.Spacing > width)
                    {
                        sb.Append(trunchars);
                        return (sb.ToString(), current_width + charwidth + whichFont.Spacing);
                    }
                    sb.Append(ch);
                    current_width += charwidth + whichFont.Spacing;
                }
                else
                {
                    Monitor?.Log($"Glyph {ch} not accounted for!", LogLevel.Error);
                }
            }
        }
        return (sb.ToString(), current_width);
    }
}

#endif