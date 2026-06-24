using Errors;

namespace Lexing;


public enum TokenType : byte
{
    Address,
    Label,
    Value,
    Name,
    OpenBr,
    ClosedBr,
    Assign, // ->
    And,
    Or,
    Not,
    Cmp,
    Jmp,
    ShiftLeft,
    ShiftRight,
    End

}

public struct Token
{
    public TokenType type;
    public string val = "null";
    public int line = -1;

    public Token(TokenType typ, string val, int line)
    {
        type = typ;
        this.val = val;
        this.line = line;
    }

    public Token(TokenType typ, string val)
    {
        type = typ;
        this.val = val;
    }

    public override string ToString()
    {
        return $"[{type} : {val}]";
    }
}

public class Lexer
{
    private string code;
    private int curIndx = -1;
    private char current;

    private string numbers = "0123456789"; 
    private Dictionary<char,Token> opMap = new();    
    private Dictionary<string, Token> keywordMap = new();
    private HashSet<char> skipChars;
    private int lineCount = 0;

    public Lexer(string code)
    {
        this.code = code;

        skipChars = ['\n',' ','\t','\r'];

        opMap.Add('[',new(TokenType.OpenBr,"["));
        opMap.Add(']',new(TokenType.ClosedBr,"]"));
        opMap.Add('&',new(TokenType.And,"&"));
        opMap.Add('|',new(TokenType.Or,"|")); 
        opMap.Add('!',new(TokenType.Not,"!"));
        opMap.Add('<',new(TokenType.ShiftLeft,"<"));
        opMap.Add('>',new(TokenType.ShiftRight,">"));
        opMap.Add('?',new(TokenType.Cmp,"?"));
        opMap.Add('\'',new(TokenType.Jmp,"'"));

        keywordMap.Add("->",new(TokenType.Assign,"->"));
    }

    private char Next()
    {
        curIndx++;
        if(curIndx<code.Length)
        {
            if(current == '\n') {lineCount++;}
            current = code[curIndx];
            return code[curIndx];
        }
        current = '\0';
        return '\0';
    }

    public List<Token> MakeTokens()
    {
        List<Token> tokens = new();


        while(Next() != '\0')
        {
            if(current == ';')
            {
                MakeComment();
            }
            else if(opMap.TryGetValue(current,out Token tok))
            {
                Token tcpy = tok;
                tcpy.line = lineCount;
                tokens.Add(tcpy);
            }
            else if(skipChars.Contains(current))
            {
                continue;
            }
            else
            {
                
                if(current == '$')
                {
                    Next();
                    if(numbers.Contains(current))
                    {
                        tokens.Add(new(TokenType.Value, MakeNum(),lineCount));
                    }
                    else
                    {
                        ErrorHandler.Throw("Expected number after $.");
                    }
                }
                else if(numbers.Contains(current))
                {
                    tokens.Add(new(TokenType.Address,MakeNum(),lineCount));
                    curIndx--; //Else next char gets skipped.
                }
                else if(current == '_')
                {
                    tokens.Add(MakeLabel());
                }
                else
                {
                    tokens.Add(MakeKeyword());
                }
            }

        }
        tokens.Add(new(TokenType.End,"END"));
        return tokens;
    }

    private void MakeComment()
    {
        int start = lineCount;
        while(Next() != ';')
        {
            if(current == '\0')
            {
                ErrorHandler.Throw("Unclosed comment.", start);
            }
        }
    }

    private string MakeNum()
    {
        string num = $"{current}";
        while(numbers.Contains(Next()))
        {
            num+=current;
        }
        return num;
    }

    private Token MakeLabel()
    {
        string lName = $"{current}";
        Next();

        while(!skipChars.Contains(current) && current != '\0')
        {
            lName += current;
            Next();
        }
        
        return new Token(TokenType.Label, lName, lineCount);
    }

    private Token MakeKeyword()
    {
        string word = $"{current}";
        Next();

        while(!skipChars.Contains(current) && current != '\0')
        {
            word+=current;
            Next();
        }

        if(keywordMap.TryGetValue(word, out Token tok))
        {
            Token tcpy = tok;
            tcpy.line = lineCount;
            return tcpy;
        }
        else
        {
            return new(TokenType.Name, word, lineCount);
        }
    }

}



