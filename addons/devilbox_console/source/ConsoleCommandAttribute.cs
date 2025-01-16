using System;

namespace DevilboxGames.DebugConsole;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public class ConsoleCommandAttribute : Attribute
{
    public string CommandName { get; set; }
    public string Description { get; set; } = "";
    public string OnlyForConsole { get; set; }
    public bool InstanceCommand { get; set; }
    public bool SystemCommand { get; set; }

    public ConsoleCommandAttribute(string commandName)
    {
        CommandName = commandName;
    }
}
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public class ConsoleCommandParameterAttribute : Attribute
{
    public enum ConsoleCommandParameterUsage
    {
        Default,
        Node,
        Resource,
        Special
    }
    public string CommandName { get; set; }
    public string Description { get; set; } = "";
    public int Order { get; set; }
    public string ParameterName { get; set; }
    public string Prefix { get; set; }
    public string[] Filters { get; set; }
    public Type ParameterType { get; set; }
    public object DefaultValue { get; set; }
    public bool Optional { get; set; }
    public ConsoleCommandParameterUsage Usage { get; set; } = ConsoleCommandParameterUsage.Default;
    public ConsoleCommandParameterAttribute(string commandName, int order, string parameterName, Type parameterType)
    {
        CommandName = commandName;
        Order = order;
        ParameterName = parameterName;
        ParameterType = parameterType;
        
    }
}
