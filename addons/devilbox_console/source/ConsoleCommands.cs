using System.Linq;
using Godot;

namespace DevilboxGames.DebugConsole;

public static class ConsoleCommands
{
    [ConsoleCommand("Quit", Description = "Quit the game", SystemCommand = true)]
    [ConsoleCommandParameter("Quit", -1, "root", null, Usage = ConsoleCommandParameterAttribute.ConsoleCommandParameterUsage.Node, DefaultValue = "/root")]
    public static void Quit(Node root)
    {
        Callable.From(()=> root.GetTree().Quit()).CallDeferred();
    }
    [ConsoleCommand("Exit", Description = "Quit the game", SystemCommand = true)]
    [ConsoleCommandParameter("Exit", -1, "root", null, Usage = ConsoleCommandParameterAttribute.ConsoleCommandParameterUsage.Node, DefaultValue = "/root")]
    public static void Exit(Node root)
    {
        Callable.From(()=> root.GetTree().Quit()).CallDeferred();
    }
    [ConsoleCommand("LoadScene", Description = "Loads a scene", SystemCommand = true)]
    [ConsoleCommandParameter("LoadScene", -1, "root", null, Usage = ConsoleCommandParameterAttribute.ConsoleCommandParameterUsage.Node, DefaultValue = "/root")]
    [ConsoleCommandParameter("LoadScene", 0, "Scene name", typeof(string), Usage = ConsoleCommandParameterAttribute.ConsoleCommandParameterUsage.Resource, Prefix = "res://scenes/", Filters = new string[]{".tscn"}, DefaultValue = null, Optional = false)]
    public static void LoadScene(Node root, string sceneName)
    {
        if (!sceneName.StartsWith("res://"))
        {
            sceneName = $"res://{(!sceneName.StartsWith("scenes/") && !sceneName.StartsWith("/scenes/") ?  "scenes/" : "")}{sceneName}";
        }
        Callable.From(()=>
        {
            GD.Print("Loaded scene: " + sceneName); 
            root.GetTree().ChangeSceneToFile(sceneName);
        }).CallDeferred();
    }
    [ConsoleCommand("LoadPrefab", Description = "Loads a scene", SystemCommand = true)]
    [ConsoleCommandParameter("LoadPrefab", -1, "root", null, Usage = ConsoleCommandParameterAttribute.ConsoleCommandParameterUsage.Node, DefaultValue = "/root")]
    [ConsoleCommandParameter("LoadPrefab", 0, "Scene name", typeof(string), Usage = ConsoleCommandParameterAttribute.ConsoleCommandParameterUsage.Resource, Prefix = "res://prefabs/", Filters = new string[]{".tscn"}, DefaultValue = null, Optional = false)]
    [ConsoleCommandParameter("LoadPrefab", 1, "ParentNode", null, Usage = ConsoleCommandParameterAttribute.ConsoleCommandParameterUsage.Node, DefaultValue = "/root/World", Optional = false)]
    public static void LoadPrefab(Node root, string sceneName, Node parentNode)
    {
        if (!sceneName.StartsWith("res://"))
        {
            sceneName = $"res://{sceneName}";
        }
        Callable.From(()=>
        {
            GD.Print("Loaded prefab: " + sceneName); 
            parentNode.AddChild(GD.Load<PackedScene>(sceneName).Instantiate());
        }).CallDeferred();
    }

}