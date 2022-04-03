using System.Linq.Expressions;
using System.Text;
using AtraBase.Toolkit.Reflection;
using Microsoft.Xna.Framework.Graphics;

namespace AtraShared.Utils;

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
        bool splitbyspaces;
        switch (LocalizedContentManager.CurrentLanguageCode)
        {
            case LocalizedContentManager.LanguageCode.ja:
            case LocalizedContentManager.LanguageCode.zh:
            case LocalizedContentManager.LanguageCode.th:
            case LocalizedContentManager.LanguageCode.mod when Game1.dialogueFont.Glyphs.Length > 4000:
                splitbyspaces = false;
                break;
            default:
                splitbyspaces = true;
                break;
        }
        StringBuilder sb = new();
        if (splitbyspaces)
        {
            string[] split = text.Split(' ');
            float current_width = -whichFont.Spacing;
            float wordwidth = 0;
            foreach (string word in split)
            {
                // This isn't quite right. I have no assurances on where the newline characters may fall.
                switch (word)
                {
                    case "\r\n":
                    case "\r":
                    case "\n":
                        current_width = -whichFont.Spacing;
                        sb.Append(Environment.NewLine);
                        break;
                    default:
                        if (LocalizedContentManager.CurrentLanguageCode is LocalizedContentManager.LanguageCode.fr && word.StartsWith("\n-"))
                        {
                            current_width = -whichFont.Spacing;
                            sb.Append(Environment.NewLine);
                            break;
                        }
                        wordwidth = whichFont.MeasureString(word).X;
                        current_width += whichFont.Spacing + wordwidth;
                        if (current_width > width)
                        {
                            sb.Append(Environment.NewLine);
                            current_width = wordwidth;
                        }
                        sb.Append(word).Append(' ');
                        break;
                }
            }
        }
        else
        {
            float current_width = -whichFont.Spacing;
            float charwidth = 0;
            float proposedcharwidth = 0;
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
                            SpriteFont.Glyph whichGlyph = whichFont.Glyphs[glyph];
                            charwidth = whichGlyph.LeftSideBearing + whichGlyph.Width + whichGlyph.RightSideBearing;
                            proposedcharwidth = whichGlyph.RightSideBearing < 0 ? whichGlyph.LeftSideBearing + whichGlyph.Width : charwidth;
                            if (current_width + proposedcharwidth > width)
                            {
                                sb.Append(Environment.NewLine);
                                current_width = charwidth;
                            }
                            sb.Append(ch);
                            current_width += charwidth;
                        }
                        break;
                }
            }
        }
        return sb.ToString();
    }
}