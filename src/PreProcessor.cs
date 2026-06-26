using Errors;
using Lexing;

namespace Parsing;

public class PreProcessor
{
    private List<Token> tokens;
    private List<Token> processed = new();

    private int curIndx = -1;
    private Token current;
    private Dictionary<string, List<Token>> macroDict = new();

    public PreProcessor(List<Token> tokens)
    {
        this.tokens = tokens;
    }

    private void Expect(TokenType type)
    {
        if(current.type != type)
        {
            ErrorHandler.Throw($"Expected {type}, got {current.type}",current);
        }
    }

    private Token Next()
    {
        curIndx++;
        if(curIndx<tokens.Count)
        {
            current = tokens[curIndx];
            return current;
        }
        return new(TokenType.End,"END");
    }

    public List<Token> Process()
    {

        Next();
        while(current.type != TokenType.End)
        {
            if(current.type == TokenType.Macro)
            {
                MakeMacro();
            }
            else if(current.type == TokenType.Name)
            {
                if(macroDict.TryGetValue(current.val,out List<Token> toks))
                {
                    foreach(Token t in toks)
                    {
                        processed.Add(t);
                    }
                }
                else
                {
                    ErrorHandler.Throw($"Macro '{current.val}' not defined in current state.", current);
                }
            }
            else
            {
                processed.Add(current);
            }
            Next();
        }
        processed.Add(new(TokenType.End,"END"));
        return processed;
    }

    private void MakeMacro()
    {
        Expect(TokenType.Macro);

        if(current.val == "def")
        {
            MakeDef();
        }       
        else if(current.val == "paste")
        {
            MakePaste();
        }
    }

    private void MakePaste()
    {
        Next();
        Expect(TokenType.Name);
        
        string code;
        try
        {
            code = File.ReadAllText(current.val);
        }
        catch
        {
            ErrorHandler.Throw($"Could not find file '{current.val}' to paste.",current);
            return;
        }

        Lexer l = new(code, current.val);
        int cur = curIndx+1; //+1 because insert pushes elements towards end.

        foreach(Token t in l.MakeTokens())
        {
            if(t.type == TokenType.End) {break;}
            tokens.Insert(cur,t);
            cur++;
        }
        
    }

    private void MakeDef()
    {
        Next();
        Expect(TokenType.Name);

        if(macroDict.ContainsKey(current.val))
        {
            ErrorHandler.Throw($"Redefenition of macro '{current.val}'",current);
        }

        List<Token> macToks = new();
        macroDict.Add(current.val, macToks);

        Next();
        while(current.type != TokenType.EndMacro && current.type != TokenType.End)
        {
            if(current.type == TokenType.Name)
            {
                if(macroDict.TryGetValue(current.val,out List<Token> toks))
                {
                    foreach(Token t in toks)
                    {
                        macToks.Add(t);
                    }
                }
                else
                {
                    ErrorHandler.Throw($"Macro '{current.val}' not defined in current state.", current);
                }
                Next();
                continue;
            }
            else if(current.type == TokenType.Macro)
            {
                ErrorHandler.Throw($"You aren't powerfull enough to wield recursive macros, mortal.", current);
            }
            macToks.Add(current);
            Next();
        }
        Expect(TokenType.EndMacro);
    }
}
