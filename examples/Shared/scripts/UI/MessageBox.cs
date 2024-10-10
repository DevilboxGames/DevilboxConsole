using DevilboxConsole.examples.Shared.scripts.animation;
using DevilboxConsole.examples.Shared.scripts.interfaces;
using Godot;
using Godot.Collections;

namespace DevilboxConsole.examples.Shared.scripts.hud;
[Tool]
public partial class MessageBox : Node3D
{
    [Export(PropertyHint.MultilineText)]
    public string Message
    {
        get => _message;
        set
        {
            _message = value;
            ApplyText();
        }
    }

    [Export]
    public bool AnimateText
    {
        get => _useAnimatedText;
        set
        {
            _useAnimatedText = value;
            ApplyText();
        }
    }

    [Export]
    public Vector2 Size
    {
        get => _messageBoxSize;
        set
        {
            _messageBoxSize = value;
            Scale = new Vector3(_messageBoxSize.X, _messageBoxSize.Y, 1);
            if (MessageSubViewport != null/* && MessageSubViewport.GetTexture() != null*/)
            {
                MessageSubViewport.Size = (Vector2I)(_viewportSize * _messageBoxSize);
            }
        }
    }


    [ExportGroup("Nodes")]
    [Export]
    public RichTextLabel MessageControl { get; set; }
    [Export]
    public NinePatchRect MessageBoxBackground { get; set; }
    [Export]
    public SubViewport MessageSubViewport { get; set; }
    [Export]
    public MeshInstance3D MessageBoxMesh { get; set; }
    [Export]
    public Array<Node> ShowAnimations { get; set; } 
    [Export]
    public Node TypingAnimationNode { get; set; }
    [Export]
    public Node ShakeAnimationNode { get; set; }
    [Export]
    public bool ShakeOnType { get; set; } = true;
    [Export]
    public Node3D DebugCursorNode {get;set;}
    
    public TextWriterAnimation TypingAnimation
    {
        get => TypingAnimationNode as TextWriterAnimation;
    }
    public RotateAnimation ShakeAnimation
    {
        get => ShakeAnimationNode as RotateAnimation;
    }

    private string _message = "This is a [color=red]Important[/color] message.";
    private Vector2 _messageBoxSize = new Vector2(2, 1);
    private Vector2I _viewportSize = new Vector2I(1024, 128);
    public bool _useAnimatedText;

    public override void _Ready()
    {
        Message = _message;
        Size = _messageBoxSize;
        StandardMaterial3D messageBoxMaterial = new StandardMaterial3D();
        //messageBoxMaterial.BillboardMode = BaseMaterial3D.BillboardModeEnum.FixedY;
        messageBoxMaterial.Transparency = BaseMaterial3D.TransparencyEnum.AlphaScissor;
        messageBoxMaterial.BillboardKeepScale = true;
        messageBoxMaterial.AlphaScissorThreshold = 0.6f;
        messageBoxMaterial.AlbedoTexture = MessageSubViewport.GetTexture();
        messageBoxMaterial.DisableReceiveShadows = true;
        messageBoxMaterial.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
        MessageBoxMesh.MaterialOverride = messageBoxMaterial;
        if (TypingAnimation != null)
        {
            TypingAnimation.OnWriteCharacter += PlayImpactAnim;
        }
        base._Ready();
    }

    private void PlayImpactAnim(Vector2 lineSize)
    {
        if (ShakeOnType && ShakeAnimation != null)
        {
            Vector2 textEnd = (lineSize + new Vector2(2,2)) * new Vector2(1.0f / MessageSubViewport.Size.X, 1.0f / MessageSubViewport.Size.Y);
            Vector3 charPosition = new Vector3(textEnd.X * 2, -textEnd.Y, 0);
            Vector3 meshBounds = MessageBoxMesh.GetAabb().Size;
            charPosition.X -= 0.5f * meshBounds.X;
            charPosition.Y += meshBounds.Y;
            if (DebugCursorNode != null)
            {
                DebugCursorNode.Position =  charPosition;
            }
            Vector3 directionToChar = charPosition.Normalized();
            Vector3 rotationAxis = directionToChar.Cross(Vector3.Forward);
            
            ShakeAnimation.Axis = rotationAxis;
            ShakeAnimation.Restart();
        }
    }

    public void ShowMessage()
    {
        Show();
        foreach (var animation in ShowAnimations)
        {
            (animation as INodeAnimation).Restart();
        }
    }

    public void CloseMessage()
    {
        Hide();
    }

    protected void ApplyText()
    {
        
        if (MessageControl != null)
        {
            if (!_useAnimatedText)
            {
                TypingAnimation?.Stop();
                MessageControl.SetText(_message);
            }
            else if(TypingAnimation != null)
            {
                TypingAnimation.Text = _message;
                TypingAnimation.Restart();
            }
        }
    }
}