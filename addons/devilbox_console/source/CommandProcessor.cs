using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using DevilboxGames.DebugConsole.extensions;
using Godot;

namespace DevilboxGames.DebugConsole;

public partial class CommandProcessor : Node
{
    [Export]
    public string ConsoleName { get; set; }
    [Export]
    public bool LimitCommands { get; set; }
    
    public event Action<string, LogLevel, string> OnLog;
    public event Action<string> OnAddHistory;
    
    protected Dictionary<(Type type, string commandName), (MethodInfo method, ConsoleCommandAttribute commandAttribute,
        IEnumerable<ConsoleCommandParameterAttribute> parameterAttributes)> _commands;

    
    protected Dictionary<string, ConsoleBinding> _bindings = new Dictionary<string, ConsoleBinding>();

    public void LogWarning(string text, string prefix = "") => Log(text, LogLevel.Warning, prefix);
    public void LogError(string text, string prefix = "") => Log(text, LogLevel.Error, prefix);
    public void Log(string text, LogLevel logLevel = LogLevel.Information, string prefix="") => OnLog?.Invoke(text, logLevel, prefix);

    public override void _Ready()
    {
        var commandMethods = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .SelectMany(t => t.GetMethods())
            .Where(m =>
            {
                bool hasAttribute = m.CustomAttributes.Any(ca => ca.AttributeType == typeof(ConsoleCommandAttribute));
                if (hasAttribute)
                {
                    GD.Print($"DebugConsole: {m.Name} has attribute");
                }
                else
                {
                    return false;
                }
                return (m.GetCustomAttribute<ConsoleCommandAttribute>()?.OnlyForConsole?.ToLowerInvariant() ==
                    ConsoleName?.ToLowerInvariant() || (m.GetCustomAttribute<ConsoleCommandAttribute>()?.SystemCommand ?? false) || !LimitCommands &&
                    string.IsNullOrWhiteSpace(m.GetCustomAttribute<ConsoleCommandAttribute>()?.OnlyForConsole));
            }).ToArray();
        if (commandMethods == null || commandMethods.Count() < 1)
        {
            GD.PrintErr("DebugConsole: There's no command methods!");
        }
        _commands = commandMethods?.Select(m => (m, m.GetCustomAttribute<ConsoleCommandAttribute>(),
            m.GetCustomAttributes<ConsoleCommandParameterAttribute>())
        )?.DistinctBy(cmd=>(cmd.Item2.InstanceCommand ? cmd.m.DeclaringType : null , cmd.Item2.CommandName)).ToDictionary(cmd=> (cmd.Item2.InstanceCommand ? cmd.m.DeclaringType : null , cmd.Item2.CommandName));

    }

    public string AutoComplete( string currentCommand, ref int caretPosition)
    {
        if (currentCommand.Length > 0)
        {
            Stack<ConsoleToken> tokenStack = new Stack<ConsoleToken>();

            ConsoleToken commandToken = ConsoleTokenizer.Tokenize(currentCommand);
            tokenStack.Push(commandToken);
                            
            ConsoleToken currentToken = commandToken.SubTokens[0];
            int tokenPosition = 0;
                            
            while(currentToken != null && tokenPosition < caretPosition)
            {
                if (tokenPosition + currentToken.TokenValue.Length >= caretPosition)
                {
                    if (currentToken.SubTokens?.Count == 0)
                    {
                        tokenPosition += currentToken.TokenValue.Length;
                        break;
                    }

                    tokenPosition += currentToken.TokenValue.Substring(0,
                        currentToken.TokenValue.IndexOf(currentToken.SubTokens[0].TokenValue)).Length;
                    tokenStack.Push(currentToken);
                    currentToken = currentToken.SubTokens[0];
                }
                else
                {
                    tokenPosition += currentToken.TokenValue.Length;
                    var parentToken = tokenStack.Peek();
                    int tokenIndex = parentToken.SubTokens.IndexOf(currentToken);
                    if (tokenIndex == parentToken.SubTokens.Count-1)
                    {
                                        
                        break;
                    }

                    currentToken = parentToken.SubTokens[tokenIndex + 1];
                    tokenPosition++;
                }
            }

            ConsoleToken parentCommandCollection = tokenStack.Count > 0 ? tokenStack.Peek() : null;
            if (parentCommandCollection.SubTokens.First().TokenValue != "bind" && parentCommandCollection != null && parentCommandCollection.TokenType == ConsoleTokenType.CommandCollection && parentCommandCollection.SubTokens.First() != currentToken)
            {
                
                var cmds = _commands.OrderBy(c=>c.Key.commandName).FirstOrDefault(
                    c => c.Key.commandName == parentCommandCollection.SubTokens.First().TokenValue);

                int paramIndex = 0;
                foreach (ConsoleToken subToken in parentCommandCollection.SubTokens)
                {
                    if (subToken == parentCommandCollection.SubTokens.First())
                    {
                        continue;
                    }

                    if (subToken == currentToken)
                    {
                        var param = cmds.Value.parameterAttributes.FirstOrDefault(p=>p.Order == paramIndex);
                        if (param?.Usage == ConsoleCommandParameterAttribute.ConsoleCommandParameterUsage.Resource)
                        {
                            string prefix = param.Prefix;
                            string[] filters = param.Filters;
                            string resName = currentToken.TokenValue;
                            if (!prefix.StartsWith("res://"))
                            {
                                prefix = $"res://{(prefix.TrimStart('/'))}";
                            }
                            AutoCompleteResource(currentToken, ref currentCommand, ref caretPosition, prefix, filters);
                            return currentCommand;
                        }
                        break;
                    }
                    
                    paramIndex++;
                }

            }
            if (currentToken?.TokenType == ConsoleTokenType.Node || currentToken?.TokenType == ConsoleTokenType.NodeProperty || currentToken?.TokenType == ConsoleTokenType.NodeCommand)
            {
                AutoCompleteNode(currentToken, ref currentCommand, ref caretPosition);
            }
            else if (currentToken.TokenValue.StartsWith("res://"))
            {
                AutoCompleteResource(currentToken, ref currentCommand, ref caretPosition);
            }
            else if (currentToken != null && _commands.Any(cmd=> cmd.Key.commandName.StartsWith(currentToken.TokenValue, true, CultureInfo.InvariantCulture)))
            {
                AutoCompleteCommand(currentToken, ref currentCommand, ref caretPosition);
            }
        }

        return currentCommand;
    }

