using System.Collections;
using UnityEngine;

namespace PriconneTLFixup;

public class TextSize
{
    private readonly Hashtable _dict; //map character -> width

    private readonly TextMesh _textMesh;
    private readonly Renderer _renderer;

    public TextSize(TextMesh tm)
    {
        _textMesh = tm;
        _renderer = tm.GetComponent<Renderer>();
        _dict = new Hashtable();
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

        _dict.Add(' ', cw);
        _dict.Add('a', aw);

        _textMesh.text = oldText;
    }

    public float GetTextWidth(string s)
    {
        var charList = s.ToCharArray();
        float w = 0;
        char c;
        var oldText = _textMesh.text;

        for (var i = 0; i < charList.Length; i++)
        {
            c = charList[i];

            if (_dict.ContainsKey(c))
            {
                w += (float)_dict[c]!;
            }
            else
            {
                _textMesh.text = "" + c;
                var cw = _renderer.bounds.size.x;
                _dict.Add(c, cw);
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

        foreach (var line in lines)
        {
            _textMesh.text += WrapLine(line, wantedWidth) + "\n";
        }
    }

    private string WrapLine(string s, float w)
    {
        // need to check if smaller than maximum character length, really...
        if (w == 0 || s.Length <= 0) return s;

        var charList = s.ToCharArray();

        float wordWidth = 0;
        float currentWidth = 0;

        var word = "";
        var newText = "";
        var oldText = _textMesh.text;

        for (var i = 0; i < charList.Length; i++)
        {
            var c = charList[i];

            float charWidth;
            if (_dict.ContainsKey(c))
            {
                charWidth = (float)_dict[c]!;
            }
            else
            {
                _textMesh.text = "" + c;
                charWidth = _renderer.bounds.size.x;
                _dict.Add(c, charWidth);
                //here check if max char length
            }

            if (c == ' ' || i == charList.Length - 1)
            {
                if (c != ' ')
                {
                    word += c.ToString();
                    wordWidth += charWidth;
                }

                if (currentWidth + wordWidth < w)
                {
                    currentWidth += wordWidth;
                    newText += word;
                }
                else
                {
                    currentWidth = wordWidth;
                    newText += word.Replace(" ", "\n");
                }

                word = "";
                wordWidth = 0;
            }

            word += c.ToString();
            wordWidth += charWidth;
        }

        _textMesh.text = oldText;
        return newText;
    }

    public float Width => GetTextWidth(_textMesh.text);
    public float Height => _renderer.bounds.size.y;
}