using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Godot;

namespace DevilboxGames.DebugConsole;

public enum ConsoleTokenType
{
    Value,
    Command,
    Object,
    Array,
    PropertyName,
    PropertyValue,
    Node,
    NodeProperty,
    NodeCommand,
    CommandCollection,
    Binding,
    BindingCollection,
    BindingParameter
}
public class ConsoleToken
{
    public List<ConsoleToken> SubTokens { get; set; }
    public string TokenValue { get; set; }
    public ConsoleTokenType TokenType { get; set; }
    public int TokenLocation { get; set; }
    
    public bool IsValueToken => TokenType != ConsoleTokenType.Array && TokenType != ConsoleTokenType.NodeCommand && TokenType != ConsoleTokenType.CommandCollection;
    public T GetValue<T>() => (T)GetValue(typeof(T));

    public void Reset()
    {
        TokenType = ConsoleTokenType.Value;
        SubTokens.Clear();
        TokenValue = "";
    }
    public object GetValue(Type type)
    {
        object output = null;

        if (type.IsArray)
        {
            return SubTokens.Select(t => Convert.ChangeType(t.GetValue(type.GetElementType()), type.GetElementType())).ToArray();
        }
        
        if (TokenType == ConsoleTokenType.Object)
        {
            output = Activator.CreateInstance(type);
            PropertyInfo[] properties = type.GetProperties();
            for (int i = 0; i < SubTokens.Count; i+=2)
            {
                string propertyName = SubTokens[i].TokenValue;
                if (properties.Any(p => p.Name == propertyName))
                {
                    var property = properties.First(p => p.Name == propertyName);
                    property.SetValue(output, SubTokens[i+1].GetValue(property.PropertyType));
                }
            }

            return output;
        }
        
        if (type == typeof(byte))
        {
            output = byte.Parse(TokenValue);
        }
        else if (type == typeof(short))
        {
            output = short.Parse(TokenValue);
        }
        else if (type == typeof(int))
        {
            output = int.Parse(TokenValue);
        }
        else if (type == typeof(long))
        {
            output = long.Parse(TokenValue);
        }
        else if (type == typeof(ushort))
        {
            output = ushort.Parse(TokenValue);
        }
        else if (type == typeof(uint))
        {
            output = uint.Parse(TokenValue);
        }
        else if (type == typeof(ulong))
        {
            output = ulong.Parse(TokenValue);
        }
        else if (type == typeof(float))
        {
            output = float.Parse(TokenValue);
        }
        else if (type == typeof(double))
        {
            output = double.Parse(TokenValue);
        }
        else if (type == typeof(DateTime))
        {
            output = DateTime.Parse(TokenValue);
        }
        else if (type == typeof(bool))
        {
            output = bool.Parse(TokenValue);
        }
        else if (type == typeof(Vector2))
        {
            List<float> values = SubTokens.Select(t => t.GetValue<float>()).ToList();
            if (values.Count >= 2)
            {
                return new Vector2(values[0], values[1]);
            }
        }
        else if (type == typeof(Vector3))
        {
            List<float> values = SubTokens.Select(t => t.GetValue<float>()).ToList();
            if (values.Count >= 3)
            {
                return new Vector3(values[0], values[1], values[2]);
            }
        }
        else if (type == typeof(Vector4))
        {
            List<float> values = SubTokens.Select(t => t.GetValue<float>()).ToList();
            if (values.Count >= 4)
            {
                return new Vector4(values[0], values[1], values[2], values[3]);
            }
        }
        else if (type == typeof(Vector2I))
        {
            List<int> values = SubTokens.Select(t => t.GetValue<int>()).ToList();
            if (values.Count >= 2)
            {
                return new Vector2I(values[0], values[1]);
            }
        }
        else if (type == typeof(Vector3I))
        {
            List<int> values = SubTokens.Select(t => t.GetValue<int>()).ToList();
            if (values.Count >= 3)
            {
                return new Vector3I(values[0], values[1], values[2]);
            }
        }
        else if (type == typeof(Vector4I))
        {
            List<int> values = SubTokens.Select(t => t.GetValue<int>()).ToList();
            if (values.Count >= 4)
            {
                return new Vector4I(values[0], values[1], values[2], values[3]);
            }
        }
        else if (type.IsEnum)
        {
            output = Enum.Parse(type, TokenValue);
        }

        if (output != null)
        {
            return output;
        }

        return TokenValue;
    }
}
