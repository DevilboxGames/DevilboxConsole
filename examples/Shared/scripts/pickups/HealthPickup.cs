using Godot;

namespace DevilboxConsole.examples.Shared.scripts.pickups;

public partial class HealthPickup : ItemPickup
{
    [Export]
    public float Health { get; set; }

    public override bool HandlePickup(ExamplePlayer player)
    {
        if (player.Health < 100)
        {
            player.Health += Health;
            return true;
        }
        
        return false;
    }
}