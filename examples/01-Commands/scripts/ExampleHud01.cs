using Godot;
using System;
using DevilboxConsole.examples.Shared.scripts.hud;
using DevilboxGames.DebugConsole;

public partial class ExampleHud01 : ExampleHudBase
{
	

	[ConsoleCommand("SetStats", Description = "Sets the health and ammo of the player", InstanceCommand = true)]
	[ConsoleCommandParameter("SetStats", 0, "Health", typeof(float))]
	[ConsoleCommandParameter("SetStats", 1, "Ammo", typeof(int))]
	public void SetStats(float health, int ammo)
	{
		Ammo = ammo;
		Health = health;
	}
	
	[ConsoleCommand("AddHealth", Description = "Adds health to the player", InstanceCommand = true)]
	[ConsoleCommandParameter("AddHealth", 0, "Health", typeof(float))]
	public void AddHealth(float health)
	{
		Health += health;
	}
	[ConsoleCommand("SubtractHealth", Description = "Subtract health to the player", InstanceCommand = true)]
	[ConsoleCommandParameter("SubtractHealth", 0, "Health", typeof(float))]
	public void SubtractHealth(float health)
	{
		Health -= health;
	}
	
	[ConsoleCommand("AddAmmo", Description = "Adds ammo to the player", InstanceCommand = true)]
	[ConsoleCommandParameter("AddAmmo", 0, "Ammo", typeof(int))]
	public void AddAmmo(int ammo)
	{
		Ammo += ammo;
	}
	[ConsoleCommand("SubtractAmmo", Description = "Subtract ammo to the player", InstanceCommand = true)]
	[ConsoleCommandParameter("SubtractAmmo", 0, "Ammo", typeof(int))]
	public void SubtractAmmo(int ammo)
	{
		Ammo -= ammo;
	}
}
