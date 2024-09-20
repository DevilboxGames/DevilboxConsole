using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Godot;

namespace DevilboxGames.DebugConsole;

public partial class ConsoleControl : Control
{

    [Export]
    public string ConsoleName
    {
        get => _consoleName;
        set
        {
            _consoleName = value;
            if (_commandProcessor != null)
            {
                _commandProcessor.ConsoleName = _consoleName;
            }
        }
    }

    [Export]
    public bool LimitCommands { get; set; }

    [Export]
    public virtual string DefaultOutput { get; set; } =
        "Welcome to Devilbox Command Console [color=red]v{{DebugConsole.Version}}[/color]";

    public static Version ConsoleVersion = new Version(0, 1);
    public static Dictionary<string, ConsoleControl> Instances { get; protected set; } =
        new Dictionary<string, ConsoleControl>();
    public bool IsFocused { get; protected set; }

    public List<string> History { get; set; } = new();
    public int CaretPosition { 
        get => InputLabel.CaretPosition;
        set => InputLabel.CaretPosition = Mathf.Min(InputLabel.Text.Length, Mathf.Max(value, 0));
    }
    public string CurrentCommand
    {
        get => _currentCommand;
        set
        {
            _currentCommand = value;
            if (InputLabel != null)
            {
                InputLabel.Text = value;
            }
        }
    }

    protected Dictionary<(Type type, string commandName), (MethodInfo method, ConsoleCommandAttribute commandAttribute,
        IEnumerable<ConsoleCommandParameterAttribute> parameterAttributes)> _commands;


    protected CommandProcessor _commandProcessor;
    private ConsoleToken _commandToken;
    protected bool ScrollToBottomNextFrame = false;
    protected ConsoleInput InputLabel;
    protected RichTextLabel OutputLabel;
    protected ScrollContainer HistoryScroll;
    protected string _currentCommand = "";
    protected int HistoryIndex = 0;
    private string _consoleName;

    public ConsoleControl(){}
    public ConsoleControl(string name) : this()
    {
        ConsoleName = name;
    }
    public override void _Ready()
    {
        
        InputLabel = GetNode("MarginContainer/Wrapper/HBoxContainer/ConsoleInput") as ConsoleInput;
        OutputLabel = GetNode("MarginContainer/Wrapper/HistoryScroll/HistoryContainer/ConsoleOutput") as RichTextLabel;
        HistoryScroll = GetNode("MarginContainer/Wrapper/HistoryScroll") as ScrollContainer;
        
        _commandProcessor = GetNode<CommandProcessor>("Processor");
        _commandProcessor.OnLog += Log;
        _commandProcessor.OnAddHistory += s =>
        {
            History.Add(s);
            HistoryIndex = History.Count;
        };
        
        if (OutputLabel != null)
        {
            string baseText =  DefaultOutput.Replace("{{DebugConsole.Version}}", ConsoleVersion.ToString());
            
            OutputLabel.Text = "";
            OutputLabel.AppendText(baseText);
            if (string.IsNullOrWhiteSpace(ConsoleName))
            {
                ConsoleName = Guid.NewGuid().ToString();
            }
        }
        
        Instances[ConsoleName] = this;
        base._Ready();
    }

    public override void _Process(double delta)
    {
        if (ScrollToBottomNextFrame)
        {
            
            HistoryScroll.ScrollVertical = (int)HistoryScroll.GetVScrollBar().MaxValue;
            ScrollToBottomNextFrame = false;
        }
    }

