#if TOOLS
using Godot;
using System;
using DevilboxGames.DebugConsole;

[Tool]
public partial class DebugConsolePlugin : EditorPlugin
{
	private DebugConsole _console;
	private const string DebugConsoleNodeName = "DebugConsole";
	private const string CommandConsoleNodeName = "CommandConsole";
	private const string CommandProcessorNodeName = "CommandProcessor";
	private const string AutoloadName = "DebugConsoleScene";
	private const string ConsoleSceneFilename = "res://addons/devilbox_console/scenes/DebugConsoleScene.tscn";
	public override void _EnterTree()
	{
		var debugscript = GD.Load<Script>("res://addons/devilbox_console/source/DebugConsoleBase.cs");
		var debugicon = GD.Load<Texture2D>("res://addons/devilbox_console/textures/DebugConsoleIcon.png");
		var commandscript = GD.Load<Script>("res://addons/devilbox_console/source/CommandConsole.cs");
		var commandicon = GD.Load<Texture2D>("res://addons/devilbox_console/textures/CommandConsoleIcon.png");
		var commandprocessorscript = GD.Load<Script>("res://addons/devilbox_console/source/CommandProcessor.cs");
		var commandprocessoricon = GD.Load<Texture2D>("res://addons/devilbox_console/textures/CommandProcessorIcon.png");
		AddCustomType(DebugConsoleNodeName, "Container", debugscript, debugicon);
		AddCustomType(CommandConsoleNodeName, "Container", commandscript, commandicon);
		AddCustomType(CommandProcessorNodeName, "Node", commandprocessorscript, commandprocessoricon);
	}

	public override void _ExitTree()
	{
		RemoveCustomType(DebugConsoleNodeName);
		RemoveCustomType(CommandConsoleNodeName);
		RemoveCustomType(CommandProcessorNodeName);
	}
}
#endif