    private void AutoCompleteCommand(ConsoleToken currentToken, ref string CurrentCommand, ref int CaretPosition)
    {
        var cmds = _commands.OrderBy(c=>c.Key.commandName).Where(
            c => c.Key.commandName.StartsWith(currentToken.TokenValue, true, CultureInfo.InvariantCulture) && (c.Key.type == null || !c.Value.commandAttribute.InstanceCommand));

        if (!cmds.Any())
        {
            return;
        }
        
        if (cmds.Count() > 1)
        {
            string commonStart = cmds.Select(c => c.Key.commandName).Aggregate((a,b)=>a.GetCommonStart(b));

            Log("Possible commands:");
            foreach (var possibleCommand in cmds)
            {
                Log($"\t{possibleCommand.Key.commandName}");
            }
            CurrentCommand = CurrentCommand.Substring(0, currentToken.TokenLocation) +
                             commonStart +
                             CurrentCommand.Substring(currentToken.TokenLocation +
                                                      currentToken.TokenValue.Length);
                                    
            CaretPosition = currentToken.TokenLocation + commonStart.Length;
        }
        else
        {
            var cmd = cmds.First();
            CurrentCommand = CurrentCommand.Substring(0, currentToken.TokenLocation) +
                             cmd.Key.commandName + " " +
                             CurrentCommand.Substring(currentToken.TokenLocation +
                                                      currentToken.TokenValue.Length);
            CaretPosition = currentToken.TokenLocation + 1 + cmd.Key.commandName.Length;
        }
    }
    private void AutoCompleteResource(ConsoleToken currentToken, ref string CurrentCommand, ref int CaretPosition, string pathPrefix = "", string[] filters = null)
    {
        if (!pathPrefix.StartsWith("res://") && !currentToken.TokenValue.StartsWith("res://"))
        {
            pathPrefix = "res://" + pathPrefix;
        }
        
        string enteredPath = currentToken.TokenValue;
        //int pathSubstringStart = enteredPath.StartsWith("res://") ? 6 : 0;
        //int pathSubstringLength = enteredPath.LastIndexOf('/') - pathSubstringStart;
        string startDir = enteredPath.Substring(0, enteredPath.LastIndexOf('/')+1);
        string completionPart = enteredPath.Substring(enteredPath.LastIndexOf('/') + 1);
        
        DirAccess dir = DirAccess.Open(pathPrefix+startDir);
        
        if (!string.IsNullOrWhiteSpace(completionPart) && dir.DirExists(completionPart))
        {
            startDir = enteredPath;
            dir = DirAccess.Open(pathPrefix + startDir);
            completionPart = "";
        }

        IEnumerable<string> files = dir.GetFiles();
        IEnumerable<string> folders = dir.GetDirectories();

        if (!string.IsNullOrWhiteSpace(completionPart))
        {
            files = files.Where(f=>f.StartsWith(completionPart, true, CultureInfo.InvariantCulture) && (filters == null || filters.Length == 0 || filters.Any(f2=> f2 == Path.GetExtension(f)))).ToArray();
            folders = folders.Where(f => f.StartsWith(completionPart, true, CultureInfo.InvariantCulture)).ToArray();
        }
        var foldersAndFiles = folders.Concat(files);
        if (foldersAndFiles.Count() > 1)
        {
            string commonStart = $"{startDir}{(startDir.EndsWith("/") ? "" : "/")}{foldersAndFiles.Aggregate((a,b)=>a.GetCommonStart(b))}";

            Log("Possible commands:");
            foreach (var possibleFiles in folders)
            {
                Log($"\t{startDir}{(startDir.EndsWith("/") ? "" : "/")}{possibleFiles}");
            }
            foreach (var possibleFiles in files)
            {
                Log($"\t{startDir}{(startDir.EndsWith("/") ? "" : "/")}{possibleFiles}");
            }
            CurrentCommand = CurrentCommand.Substring(0, currentToken.TokenLocation) +
                             commonStart +
                             CurrentCommand.Substring(currentToken.TokenLocation +
                                                      currentToken.TokenValue.Length);
                                    
            CaretPosition = currentToken.TokenLocation + commonStart.Length;
        }
        else
        {
            var file = $"{startDir}{(startDir.EndsWith("/") ? "" : "/")}{foldersAndFiles.First()}";
            
            CurrentCommand = CurrentCommand.Substring(0, currentToken.TokenLocation) +
                             file +
                             CurrentCommand.Substring(currentToken.TokenLocation +
                                                      currentToken.TokenValue.Length);
            CaretPosition = currentToken.TokenLocation + file.Length;
        }
    }

