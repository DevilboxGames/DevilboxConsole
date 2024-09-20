using System;
using System.Collections.Generic;

namespace DevilboxGames.DebugConsole;

public static class ConsoleTokenizer
{
    protected enum TokenizerState
    {
        ReadToken,
        InQuote
    }
    
    private static bool HandleQuotes(TokenizerState state, ConsoleToken token, out ConsoleToken  newToken, Stack<ConsoleToken> tokenStack)
    {
        newToken = token;
        if (state == TokenizerState.InQuote)
        {
            return false;
        }

        if (token != null)
        {
            StoreCurrentToken(token, out newToken, tokenStack);
        }

        return true;
    }

    private static bool StoreCurrentToken(ConsoleToken token, out ConsoleToken newToken, Stack<ConsoleToken> tokenStack)
    {
        
        if (token == null || (token.IsValueToken && string.IsNullOrWhiteSpace(token.TokenValue) || !token.IsValueToken && token.SubTokens.Count < 1))
        {
            newToken = token;
            return false;
        }

        
        ConsoleTokenType newTokenType = ConsoleTokenType.Value;

        if (tokenStack.Peek().TokenType == ConsoleTokenType.Object)
        {
            newTokenType = token.TokenType == ConsoleTokenType.PropertyName
                ? ConsoleTokenType.PropertyValue
                : ConsoleTokenType.PropertyName;
        }
        newToken = new ConsoleToken()
        {
            TokenType = newTokenType, 
            TokenValue = "",
            SubTokens = new List<ConsoleToken>()
        };
        
        tokenStack.Peek().SubTokens.Add(token);
        return true;
    }

    private static void OpenWrapperToken(ConsoleToken token, ConsoleTokenType tokenType, out ConsoleToken  newToken, Stack<ConsoleToken> tokenStack)
    {
        // Edge Case: If there's an existing which is being read we need to store it,
        // this should only ever happen if there's no space between the previous token and the open wrapper token
        // but ideally it would never happen ¯\_(ツ)_/¯
        if (token != null)
        {
            StoreCurrentToken(token, out token, tokenStack);
        }
        
        // Now token is an empty token we can set it up as the sub token type
        token.TokenType = tokenType;
        tokenStack.Push(token);

        ConsoleTokenType newTokenType = ConsoleTokenType.Value;

        switch (tokenType)
        {
            case ConsoleTokenType.CommandCollection:
                newTokenType = ConsoleTokenType.Command;
                break;
            case ConsoleTokenType.Object:
                newTokenType = ConsoleTokenType.PropertyName;
                break;
        }
        
        newToken = new ConsoleToken()
        {
            TokenType = newTokenType, 
            TokenValue = "",
            SubTokens = new List<ConsoleToken>()
        };
        
    }

    private static void CloseWrapperToken(ConsoleToken token, out ConsoleToken newToken, Stack<ConsoleToken> tokenStack)
    {
        ConsoleToken commandToken = tokenStack.Pop();

        if (token != null && (token.IsValueToken && !string.IsNullOrWhiteSpace(token.TokenValue) || !token.IsValueToken && token.SubTokens.Count > 0))
        {
            commandToken.SubTokens.Add(token);
        }

        tokenStack.Peek().SubTokens.Add(commandToken);
        
        ConsoleTokenType newTokenType = ConsoleTokenType.Value;

        if (tokenStack.Peek().TokenType == ConsoleTokenType.Object)
        {
            newTokenType = token.TokenType == ConsoleTokenType.PropertyName
                ? ConsoleTokenType.PropertyValue
                : ConsoleTokenType.PropertyName;
        }
        
        newToken = new ConsoleToken()
        {
            TokenType = newTokenType, 
            TokenValue = "",
            SubTokens = new List<ConsoleToken>()
        };
        
    }

    private static void AddValueToToken(ConsoleToken token, char value, ref string input, ref int pos, Stack<ConsoleToken> tokenStack)
    {
        AddValueToTokenParent(token, value, tokenStack);
        if (value == '\\')
        {
            pos++;
            value = input[pos];
            AddValueToTokenParent(token, value, tokenStack);
        }

        token.TokenValue += value;
    }

    private static void AddValueToTokenParent(ConsoleToken token, char value, Stack<ConsoleToken> tokenStack)
    {
        foreach (var parentToken in tokenStack)
        {
            parentToken.TokenValue += value;
        }
    }

