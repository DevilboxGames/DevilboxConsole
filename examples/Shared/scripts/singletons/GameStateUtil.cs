using System.Collections.Generic;

namespace DevilboxConsole.examples.Shared.scripts.singletons;

public static class GameStateUtil
{
    public enum PlayerInputStateMode
    {
        Controller,
        Console,
        Dialogue,
        DebugConsole
    }
    public static PlayerInputStateMode PlayerInputState { get=> _playerInputStateStack.Peek(); set=>_playerInputStateStack.Push(value); }

    public static void PopPlayerInputState()
    {
        if(_playerInputStateStack.Count > 1) _playerInputStateStack.Pop(); 
        
    }
    private static Stack<PlayerInputStateMode> _playerInputStateStack = new(new[] { PlayerInputStateMode.Controller });

}