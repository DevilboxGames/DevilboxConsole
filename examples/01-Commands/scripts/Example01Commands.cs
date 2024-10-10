using DevilboxConsole.examples.Shared.scripts.pickups;
using DevilboxGames.DebugConsole;
using Godot;

namespace DevilboxConsole.examples._01_Commands.scripts;

public partial class Example01Commands : Node
{
    [Export]
    public PackedScene HealthPrefab { get=>_healthPrefab; set=>_healthPrefab=value; }
    
    [Export]
    public PackedScene AmmoPrefab { get=>_ammoPrefab; set=>_ammoPrefab=value; }
    [Export]
    public Node3D Player { get=>_player; set=>_player=value; }
    [Export]
    public Node3D SpawnWrapper { get=>_spawnWrapper; set=>_spawnWrapper=value; }
    
    
    static PackedScene _healthPrefab;
    
    
    static PackedScene _ammoPrefab;
    
    static Node3D _player;
    
    static Node3D _spawnWrapper;
    
    
    [ConsoleCommand("ThrowHealth", Description = "Throws a health box")]
    public static void ThrowHealth()
    {
        HealthPickup pickup = _healthPrefab.Instantiate<HealthPickup>();
        Vector3 spawnPosition = _player.GlobalPosition + Vector3.Up * 0.5f;
        Vector3 throwDirection = -_player.GlobalBasis.Z + Vector3.Up * 0.5f;
        pickup.DelayPickupOnSpawn = true;
        pickup.DelayPickupTime = 1.5f;

        _spawnWrapper.AddChild(pickup);
        RigidBody3D rigidBody3D = (pickup.FindChild("RigidBody3D") as RigidBody3D);
        rigidBody3D.LinearVelocity = throwDirection.Normalized() * 10;
        rigidBody3D.GlobalPosition = pickup.GlobalPosition = spawnPosition;
        
    }
    [ConsoleCommand("ThrowAmmo", Description = "Throws a ammo box")]
    public static void ThrowAmmo()
    {
        AmmoPickup pickup = _ammoPrefab.Instantiate<AmmoPickup>();
        Vector3 spawnPosition = _player.GlobalPosition + Vector3.Up * 0.5f;
        Vector3 throwDirection = -_player.GlobalBasis.Z + Vector3.Up * 0.5f;
        pickup.DelayPickupOnSpawn = true;
        pickup.DelayPickupTime = 1.5f;

        _spawnWrapper.AddChild(pickup);
        RigidBody3D rigidBody3D = (pickup.FindChild("RigidBody3D") as RigidBody3D);
        rigidBody3D.LinearVelocity = throwDirection.Normalized() * 10;
        rigidBody3D.GlobalPosition = pickup.GlobalPosition =  spawnPosition;
        
    }
}