    private void AutoCompleteNode(ConsoleToken currentToken, ref string CurrentCommand, ref int CaretPosition)
    {
        int lastSlashIndex = currentToken.TokenValue.LastIndexOf("/");
        string pathStart = "";
        string pathEnd = "";
        if (lastSlashIndex < 0)
        {
            pathStart = "$";
            pathEnd = currentToken.TokenValue.Substring(1);
        }
        else
        {
            pathStart = currentToken.TokenValue.Substring(0, lastSlashIndex);
            pathEnd = currentToken.TokenValue.Substring(lastSlashIndex + 1);
        }

        bool autoCompleteProperty = false;
        if (currentToken.TokenValue.Contains('.'))
        {
            autoCompleteProperty = true;
            int firstDotIndex = currentToken.TokenValue.IndexOf('.');
            int lastDotIndex = currentToken.TokenValue.LastIndexOf('.');
            pathStart = currentToken.TokenValue.Substring(0, firstDotIndex);
            pathEnd = currentToken.TokenValue.Substring(firstDotIndex + 1);
        }
        
        
        bool autoCompleteNodeCommand = false;
        if (currentToken.TokenValue.Contains(':'))
        {
            autoCompleteNodeCommand = true;
            int firstColonIndex = currentToken.TokenValue.IndexOf(':');
            int lastColonIndex = currentToken.TokenValue.LastIndexOf(':');
            pathStart = currentToken.TokenValue.Substring(0, lastColonIndex);
            pathEnd = currentToken.TokenValue.Substring(lastColonIndex + 1);
        }
                                

        Node node = GetNode($"/root/{pathStart.Substring(1)}");
        
        if (autoCompleteNodeCommand)
        {
            AutoCompleteNodeCommand(currentToken, node, pathStart, pathEnd, ref CurrentCommand, ref CaretPosition);
        }
        else if (autoCompleteProperty)
        {
            AutoCompleteNodeProperty(currentToken, node, pathStart, pathEnd, ref CurrentCommand, ref CaretPosition);
        }
        else
        {
            AutoCompleteNodeName(currentToken, node, pathStart, pathEnd, ref CurrentCommand, ref CaretPosition);
        }
    }

    private void AutoCompleteNodeName(ConsoleToken currentToken, Node node, string pathStart, string pathEnd, ref string CurrentCommand, ref int CaretPosition)
    {
        string newNodePath;
        var children = node.GetChildren().OrderBy(c => c.Name.ToString());
        if (children.Any(c =>
                c.Name.ToString().StartsWith(pathEnd, true, CultureInfo.InvariantCulture)))
        {
            var nodes = children.Where(c =>
                c.Name.ToString().StartsWith(pathEnd, true,
                    CultureInfo.InvariantCulture));

            if (nodes.Count() > 1)
            {
                                            
                Log("Possible commands:");
                foreach (var possibleCommand in nodes)
                {
                    Log($"\t{possibleCommand.Name}");
                }

                if (pathEnd != "")
                {
                    string commonStart = nodes.Select(c => c.Name.ToString()).Aggregate((a,b)=>a.GetCommonStart(b));
                    newNodePath = $"{pathStart}/{commonStart}";
                    CurrentCommand = CurrentCommand.Substring(0, currentToken.TokenLocation) +
                                     newNodePath +
                                     CurrentCommand.Substring(currentToken.TokenLocation +
                                                              currentToken.TokenValue.Length);

                    CaretPosition = currentToken.TokenLocation + newNodePath.Length;
                }
            }
            else
            {
                string nodeName = nodes.First().Name.ToString();
                newNodePath = $"{pathStart}/{nodeName}";

                CurrentCommand = CurrentCommand.Substring(0, currentToken.TokenLocation) +
                                 newNodePath +
                                 CurrentCommand.Substring(currentToken.TokenLocation +
                                                          currentToken.TokenValue.Length);
                CaretPosition = currentToken.TokenLocation + newNodePath.Length;
            }
        }
    }

    private void AutoCompleteNodeProperty(ConsoleToken currentToken, Node node, string pathStart, string pathEnd, ref string CurrentCommand, ref int CaretPosition)
    {
        Type objectType = node.GetType();
        var splitPath = pathEnd.Split('.');
        string startPropertyPath = string.Join('.', splitPath.Take(splitPath.Length-1));
        for (int i = 0; i < splitPath.Length; i++)
        {
            IEnumerable<MemberInfo> allMembers = objectType.GetMembers().Where(m=>m.MemberType is MemberTypes.Property or MemberTypes.Field);
            MemberInfo propertyInfo = allMembers.FirstOrDefault(p=> p.Name.ToLower() == splitPath[i].ToLower());
            if (propertyInfo == null)
            {
                if (i == splitPath.Length - 1)
                {
                    if(allMembers.Any(p=>p.Name.StartsWith(splitPath[i], true, CultureInfo.InvariantCulture)))
                    {
                        var potentialProperties = allMembers.Where(p =>
                            p.Name.StartsWith(splitPath[i], true,
                                CultureInfo.InvariantCulture));
                        if (potentialProperties.Count() > 1)
                        {
                            Log("Possible commands:");
                            foreach (var possibleCommand in potentialProperties)
                            {
                                Log($"\t{possibleCommand.Name}");
                            }

                            if (splitPath[i] != "")
                            {
                                string commonStart = potentialProperties.Select(c => c.Name).Aggregate((a,b)=>a.GetCommonStart(b));

                                string newNodePath = $"{pathStart}{(startPropertyPath != "" ? $".{startPropertyPath}" : "")}.{commonStart}";
                                CurrentCommand = CurrentCommand.Substring(0, currentToken.TokenLocation) +
                                                 newNodePath +
                                                 CurrentCommand.Substring(currentToken.TokenLocation +
                                                                          currentToken.TokenValue.Length);

                                CaretPosition = currentToken.TokenLocation + newNodePath.Length;
                            }
                        }
                        else
                        {
                            var propertyName = potentialProperties.First().Name;
                            
                            string newNodePath = $"{pathStart}{(startPropertyPath != "" ? $".{startPropertyPath}" : "")}.{propertyName}";
                            CurrentCommand = CurrentCommand.Substring(0, currentToken.TokenLocation) +
                                             newNodePath +
                                             CurrentCommand.Substring(currentToken.TokenLocation +
                                                                      currentToken.TokenValue.Length);
                                    
                            CaretPosition = currentToken.TokenLocation + newNodePath.Length;
                                                        
                        }
                    }
                }
                break;
            }
                                        
            objectType = propertyInfo.MemberType == MemberTypes.Field? (propertyInfo as FieldInfo).FieldType : (propertyInfo as PropertyInfo).PropertyType;
        }
    }

