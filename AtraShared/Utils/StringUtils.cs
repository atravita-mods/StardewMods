using System.Text;
using Microsoft.Xna.Framework.Graphics;

namespace AtraShared.Utils;

internal static class StringUtils
{
    internal static string ParseAndWrapText(string? text)
        => text is null ? string.Empty : ParseAndWrapText(text, Game1.dialogueFont, Game1.dialogueWidth);

    internal static string ParseAndWrapText(string? text, SpriteFont whichFont, float width)
    {
        if (text is null)
        {
            return string.Empty;
        }
        if (text.IndexOf(Dialogue.genderDialogueSplitCharacter) is int genderseperator && genderseperator > 0)
        {
            text = Game1.player.IsMale ? text[..genderseperator] : text[genderseperator..];
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
                switch (word)
                {
                    case "\r\n":
                    case "\r":
                    case "\n":
                        current_width = -whichFont.Spacing;
                        sb.Append(Environment.NewLine);
                        break;
                    default:
                        wordwidth += whichFont.MeasureString(word);
                        current_width += whichFont.Spacing + wordwidth;
                        if (current_width > width)
                        {
                            sb.Append(Environment.NewLine);
                            width = wordwidth;
                        }
                        sb.Append(word);
                }
            }
        }
        else
        {
            float current_width = -whichFont.Spacing;
            float charwidth = 0;
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
                        charwidth = whichFont.MeasureString(ch.ToString()).X;
                        current_width += whichFont.Spacing + charwidth;
                        if (current_width > width)
                        {
                            sb.Append(Environment.NewLine);
                            width = charwidth;
                        }
                        sb.Append(ch);
                        break;
                }
            }
        }
        return sb.ToString();
    }
}