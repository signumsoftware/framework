using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Text.RegularExpressions;
using System.Windows;

namespace Signum.Windows.SyntaxHighlight
{
    public interface IHighlighter
    {
        void Highlight(FormattedText text);
    }

    public class Highlighter : IHighlighter
    {
        public List<HighlightRule> Rules { get; set; }

        public Highlighter()
        {
            Rules = new List<HighlightRule>();
        }

        public void Highlight(FormattedText text)
        {
            foreach (var item in Rules)
            {
                item.Highlight(text);
            }
        }
    }

    public abstract class HighlightRule
    {
        public RuleFormatter Formatter { get; set; }

        public abstract void Highlight(FormattedText text);
    }

    public class WordRule : HighlightRule
    {
        public bool IgnoreCase { get; set; }

        public List<string> Words { get; set; }

        public WordRule()
        {
        }

        public WordRule(string words)
        {
            Words = Regex.Split(words, "\\s+").Where(w => !string.IsNullOrWhiteSpace(w)).ToList();
        }

        static Regex wordsRgx = new Regex("[a-zA-Z_][a-zA-Z0-9_]*");
        public override void Highlight(FormattedText text)
        {
            foreach (Match m in wordsRgx.Matches(text.Text))
            {
                if (Words.Contains(m.Value, IgnoreCase ? StringComparer.InvariantCultureIgnoreCase : StringComparer.InvariantCulture))
                {
                    Formatter.Format(text, m.Index, m.Length);
                }
            }
        }

    }

    public class LineRule : HighlightRule
    {
        public string LineStart { get; set; }

        public override void Highlight(FormattedText text)
        {
            Regex lineRgx = new Regex(Regex.Escape(LineStart) + ".*");
            foreach (Match m in lineRgx.Matches(text.Text))
            {
                Formatter.Format(text, m.Index, m.Length);
            }
        }
    }

    public class RegexRule : HighlightRule
    {
        public Regex Regex { get; set; }

        public override void Highlight(FormattedText text)
        {
            foreach (Match m in Regex.Matches(text.Text))
            {
                Formatter.Format(text, m.Index, m.Length);
            }
        }
    }

    public class RuleFormatter
    {
        public Brush Foreground { get; set; }
        public FontWeight FontWeight { get; set; }
        public FontStyle FontStyle { get; set; }

        public RuleFormatter(string foreground)
        {
            Foreground = (Brush)new BrushConverter().ConvertFrom(foreground);
        }

        internal void Format(FormattedText text, int index, int length)
        {
            text.SetForegroundBrush(Foreground, index, length);
            text.SetFontWeight(FontWeight, index, length);
            text.SetFontStyle(FontStyle, index, length);
        }
    }
}
