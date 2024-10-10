using Godot;

namespace DevilboxConsole.examples.Shared.scripts.pickups;

public partial class AmmoPickup : ItemPickup
{
    [Export]
    public int Ammo { get; set; }

    public override bool HandlePickup(ExamplePlayer player)
    {
        if (player.Ammo < 100)
        {
            player.Ammo += Ammo;
            return true;
        }
        
        return false;
    }
}