    private void AutoCompleteNodeCommand(ConsoleToken currentToken, Node node, string nodePath, string nodeCommand, ref string CurrentCommand, ref int CaretPosition)
    {
        var splitPath = nodePath.Split('.');

        Type objectType = node.GetType();
        object currentObject = node;
        
        for (int i = 1; i < splitPath.Length; i++)
        {
            PropertyInfo propertyInfo = objectType.GetProperty(splitPath[i]);
            if (propertyInfo != null)
            {
                currentObject = propertyInfo.GetValue(currentObject, null);
                objectType = currentObject.GetType();
            }
            else
            {
                LogError($"ERROR: {splitPath[i-1]} does not contain a property named {splitPath[i]}, in: {nodePath}");
                return;
            }
        }
        
        var cmds = _commands.OrderBy(c=>c.Key.commandName).Where(
            c => c.Key.commandName.StartsWith(nodeCommand, true, CultureInfo.InvariantCulture) && (c.Key.type != null && c.Key.type.IsAssignableFrom(objectType) && c.Value.commandAttribute.InstanceCommand));

        if (!cmds.Any())
        {
            return;
        }
        
        if (cmds.Count() > 1)
        {
            string commonStart = cmds.Select(c => c.Key.commandName).Aggregate((a,b)=>a.GetCommonStart(b));

            Log("Possible commands:");
            foreach (var possibleCommand in cmds)
            {
                Log($"\t{possibleCommand.Key.commandName}");
            }
            CurrentCommand = CurrentCommand.Substring(0, currentToken.TokenLocation + nodePath.Length) + ":"+
                             commonStart +
                             CurrentCommand.Substring(currentToken.TokenLocation +
                                                      currentToken.TokenValue.Length);
                                    
            CaretPosition = currentToken.TokenLocation + nodePath.Length + 1 + commonStart.Length;
        }
        else
        {
            var cmd = cmds.First();
            CurrentCommand = CurrentCommand.Substring(0, currentToken.TokenLocation + nodePath.Length) + ":" +
                             cmd.Key.commandName + " " +
                             CurrentCommand.Substring(currentToken.TokenLocation + currentToken.TokenValue.Length);
            CaretPosition = currentToken.TokenLocation + nodePath.Length + 2 + cmd.Key.commandName.Length;
        }
        
    }

    protected object ConvertParameterType(string input, Type type)
    {
        object output = null;
        
        if (type == typeof(byte))
        {
            output = byte.Parse(input);
        }
        else if (type == typeof(short))
        {
            output = short.Parse(input);
        }
        else if (type == typeof(int))
        {
            output = int.Parse(input);
        }
        else if (type == typeof(long))
        {
            output = long.Parse(input);
        }
        else if (type == typeof(ushort))
        {
            output = ushort.Parse(input);
        }
        else if (type == typeof(uint))
        {
            output = uint.Parse(input);
        }
        else if (type == typeof(ulong))
        {
            output = ulong.Parse(input);
        }
        else if (type == typeof(float))
        {
            output = float.Parse(input);
        }
        else if (type == typeof(double))
        {
            output = double.Parse(input);
        }
        else if (type == typeof(DateTime))
        {
            output = DateTime.Parse(input);
        }
        else if (type == typeof(bool))
        {
            output = bool.Parse(input);
        }
        else if (type == typeof(Vector2))
        {
            string[] splitinput = input.Split(new[]{',', ' ', '[', ']'}, StringSplitOptions.RemoveEmptyEntries);
            if (splitinput.Length >= 2)
            {
                return new Vector2(float.Parse(splitinput[0]), float.Parse(splitinput[1]));
            }
        }
        else if (type == typeof(Vector3))
        {
            string[] splitinput = input.Split(new[]{',', ' ', '[', ']'}, StringSplitOptions.RemoveEmptyEntries);
            if (splitinput.Length >= 3)
            {
                return new Vector3(float.Parse(splitinput[0]), float.Parse(splitinput[1]), float.Parse(splitinput[2]));
            }
        }
        else if (type == typeof(Vector4))
        {
            string[] splitinput = input.Split(new[]{',', ' ', '[', ']'}, StringSplitOptions.RemoveEmptyEntries);
            if (splitinput.Length >= 4)
            {
                return new Vector4(float.Parse(splitinput[0]), float.Parse(splitinput[1]), float.Parse(splitinput[2]), float.Parse(splitinput[3]));
            }
        }
        else if (type == typeof(Vector2I))
        {
            string[] splitinput = input.Split(new[]{',', ' ', '[', ']'}, StringSplitOptions.RemoveEmptyEntries);
            if (splitinput.Length >= 2)
            {
                return new Vector2(int.Parse(splitinput[0]), int.Parse(splitinput[1]));
            }
        }
        else if (type == typeof(Vector3I))
        {
            string[] splitinput = input.Split(new[]{',', ' ', '[', ']'}, StringSplitOptions.RemoveEmptyEntries);
            if (splitinput.Length >= 3)
            {
                return new Vector3(int.Parse(splitinput[0]), int.Parse(splitinput[1]), int.Parse(splitinput[2]));
            }
        }
        else if (type == typeof(Vector4I))
        {
            string[] splitinput = input.Split(new[]{',', ' ', '[', ']'}, StringSplitOptions.RemoveEmptyEntries);
            if (splitinput.Length >= 4)
            {
                return new Vector4(int.Parse(splitinput[0]), int.Parse(splitinput[1]), int.Parse(splitinput[2]), int.Parse(splitinput[3]));
            }
        }
        else if (type.IsEnum)
        {
            output = Enum.Parse(type, input);
        }

