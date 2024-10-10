using Godot;
using DevilboxGames.DebugConsole;
using DevilboxConsole.examples.Shared.scripts.interfaces;

namespace DevilboxConsole.examples.Shared.scripts;
public partial class InteractableConsole : Node3D, IInteractableObject
{
    
    [Export]
    public SubViewport ConsoleViewport { get; set; }
    [Export]
    public StandardMaterial3D ConsoleScreenMaterial { get; set; }
    
    [Export]
    public MeshInstance3D ScreenMesh { get; set; }

    [Export]
    public int ScreenMaterialIndex { get; set; } = 0;
    [Export]
    public Node3D CameraPosition { get; set; }
    [Export]
    public bool Interactable { get; set; }

    [Export]
    public bool LimitCommands
    {
        get => Console?.LimitCommands ?? _limitCommands;
        set
        {
            _limitCommands = value;
            if (Console != null)
            {
                Console.LimitCommands = value;
            }
        }
    }

    [Export]
    public string DefaultOutput
    {
        get => Console?.DefaultOutput ?? _defaultOutput;
        set
        {
            _defaultOutput = value;
            if (Console != null)
            {
                Console.DefaultOutput = value;
            }
        }
    }

    [Export]
    public string ConsoleName
    {
        get => _consoleName;
        set
        {
            _consoleName = value;
            if (Console != null)
            {
                Console.Name = _consoleName;
            }
        }
    }

    [Export]
    public float CameraMoveTime { get; set; } = 0.2f; 

    [Export]
    public CommandConsole Console { get; set; }

    private bool _inUse { get; set; }
    private ExamplePlayerController _currentPlayer { get; set; }
    private Vector3 _playerOldCameraPosition { get; set; }
    private Basis _playerOldCameraRotation { get; set; }
    private Vector3 _playerNewCameraPosition { get; set; }
    private Basis _playerNewCameraRotation { get; set; }
    private bool _movingCamera;
    private double _movingCameraTime;
    private string _consoleName;
    private bool _limitCommands;
    private string _defaultOutput;
    public bool IsInteractable() => Interactable;

    public override void _Ready()
    {
        if (ConsoleViewport == null)
        {
            ConsoleViewport = new SubViewport();
            ConsoleViewport.Size = new Vector2I(512, 512);
            ConsoleViewport.Name = $"{Name}_Viewport";
            CanvasLayer canvasLayer = new CanvasLayer();
            canvasLayer.Name = $"{Name}_CanvasLayer";
            ConsoleViewport.AddChild(canvasLayer);
            Console = new CommandConsole();
            Console.Name = $"{Name}_Console";

            if (!string.IsNullOrWhiteSpace(DefaultOutput))
            {
                Console.DefaultOutput = DefaultOutput;
            }
            Console.LimitCommands = LimitCommands;
            canvasLayer.AddChild(Console);
            AddChild(ConsoleViewport);
        }
        ConsoleScreenMaterial = new StandardMaterial3D();
        ConsoleScreenMaterial.AlbedoTexture = ConsoleViewport.GetTexture();
        ScreenMesh.SetSurfaceOverrideMaterial(ScreenMaterialIndex, ConsoleScreenMaterial);
        //Screen.MaterialOverride = ConsoleScreenMaterial;
        Console.ConsoleName = ConsoleName;
        Console.Console.ConsoleName = ConsoleName;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (_movingCamera && _currentPlayer != null)
        {
            if (_inUse)
            {
                _movingCameraTime += delta;
                if (_movingCameraTime >= CameraMoveTime)
                {
                    _movingCamera = false;
                    Console.Console.GrabFocus();
                    _currentPlayer.Camera.GlobalPosition = _playerNewCameraPosition;
                    _currentPlayer.Camera.GlobalBasis = _playerNewCameraRotation;
                }
                else
                {
                    _currentPlayer.Camera.GlobalPosition = _playerOldCameraPosition.Lerp(_playerNewCameraPosition,
                        (float)(_movingCameraTime / CameraMoveTime));
                    _currentPlayer.Camera.GlobalBasis = _playerOldCameraRotation.Slerp(_playerNewCameraRotation,
                        (float)(_movingCameraTime / CameraMoveTime));
                }
            }
            else
            {
                _movingCameraTime += delta;
                if (_movingCameraTime >= CameraMoveTime)
                {
                    _movingCamera = false;
                    _currentPlayer.Camera.GlobalPosition = _playerOldCameraPosition;
                    _currentPlayer.Camera.GlobalBasis = _playerOldCameraRotation;
                    _currentPlayer.EnableControllers();
                }
                else
                {
                    Console.Console.ReleaseFocus();
                    _currentPlayer.Camera.GlobalPosition = _playerNewCameraPosition.Lerp(_playerOldCameraPosition,
                        (float)(_movingCameraTime / CameraMoveTime));
                    _currentPlayer.Camera.GlobalBasis = _playerNewCameraRotation.Slerp(_playerOldCameraRotation,
                        (float)(_movingCameraTime / CameraMoveTime));
                }
            }
        }

        if (_inUse)
        {
            if (Input.IsActionJustPressed("ui_cancel"))
            {
                Cancel(_currentPlayer);
            }
        }
    }

    public override void _UnhandledKeyInput(InputEvent @event)
    {
        if (_inUse)
        {
            ConsoleViewport.PushInput(@event);
            //Console.Console.HandleInput(@event);
        }
    }

    public void Interact(Node instigator)
    {
        if (!Interactable && !_inUse)
        {
            return;
        }

        if (instigator is ExamplePlayerController consolePlayer)
        {
            _currentPlayer = consolePlayer;
            _inUse = true;
            _currentPlayer.DisableControllers();
            
            _playerOldCameraPosition = _currentPlayer.Camera.GlobalPosition;
            _playerOldCameraRotation = _currentPlayer.Camera.GlobalBasis;
            _playerNewCameraPosition = CameraPosition.GlobalPosition;
            _playerNewCameraRotation = CameraPosition.GlobalBasis;
            _movingCamera = true;
            _movingCameraTime = 0;
        }
    }
    public void Cancel(Node instigator)
    {
        if (!Interactable)
        {
            return;
        }
        
        _inUse = false;
        _movingCamera = true;
        _movingCameraTime = 0;
    }
}