    public static ConsoleToken Tokenize(string input)
    {
        return Tokenize(input, new Stack<ConsoleToken>());
    }
    public static ConsoleToken Tokenize(string input, Stack<ConsoleToken> tokenStack)
    {
        TokenizerState state = TokenizerState.ReadToken;
        tokenStack.Push(new ConsoleToken
        {
            TokenType = ConsoleTokenType.CommandCollection,
            SubTokens = new List<ConsoleToken>(),
            TokenValue = ""
        });
        
        ConsoleToken token = new ConsoleToken
        {
            TokenType = ConsoleTokenType.Command,
            SubTokens = new List<ConsoleToken>(),
            TokenValue = ""
        };
        
        char tokenChar = '\0';
        int pos = 0;

        while (pos < input.Length)
        {
            tokenChar = input[pos];

            switch (tokenChar)
            {
                case '"':
                    AddValueToTokenParent(token, tokenChar, tokenStack);
                    if (HandleQuotes(state, token, out token, tokenStack))
                    {
                        state = TokenizerState.InQuote;
                        token.TokenLocation = pos + 1;

                    }
                    else
                    {
                        state = TokenizerState.ReadToken;
                    }
                    break;
                
                case '(':
                    if (state != TokenizerState.InQuote)
                    {
                        OpenWrapperToken(token, ConsoleTokenType.CommandCollection, out token, tokenStack);
                        AddValueToTokenParent(token, tokenChar, tokenStack);
                        token.TokenLocation = pos + 1;
                    }
                    else
                    {
                        AddValueToToken(token, tokenChar, ref input, ref pos, tokenStack);
                    }
                    break;
                
                case '{':
                    if (state != TokenizerState.InQuote)
                    {
                        OpenWrapperToken(token, ConsoleTokenType.Object, out token, tokenStack);
                        AddValueToTokenParent(token, tokenChar, tokenStack);
                        token.TokenLocation = pos + 1;
                    }
                    else
                    {
                        AddValueToToken(token, tokenChar, ref input, ref pos, tokenStack);
                    }
                    break;
                
                case '[':
                    if (state != TokenizerState.InQuote)
                    {
                        OpenWrapperToken(token, ConsoleTokenType.Array, out token, tokenStack);
                        AddValueToTokenParent(token, tokenChar, tokenStack);
                        token.TokenLocation = pos + 1;
                    }
                    else
                    {
                        AddValueToToken(token, tokenChar, ref input, ref pos, tokenStack);
                    }
                    break;
                
                case ')':
                case '}':
                case ']':
                    
                    if (state != TokenizerState.InQuote && tokenStack.Peek().TokenType is ConsoleTokenType.CommandCollection or ConsoleTokenType.Array or ConsoleTokenType.Object)
                    {
                        AddValueToTokenParent(token, tokenChar, tokenStack);
                        CloseWrapperToken(token, out token, tokenStack);
                        token.TokenLocation = pos + 1;
                    }
                    else if (state == TokenizerState.InQuote)
                    {
                        AddValueToToken(token, tokenChar, ref input, ref pos, tokenStack);
                    }
                    break;
                
                case ' ':
                    
                    if (state != TokenizerState.InQuote)
                    {
                        AddValueToTokenParent(token, tokenChar, tokenStack);
                        if (StoreCurrentToken(token, out token, tokenStack))
                        {
                            // TODO: Do something with the successful storing of a token?
                        }
                        else
                        {
                            // TODO: Do something with the failed storing of a token?
                        }
                        token.TokenLocation = pos + 1;
                    }
                    else
                    {
                        AddValueToToken(token, tokenChar, ref input, ref pos, tokenStack);
                    }

                    break;
                
                case '$':
                    if (state != TokenizerState.InQuote)
                    {
                        token.TokenType = ConsoleTokenType.Node;
                    }
                    AddValueToToken(token, tokenChar, ref input, ref pos, tokenStack);
                    break;
                
                case '@':
                    if (state != TokenizerState.InQuote)
                    {
                        if (input[pos + 1] == '(')
                        {
                            OpenWrapperToken(token, ConsoleTokenType.BindingCollection, out token, tokenStack);
                            AddValueToTokenParent(token, tokenChar, tokenStack);
                            AddValueToTokenParent(token, input[pos + 1], tokenStack);
                            pos++;
                            token.TokenLocation = pos + 1;
                        }
                        else
                        {
                            token.TokenType = ConsoleTokenType.Binding;
                            AddValueToToken(token, tokenChar, ref input, ref pos, tokenStack);
                        }

                    }
                    else
                    {
                        AddValueToToken(token, tokenChar, ref input, ref pos, tokenStack);
                    }

                    break;
                
                case '&':
                    if (state != TokenizerState.InQuote)
                    {
                        token.TokenType = ConsoleTokenType.BindingParameter;
                    }
                    AddValueToToken(token, tokenChar, ref input, ref pos, tokenStack);
                    break;
                
                case '.':
                    if (token.TokenType == ConsoleTokenType.Node)
                    {
                        token.TokenType = ConsoleTokenType.NodeProperty;
                    }
                    
                    AddValueToToken(token, tokenChar, ref input, ref pos, tokenStack);
                    
                    break;
                
                case ':':
                    if (token.TokenType == ConsoleTokenType.Node)
                    {
                        token.TokenType = ConsoleTokenType.NodeCommand;
                    }
                    
                    AddValueToToken(token, tokenChar, ref input, ref pos, tokenStack);
                    
                    break;
                
                default:
                    AddValueToToken(token, tokenChar, ref input, ref pos, tokenStack);
                    break;
            }
       
            pos++;
        }

        while (tokenStack.Count > 0)
        {
            var nextToken = tokenStack.Pop();
            if (token != null && (token.IsValueToken && !string.IsNullOrWhiteSpace(token.TokenValue) || !token.IsValueToken && token.SubTokens.Count > 0) && !nextToken.SubTokens.Contains(token))
            {
                nextToken.SubTokens.Add(token);
            }

            token = nextToken;
        }
        
        
        return token;
    }
    
    
}