        if (output != null)
        {
            return output;
        }

        return input;
    }

    protected MemberInfo GetObjectProperty(string propertyPath, out Node node, out object propertyObject)
    {
        
        var nodeCmd = propertyPath.Split(".");
        node = GetNode($"/root/{nodeCmd[0].Substring(1)}");
        propertyObject = node;
        if (node == null)
        {
            LogError($"Can not find node at path {nodeCmd[0]}", "Invalid Node Path");
        }

        if (nodeCmd.Length < 2)
        {
            propertyObject = node;
            return null;
        }
        
        Type nodeType = node.GetType();
        
        IEnumerable<MemberInfo> allMembers = nodeType.GetMembers().Where(m=>m.MemberType is MemberTypes.Property or MemberTypes.Field);
        MemberInfo propertyInfo = allMembers.FirstOrDefault(p=> p.Name.ToLower() == nodeCmd[1].ToLower());
        // PropertyInfo propertyInfo = nodeType.GetProperties().FirstOrDefault(p => p.Name.ToLower() == nodeCmd[1].ToLower());

        if (propertyInfo == null)
        {
            LogError($"Node {node.Name} ({nodeType.Name}) does not have property {nodeCmd[1]}");
        }

        if (nodeCmd.Length > 2)
        {
            PropertyInfo pi = propertyInfo as PropertyInfo;
            FieldInfo fi = propertyInfo as FieldInfo;
            
            Type propertyType = pi?.PropertyType ?? fi.FieldType;
            propertyObject = node;
            for (int i = 2; i < nodeCmd.Length; i++)
            {
                
                propertyObject = pi != null ? pi.GetValue(propertyObject) : fi.GetValue(propertyObject);
                allMembers = propertyType.GetMembers().Where(m=>m.MemberType is MemberTypes.Property or MemberTypes.Field);
                propertyInfo = allMembers.FirstOrDefault(p=> p.Name.ToLower() == nodeCmd[i].ToLower());
                //propertyInfo = propertyType.GetProperties().FirstOrDefault(p => p.Name.ToLower() == nodeCmd[i].ToLower());
                if (propertyInfo == null)
                {
                    LogError($"Node {node.Name} ({nodeType.Name}) does not have property {string.Join('.', nodeCmd.Take(i+1).Skip(1))}");
                }
                propertyType = pi?.PropertyType ?? fi.FieldType;
            }
        }
        
        
        return propertyInfo;
    }


    [ConsoleCommand("Help", Description = "Returns a list of console commands available", SystemCommand = true)]
    [ConsoleCommandParameter("Help", 0, "Command", typeof(string), DefaultValue = null, Optional = true)]
    public void HelpCommand(string command = null)
    {
        if (command != null)
        {
            GetHelp(command, out string help);
            Log($"Getting help for \"{command}\":");
            Log(help);
            return;
        }

        Log("The following commands are available:");
        foreach (var cmd in _commands.OrderBy(item=> item.Key.type == null))
        {
            (Type commandObjectType, string commandName) = cmd.Key;
            if (commandObjectType == null)
            {
                Log($"\t{commandName}");
            }
        }
    }
    public bool GetHelp(string command, out string output)
    {
        output = "";
        Type targetType = null;
        if (command.StartsWith("$"))
        {
            if (command.Contains(":"))
            {
                var nodeCmd = command.Split(new char[] { ':' });
                targetType = GetNode($"/root/{nodeCmd[0].Substring(1)}").GetType();
                command = nodeCmd[1];
            }
        }

        if (!_commands.ContainsKey((targetType, command)))
        {
            return false;
        }

        var commandInfo = _commands[(targetType, command)];
        if (commandInfo.commandAttribute.InstanceCommand && targetType == null)
        {
            
        }

        var parameters = commandInfo.parameterAttributes;
        output = $"Expected usage: {(targetType != null ? $"$/root/PathTo/{targetType.Name}:" : "")}{command} {string.Join(' ',parameters.Where(p=>p.Order >= 0).OrderBy(p=>p.Order).Select(p=> p.Optional ? $"[{p.ParameterName}={p.DefaultValue}] " : $"{{{p.ParameterName}}}"))}";
        return true;
    }
    protected object ProcessCommandCollection(ConsoleToken token,
        out (string errorReason, string errorMessage) error, object targetObject = null, Type targetObjectType = null,
        Dictionary<string, ConsoleToken> bindingSubstutions = null)
    {
        if (token.SubTokens.Count < 1)
        {
            error.errorReason = "No command provided";
            error.errorMessage = "You did not enter a command!";

            return null;
        }
        return ProcessToken(token.SubTokens[0], token.SubTokens.Count > 1 ? token.SubTokens.Skip(1).ToArray() : null, out error, targetObject, targetObjectType);
    }