    protected virtual void OnGainedFocus() => GD.PrintRich($"[color=green]DEBUG CONSOLE  \"{ConsoleName}\" HAS FOCUS![/color]"); 
    protected virtual void OnLostFocus() => GD.PrintRich($"[color=red]DEBUG CONSOLE  \"{ConsoleName}\" HAS LOST FOCUS![/color]"); 
    public override void _EnterTree()
    {
        FocusEntered += OnGainedFocus;
        FocusExited += OnLostFocus;
    }
    public override void _ExitTree()
    {
        FocusEntered -= OnGainedFocus;
        FocusExited -= OnLostFocus;
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventKey keyEvent && keyEvent.Keycode == Key.Tab)
        {
            HandleInput(@event);
        }
    }

    public override void _UnhandledKeyInput(InputEvent @event)
    {
        HandleInput(@event);
    }
    public virtual void HandleInput(InputEvent @event)
    {
        if (@event is InputEventKey keyEvent)
        {
            HandleKeyInput(keyEvent);
        }
    }

    public virtual void HandleKeyInput(InputEventKey keyEvent)
    {
        if (keyEvent.Pressed)
        {
            if (IsFocused)
            {
                GD.Print("DebugConsole: Typing in console");
                if (keyEvent.Keycode is Key.Enter or Key.KpEnter )
                {
                    string command = CurrentCommand;
                    CurrentCommand = "";
                    InputLabel.Text = "";
                    CaretPosition = 0;
                    RunCommand(command);
                    HistoryIndex = History.Count;

                }
                else if (keyEvent.Keycode == Key.Tab)
                {
                   CurrentCommand = _commandProcessor.AutoComplete( _currentCommand, ref HistoryIndex);
                }
                else if(keyEvent.Keycode == Key.Delete && CurrentCommand.Length > 0)
                {
                    if (CurrentCommand.Length > 0 && CaretPosition < CurrentCommand.Length)
                    {
                        CurrentCommand = CurrentCommand.Substring(0, CaretPosition) +
                                         CurrentCommand.Substring(CaretPosition + 1);
                        InputLabel.Text = CurrentCommand;
                    }
                }
                else if(keyEvent.Keycode == Key.Backspace && CurrentCommand.Length > 0)
                {
                    if (CurrentCommand.Length > 0 && CaretPosition > 0)
                    {
                        CurrentCommand = CurrentCommand.Substring(0, CaretPosition - 1) +
                                         CurrentCommand.Substring(CaretPosition);
                        InputLabel.Text = CurrentCommand;
                        CaretPosition--;
                    }
                }
                else if (keyEvent.Keycode == Key.Up)
                {
                    if (HistoryIndex > 0)
                    {
                        HistoryIndex--;

                        if (History.Count > HistoryIndex)
                        {
                            CurrentCommand = History[HistoryIndex];
                            InputLabel.Text = CurrentCommand;
                            CaretPosition = CurrentCommand.Length;
                        }
                    }
                }
                else if (keyEvent.Keycode == Key.Down)
                {
                    HistoryIndex++;
                    if (HistoryIndex < History.Count)
                    {
                        CurrentCommand = History[HistoryIndex];
                        InputLabel.Text = CurrentCommand;
                        CaretPosition = CurrentCommand.Length;
                    }
                    else
                    {
                        HistoryIndex = History.Count;
                        InputLabel.Text = CurrentCommand;
                        CurrentCommand = "";
                        InputLabel.Text = "";
                        CaretPosition = 0;
                    }
                }
                else if (keyEvent.Keycode == Key.Left)
                {
                    CaretPosition--;
                }
                else if (keyEvent.Keycode == Key.Right)
                {
                    CaretPosition++;
                }
                else
                {
                    if ((keyEvent.Keycode & Key.Special) == 0 || keyEvent.Keycode == Key.Unknown)
                    {
                        string keyValue = "";
                        switch (keyEvent.Keycode)
                        {
                            case Key.Space:
                                keyValue = " ";
                                break;
                            default:
                                var bytes = BitConverter.GetBytes(keyEvent.Unicode).TakeWhile(b=>b>0).ToArray();
                                
                                keyValue = Encoding.ASCII.GetString(bytes);
                                
                                break;
                        }

                        if (CaretPosition < CurrentCommand?.Length)
                        {
                            CurrentCommand = CurrentCommand.Insert(CaretPosition, keyValue);
                        }
                        else
                        {
                            CurrentCommand += keyValue;
                        }

                        CaretPosition++;
                        InputLabel.Text = CurrentCommand;
                        
                        _commandToken = ConsoleTokenizer.Tokenize(CurrentCommand); 
                    }
                }
            }
        }
    }

    
    public void LogWarning(string text, string prefix = "") => Log(text, LogLevel.Warning, prefix);
    public void LogError(string text, string prefix = "") => Log(text, LogLevel.Error, prefix);
    public void Log(string text, LogLevel logLevel = LogLevel.Information, string prefix="")
    {
        switch (logLevel)
        {
            case LogLevel.Warning:
                OutputLabel.AppendText($"\n[color=yellow][b]WARNING: {prefix}[/b][/color] {text}");
                break;
            case LogLevel.Error:
                OutputLabel.AppendText($"\n[color=red][b]ERROR: {prefix}[/b][/color] {text}");
                break;
            default:
                OutputLabel.AppendText($"\n{text}");
                break;
        }

        ScrollToBottomNextFrame = true;
    }

    
    public void RunCommand(string command)
    {
        GD.Print($"ConsoleControl: Running command: {command}");
        //Log(command);
        //History.Add(command);
        
        _commandProcessor.RunCommand(command);
        HistoryIndex = History.Count;
    }
}