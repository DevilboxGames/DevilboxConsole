using Godot;

namespace DevilboxConsole.examples.Shared.scripts;

public partial class ItemPickup : Godot.Node3D
{
    [ExportGroup("Nodes")]
    [Export]
    public Area3D PickupArea { get; set; }
    [Export]
    public Node Wrapper { get; set; }
    [Export]
    public GpuParticles3D PickupParticles { get; set; }

    [Export]
    public GpuParticles3D SpawnParticles { get; set; }

    [ExportGroup("Spawn Settings")]
    [Export]
    public bool WaitToSpawn { get; set; } = false;

    [Export]
    public float SpawnDelay { get; set; } = 1;
    [Export]
    public bool Respawn { get; set; }

    [Export]
    public float RespawnTime { get; set; } = 2;

    [Export]
    public bool DelayPickupOnSpawn { get; set; } = true;
    [Export]
    public float DelayPickupTime { get; set; } = 2;


    private Timer _pickUpTimer;
    private Timer _spawnTimer;
    private bool _canPickup;
    public override void _Ready()
    {
        PickupArea.BodyEntered += PickupAreaOnBodyEntered;
        
        _pickUpTimer = GetNode<Timer>("PickupDelayTimer");
        _pickUpTimer.Timeout += EnablePickup;
        
        _spawnTimer = GetNode<Timer>("SpawnTimer");
        _spawnTimer.Timeout += Spawn;
        
        if (WaitToSpawn)
        {
            DeSpawn();
            _spawnTimer.WaitTime = SpawnDelay;
            _spawnTimer.Start();
        }
        HandlePickupDelayOnSpawn();
    }

    public void EnablePickup()
    {
        GD.Print("EnablePickup");
        _canPickup = true;
    }
    public void DisablePickup()
    {
        GD.Print("DisablePickup");
        _canPickup = false;
    }
    public void Spawn()
    {
        HandlePickupDelayOnSpawn();
        AddChild(Wrapper);
        SpawnParticles.Restart();
    }

    private void HandlePickupDelayOnSpawn()
    {
        if (DelayPickupOnSpawn)
        {
            DisablePickup();
            _pickUpTimer.WaitTime = DelayPickupTime;
            _pickUpTimer.Start();
        }
    }

    public void DeSpawn()
    {
        RemoveChild(Wrapper);
    }
    
    private void PickupAreaOnBodyEntered(Node3D body)
    {
        if (!_canPickup)
        {
            GD.Print("Can't pick up!");
            return;
        }
        GD.Print("Can pickup");
        if (body is ExamplePlayer player)
        {
            bool isPickedUp = HandlePickup(player);

            if (isPickedUp)
            {
                PickupParticles.Restart();
                CallDeferred("DeSpawn");
                if (Respawn)
                {
                    _spawnTimer.WaitTime = RespawnTime;
                    _spawnTimer.Start();
                }
            }
        }
    }

    public virtual bool HandlePickup(ExamplePlayer player)
    {
        return true;
    }
}