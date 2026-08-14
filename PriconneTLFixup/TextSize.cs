using System.Text;
using UnityEngine;

namespace PriconneTLFixup;

public class TextSize
{
    private readonly Dictionary<char, float> _characterWidths;

    private readonly TextMesh _textMesh;
    private readonly Renderer _renderer;

    public TextSize(TextMesh tm)
    {
        _textMesh = tm;
        _renderer = tm.GetComponent<Renderer>();
        _characterWidths = new Dictionary<char, float>();
        GetSpace();
    }

    private void GetSpace()
    {
        //the space can not be got alone
        var oldText = _textMesh.text;

        _textMesh.text = "a";
        var aw = _renderer.bounds.size.x;
        // ReSharper disable once Unity.InefficientPropertyAccess
        _textMesh.text = "a a";
        // ReSharper disable once Unity.InefficientPropertyAccess
        var cw = _renderer.bounds.size.x - 2 * aw;

        _characterWidths.Add(' ', cw);
        _characterWidths.Add('a', aw);

        _textMesh.text = oldText;
    }

    public float GetTextWidth(string s)
    {
        float w = 0;
        var oldText = _textMesh.text;

        foreach (var c in s)
        {
            if (_characterWidths.TryGetValue(c, out var cachedWidth))
            {
                w += cachedWidth;
            }
            else
            {
                _textMesh.text = "" + c;
                var cw = _renderer.bounds.size.x;
                _characterWidths.Add(c, cw);
                w += cw;
            }
        }

        _textMesh.text = oldText;
        return w;
    }

    public void FitToWidth(float wantedWidth)
    {
        if (Width <= wantedWidth) return;

        var oldText = _textMesh.text;
        _textMesh.text = "";
        
        var lines = oldText.Split('\n');
        var wrappedText = new StringBuilder(oldText.Length + lines.Length);

        foreach (var line in lines)
        {
            wrappedText.Append(WrapLine(line, wantedWidth)).Append('\n');
        }

        _textMesh.text = wrappedText.ToString();
    }

    private string WrapLine(string s, float w)
    {
        // need to check if smaller than maximum character length, really...
        if (w == 0 || s.Length <= 0) return s;

        float wordWidth = 0;
        float currentWidth = 0;

        var word = new StringBuilder();
        var newText = new StringBuilder(s.Length + 4);
        var oldText = _textMesh.text;

        for (var i = 0; i < s.Length; i++)
        {
            var c = s[i];

            float charWidth;
            if (_characterWidths.TryGetValue(c, out var cachedWidth))
            {
                charWidth = cachedWidth;
            }
            else
            {
                _textMesh.text = "" + c;
                charWidth = _renderer.bounds.size.x;
                _characterWidths.Add(c, charWidth);
                //here check if max char length
            }

            if (c == ' ' || i == s.Length - 1)
            {
                if (c != ' ')
                {
                    word.Append(c);
                    wordWidth += charWidth;
                }

                if (currentWidth + wordWidth < w)
                {
                    currentWidth += wordWidth;
                    newText.Append(word);
                }
                else
                {
                    currentWidth = wordWidth;
                    newText.Append(word.Replace(' ', '\n'));
                }

                word.Clear();
                wordWidth = 0;
            }

            word.Append(c);
            wordWidth += charWidth;
        }

        _textMesh.text = oldText;
        return newText.ToString();
    }

    public float Width => GetTextWidth(_textMesh.text);
    public float Height => _renderer.bounds.size.y;
}
