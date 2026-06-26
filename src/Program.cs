using Interpreter;
using Lexing;
using Parsing;

Lexer l = new(File.ReadAllText("C:/Users/Andre/Desktop/CodeProjects/BitLang/src/test.bl"));

Console.WriteLine("-Tokens");

string toks = "";
List<Token> tokens = l.MakeTokens();

foreach(Token t in tokens)
{
    toks+=t.ToString() + "\n";
}

File.WriteAllText("tok_dump.txt",toks);

Parser parser = new(tokens);

List<Instruction> instructions = parser.MakeInstructs();



string instStr = "";
foreach(Instruction inst in instructions)
{
    instStr += inst.ToString() + "\n";
}

File.WriteAllText("inst_dump.txt",instStr);


Engine intpr = new(instructions);
intpr.Run();