    protected object ProcessBindingCollection(ConsoleToken bindingCollectionToken, IEnumerable<ConsoleToken> parameterTokens,
        out (string errorReason, string errorMessage) error, object targetObject = null, Type targetObjectType = null,
        Dictionary<string, ConsoleToken> bindingSubstutions = null)
    {
        ConsoleToken bindingToken = bindingCollectionToken.SubTokens[0];
        /*
        Dictionary<string, ConsoleToken> processedParameters = bindingCollectionToken.SubTokens?.Count() >= 3 ? 
                bindingCollectionToken.SubTokens.Skip(1).Select(((token, i) => new {token, i})).GroupBy(t=>t.i / 2)
                    .ToDictionary(g=>g.First().token.TokenValue, g=> g.Last().token) :
                new Dictionary<string, ConsoleToken>();

        if (bindingSubstutions != null)
        {
            foreach ((string key, ConsoleToken value) in bindingSubstutions)
            {
                if (!processedParameters.ContainsKey(key))
                {
                    processedParameters[key] = value;
                }
            }
        }
*/
        return ProcessBinding(bindingToken, bindingCollectionToken.SubTokens.Skip(1), out error, targetObject, targetObjectType, bindingSubstutions);
    }
    protected object ProcessBinding(ConsoleToken bindingToken, IEnumerable<ConsoleToken> parameterTokens,
        out (string errorReason, string errorMessage) error, object targetObject = null, Type targetObjectType = null,
        Dictionary<string, ConsoleToken> bindingSubstutions = null)
    {
        string bindingName = bindingToken.TokenValue[0] == '@' ? bindingToken.TokenValue.Substring(1) :  bindingToken.TokenValue;
        if (!_bindings.ContainsKey(bindingName))
        {
            error.errorReason = "Binding does not exist";
            error.errorMessage = "You have tried to call a binding which does not exist. Type 'help bindings' to list out the available bindings";
            return null;
        }
        
        var binding = _bindings[bindingName];
        
        Dictionary<string, ConsoleToken> processedParameters = parameterTokens?.Count() >= 2 ? parameterTokens.Select(((token, i) => new {token, i})).GroupBy(t=>t.i / 2).ToDictionary(g=>
        {
            string key = g.First().token.TokenValue;
            return key[0] == '&' ? key : $"&{key}";
        }, g=> g.Last().token) : new Dictionary<string, ConsoleToken>();
        
        if (bindingSubstutions != null)
        {
            foreach ((string key, ConsoleToken value) in bindingSubstutions)
            {
                if (!processedParameters.ContainsKey(key))
                {
                    processedParameters[key] = value;
                }
            }
        }
        
        return ProcessToken(binding.Tokens[0], binding.Tokens.Length > 1 ? binding.Tokens.Skip(1).ToArray() : null, out error, targetObject, targetObjectType, null, processedParameters);
    }
    protected object ProcessCommand(ConsoleToken commandToken, IEnumerable<ConsoleToken> parameterTokens,
        out (string errorReason, string errorMessage) error, object targetObject = null, Type targetObjectType = null,
        Dictionary<string, ConsoleToken> bindingSubstutions = null)
    {
        error.errorReason = "";
        error.errorMessage = "";
        string commandName = commandToken.TokenType == ConsoleTokenType.NodeCommand ? commandToken.TokenValue.Split(':').LastOrDefault() :  commandToken.TokenValue;

        if (!_commands.Any(c =>
                (c.Key.type == targetObjectType || c.Key.type.IsAssignableFrom(targetObjectType)) &&
                c.Key.commandName == commandName))
        {
            error.errorReason = "Invalid Command";
            error.errorMessage = $"The command you entered is invalid: {commandToken.TokenValue} {(parameterTokens != null ? string.Join(' ', parameterTokens.Select(t=>t.TokenValue)) : "")}";
            return null;
        }
        
        (MethodInfo method, ConsoleCommandAttribute commandAttribute, IEnumerable<ConsoleCommandParameterAttribute> parameterAttributes) cmd;
            
        cmd = _commands.FirstOrDefault(c =>
            (c.Key.type == targetObjectType || c.Key.type.IsAssignableFrom(targetObjectType)) && c.Key.commandName == commandName).Value;

        
        int tokenCount = parameterTokens?.Count() ?? 0;
        int parameterCount = cmd.parameterAttributes.Count(p=> /*p.Usage == ConsoleCommandParameterAttribute.ConsoleCommandParameterUsage.Default*/ p.Order >= 0);
        int requiredParameterCount = cmd.parameterAttributes.Count(p => !p.Optional && /*p.Usage == ConsoleCommandParameterAttribute.ConsoleCommandParameterUsage.Default*/ p.Order >= 0);

        if (tokenCount < requiredParameterCount || tokenCount > parameterCount)
        {
            error.errorReason = "Invalid Parameter Count";
            error.errorMessage =
                $"You wrote the wrong number of arguments! Expected [color=red]{requiredParameterCount}[/color]{(requiredParameterCount != parameterCount ? $" (or up to [color=red]{parameterCount}[/color])" : "")} but got [color=red]{tokenCount}[/color]!\n";
            error.errorMessage += $"Expected usage: {commandName} {string.Join(' ',cmd.parameterAttributes.Where(p=>p.Order >= 0).OrderBy(p=>p.Order).Select(p=> p.Optional ? $"[{p.ParameterName}={p.DefaultValue}] " : $"{{{p.ParameterName}}}"))}";
            return null;
        }
        object[] parameters = new object[cmd.parameterAttributes.Count()];
        int paramIndex = 0;
        foreach (var parameter in cmd.parameterAttributes)
        {
            bool useDefaultValue = parameter.Order == -1 || parameter.Order >= tokenCount;
            if (!useDefaultValue)
            {
                var paramObject =  ProcessToken(parameterTokens.ElementAt(parameter.Order),
                    out var tokenErrorDetails, null,null, parameter.ParameterType);
                if (parameter.ParameterType != null && !typeof(Node).IsInstanceOfType(paramObject))
                {
                    if (parameter.ParameterType == typeof(string))
                    {
                        paramObject = paramObject.ToString();
                    }
                    else
                    {
                        paramObject = ConvertParameterType(paramObject.ToString(), parameter.ParameterType);
                    }
                }
                else if ((parameter.Usage == ConsoleCommandParameterAttribute.ConsoleCommandParameterUsage.Node && paramObject is not Node) ||
                         (parameter.Usage == ConsoleCommandParameterAttribute.ConsoleCommandParameterUsage.Default && parameter.ParameterType != null && paramObject.GetType().IsAssignableFrom(parameter.ParameterType)))
                {
                    error.errorReason = "Invalid Parameter Type!";
                    error.errorMessage =
                        $"The parameter you provided is! Expected [color=red]{requiredParameterCount}[/color]{(requiredParameterCount != parameterCount ? $" (or up to [color=red]{parameterCount}[/color])" : "")} but got [color=red]{tokenCount}[/color]!\n";
                    error.errorMessage += $"Expected usage: {commandName} {string.Join(' ',cmd.parameterAttributes.Where(p=>p.Order >= 0).OrderBy(p=>p.Order).Select(p=> p.Optional ? $"[{p.ParameterName}={p.DefaultValue}] " : $"{{{p.ParameterName}}}"))}";
                    return null;
                }

                parameters[paramIndex] = paramObject;
            }
            else
            {
                switch (parameter.Usage)
                {
                    case ConsoleCommandParameterAttribute.ConsoleCommandParameterUsage.Default:
                        parameters[paramIndex] = parameter.DefaultValue;
                        break;
                    case ConsoleCommandParameterAttribute.ConsoleCommandParameterUsage.Node:
                        var node = GetNode(parameter.DefaultValue as string);
                        parameters[paramIndex] = parameter.ParameterType != null ? Convert.ChangeType(node, parameter.ParameterType) : node;
                        break;
                    case ConsoleCommandParameterAttribute.ConsoleCommandParameterUsage.Special:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            paramIndex++;
        }

        var output = cmd.method.Invoke(cmd.commandAttribute.SystemCommand ? this : targetObject, parameters);

        return output;
    }

    protected T ProcessToken<T>(ConsoleToken token, out (string errorReason, string errorMessage) error,
        object targetObject = null, Type targetType = null) => (T)ProcessToken(token, out error, targetObject, targetType, typeof(T));
    
    protected T ProcessToken<T>(ConsoleToken token, ConsoleToken[] argumentTokens, out (string errorReason, string errorMessage) error,
        object targetObject = null, Type targetType = null) => (T)ProcessToken(token, null, out error, targetObject, targetType, typeof(T));

    protected object ProcessToken(ConsoleToken token, out (string errorReason, string errorMessage) error,
        object targetObject = null, Type targetType = null, Type returnType = null, Dictionary<string, ConsoleToken> bindingSubstutions = null)
        => ProcessToken(token, null, out error, targetObject, targetType, returnType, bindingSubstutions);
    protected object ProcessToken(ConsoleToken token, ConsoleToken[] argumentTokens, out (string errorReason, string errorMessage) error, object targetObject = null, Type targetType = null, Type returnType = null, Dictionary<string, ConsoleToken> bindingSubstutions = null)
    {
        error = ("", "");
        switch (token.TokenType)
        {
            case ConsoleTokenType.Value:
                return returnType != null ? token.GetValue(returnType) : token.TokenValue;
                
            case ConsoleTokenType.Command:
                if (token.TokenValue.ToLower() == "bind" && argumentTokens?.Length >= 2)
                {
                    return CreateBind(argumentTokens[0], argumentTokens.Skip(1).ToArray(), out error, bindingSubstutions);
                }
                
                if (token.TokenValue.ToLower() == "bind"  && (argumentTokens == null || argumentTokens.Length < 2))
                {
                    error.errorReason = "Can not perform bind!";
                    error.errorMessage = $"Binding requires 2 arguments, only {argumentTokens?.Length ?? 0} arguments were provided.\n Expected usage: bind [binding-name] [command|property|value]";
                    return null;
                }
                
                return ProcessCommand(token, argumentTokens, out error, targetObject, targetType, bindingSubstutions);
                
            case ConsoleTokenType.BindingParameter:
                return bindingSubstutions != null && bindingSubstutions.TryGetValue(token.TokenValue, out ConsoleToken bindingValue) ? ProcessToken(bindingValue, out error, targetObject, targetType, returnType ,bindingSubstutions) : token.TokenValue;
                
            case ConsoleTokenType.Binding:
                return ProcessBinding(token, argumentTokens, out error, targetObject, targetType, bindingSubstutions);
            case ConsoleTokenType.BindingCollection:
                return ProcessBindingCollection(token, argumentTokens, out error, targetObject, targetType, bindingSubstutions);
            case ConsoleTokenType.Object:
                if (returnType != null)
                {
                    object typedObject = Activator.CreateInstance(returnType);
                    PropertyInfo[] propertyInfos = returnType.GetProperties();
                    for (int i = 0; i + 1 < token.SubTokens.Count; i += 2)
                    {
                        string key = ProcessToken(token.SubTokens[i], null, out error, null, null, typeof(string), bindingSubstutions) as string;
                        PropertyInfo propertyInfo = propertyInfos.FirstOrDefault(p => p.Name.ToLower() == key.ToLower());
                        if (propertyInfo != null)
                        {
                            object value = ProcessToken(token.SubTokens[i + 1], null, out error, null, null,
                                propertyInfo.PropertyType, bindingSubstutions);
                            propertyInfo.SetValue(typedObject, value);
                        }
                    }

                    return typedObject;
                }

                dynamic expandoObject = new ExpandoObject();
                var expandoDict = expandoObject as IDictionary;
                
                for (int i = 0; i + 1 < token.SubTokens.Count; i += 2)
                {
                    string key = ProcessToken(token.SubTokens[i], null, out error, null, null, typeof(string), bindingSubstutions) as string;
                    object value = ProcessToken(token.SubTokens[i + 1], null, out error, null, null, null, bindingSubstutions);
                    expandoDict[key] = value;
                }

                return expandoObject;
            case ConsoleTokenType.Array:
                // Because of reasons, it's useful to use arrays for vector definitions
                if (returnType != null && !returnType.IsArray)
                {
                    return token.GetValue(returnType);
                }
                
                object[] output = new object[token.SubTokens.Count];
                Type elementType = returnType.GetElementType();
                for (int i = 0; i < token.SubTokens.Count; i++)
                {
                    output[i] = ProcessToken(token, null, out error, null, null, elementType, bindingSubstutions);
                }

                return output;
                
            
            case ConsoleTokenType.PropertyName:
                break;
            
            case ConsoleTokenType.PropertyValue:
                break;
            
            case ConsoleTokenType.Node:
                Node node = GetNode($"/root/{token.TokenValue.Substring(1)}");
                return returnType != null && returnType.IsInstanceOfType(node) ? Convert.ChangeType(node, returnType) : node;

            case ConsoleTokenType.NodeProperty:
            {
                MemberInfo objectProperty =
                    GetObjectProperty(token.TokenValue, out Node propertyNode, out object propertyObject);
                PropertyInfo pi = objectProperty as PropertyInfo;
                FieldInfo fi = objectProperty as FieldInfo;

                if (objectProperty != null && argumentTokens != null && argumentTokens.Length > 0)
                {
                    object newValue = ProcessToken(argumentTokens[0], out error, null, null,
                        pi?.PropertyType ?? fi.FieldType, bindingSubstutions);

                    if (pi != null)
                    {
                        pi.SetValue(propertyObject, newValue);
                    }
                    else
                    {
                        fi.SetValue(propertyObject, newValue);
                    }

                    return newValue;
                }

                if (objectProperty != null)
                {
                    object propertyValue = pi != null ? pi.GetValue(propertyObject) : fi.GetValue(propertyObject);
                    return returnType != null && returnType.IsInstanceOfType(propertyValue)
                        ? Convert.ChangeType(propertyValue, targetType)
                        : propertyValue;
                }

                break;
            }

            case ConsoleTokenType.NodeCommand:
            {
                MemberInfo objectProperty =
                    GetObjectProperty(token.TokenValue, out Node propertyNode, out object propertyObject);
                return ProcessCommand(token, argumentTokens, out error, propertyObject, propertyObject.GetType(), bindingSubstutions);
            }

            case ConsoleTokenType.CommandCollection:
                return ProcessCommandCollection(token, out error, targetObject, targetType, bindingSubstutions);
                
            default:
                throw new ArgumentOutOfRangeException();
        }

        return null;
    }

    public IEnumerable<string> GetBindingParameters(ConsoleToken token)
    {
        List<string> parameters = new List<string>();
        if (token.TokenType == ConsoleTokenType.BindingParameter)
        {
            parameters.Add(token.TokenValue.ToLower());
        }

        if (token.SubTokens != null && token.SubTokens.Count > 0)
        {
            parameters.AddRange(GetBindingParameters(token.SubTokens[0]));
        }
        
        return parameters;
    }
    private object CreateBind(ConsoleToken token, ConsoleToken[] argumentTokens,
        out (string errorReason, string errorMessage) error, Dictionary<string, ConsoleToken> bindingSubstutions = null)
    {
        error.errorReason = "";
        error.errorMessage = "";

        ConsoleBinding binding = new ConsoleBinding()
        {
            Name = token.TokenValue,
            Tokens = argumentTokens,
            BindingParameters = GetBindingParameters(token).ToArray()
        };
        
        _bindings[token.TokenValue] = binding;
        
        return null;
    }
    public void RunCommand(string command)
    {
        Log(command);
        OnAddHistory?.Invoke(command);
        
        ConsoleToken commandToken = ConsoleTokenizer.Tokenize(command); //command.Split(" ", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var result = ProcessToken(commandToken, out var error);

        if (!string.IsNullOrWhiteSpace(error.errorReason))
        {
            LogError(error.errorMessage, error.errorReason);
        }

        if (result != null)
        {
            Log(result.ToString());
        }
        
    }
}