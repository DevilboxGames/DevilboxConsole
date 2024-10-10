using System;
using System.Linq;
using System.Text.RegularExpressions;
using DevilboxConsole.examples.Shared.scripts.interfaces;
using Godot;

namespace DevilboxConsole.examples.Shared.scripts.animation;

public partial class TextWriterAnimation : Node, INodeAnimation
{
    [Export]
    public bool IsPlaying { get; set; }
    [Export]
    public bool AutoPlay { get; set; }
    
    [Export]
    public float CharactersPerSecond { get; set; } = 10;
    
    [Export]
    public Curve WritingCurve { get; set; } = new Curve(){ _Data = {0, 1}};
    [Export]
    public string Text { get; set; } = "";
    [Export]
    public int StartingIndex { get; set; } = 0;

    [Export]
    public bool ShakeOnType { get; set; } = true;
    [Export]
    public Node ShakeAnimationNode { get; set; }

    [Export]
    public Font TextFont { get; set; }
    [Export]
    public int FontSize { get; set; }

    public event Action<Vector2> OnWriteCharacter;
    public RotateAnimation ShakeAnimation { get=> ShakeAnimationNode as RotateAnimation; }

    protected RichTextLabel ParentLabel { get => _parentNode as RichTextLabel; }
    private Node _parentNode;

    private double _progressTime;
    private int _progressCharacter;
    private string _progressText;
    
    private TextParagraph _paragraph;
    protected void WriteText(string text)
    {
        if (ParentLabel != null)
        {
            ParentLabel.Text = text;
        }
    }
    public override void _Ready()
    {
        _paragraph = new TextParagraph();
        _parentNode = GetParent();
        if (StartingIndex > 0)
        {
            _progressCharacter = StartingIndex;
            _progressText = Text.Substring(0, _progressCharacter);
        }
        
        WriteText(_progressText);
        
        if (AutoPlay)
        {
            Play();
        }
    }

    public override void _Process(double delta)
    {
        if (IsPlaying && _progressCharacter < Text.Length)
        {
            _paragraph.Clear();
            GD.Print($"Content width: { ParentLabel.GetContentWidth()}");
            _paragraph.Width = ParentLabel.Size.X / 2f;
            _paragraph.BreakFlags = TextServer.LineBreakFlag.Mandatory | TextServer.LineBreakFlag.WordBound;
            _progressTime += delta;
            if (_progressTime >= 1 / CharactersPerSecond)
            {
                _progressTime = 0;
                if (Text[_progressCharacter] == '[')
                {
                    _progressCharacter = Text.IndexOf(']', _progressCharacter);
                }
                _progressCharacter++;
                _progressText = Text.Substring(0, _progressCharacter);
                if (!string.IsNullOrWhiteSpace(_progressText))
                {
                    Regex bbcode = new Regex("\\[[^\\]]+\\]");
                    _paragraph.AddString(bbcode.Replace(_progressText, ""), ParentLabel.GetThemeFont("font"),
                        ParentLabel.GetThemeFontSize("font_size"));
                    int lineCount = _paragraph.GetLineCount() ;
                    if (lineCount > 0)
                    {
                        float bottomOfParagraph = 0;
                        for (int i = 0; i < lineCount; i++)
                        {
                            bottomOfParagraph += _paragraph.GetLineSize(i).Y * 0.4f;
                        }
                        Vector2 lineSize = _paragraph.GetLineSize(lineCount-1);
                        //lineSize.X = Math.Min(lineSize.X, ParentLabel.GetContentWidth());
                        lineSize.Y = bottomOfParagraph;// - lineSize.Y * 0.5f;
                        OnWriteCharacter?.Invoke(lineSize);
                    }

                    if (ShakeOnType && ShakeAnimation != null)
                    {
                        //Vector3 hitPos = (ShakeAnimation.GetParent() as Node3D).Transform * new Vector3(linerect.End.X, linerect.End.Y, 0);

                        ShakeAnimation.Restart();
                    }
                }

                WriteText(_progressText);
            }
            
        }
    }

    public void Play()
    {
        IsPlaying = true;
    }

    public void Stop()
    {
        IsPlaying = false;
    }

    public void Restart()
    {
        IsPlaying = true;
        _progressTime = 0;
        _progressCharacter = StartingIndex;
        _progressText = Text.Substring(0, _progressCharacter);
        WriteText(_progressText);
    }
}