using Errors;
using Lexing;
using Parsing;
using Interpreter;
using Compiler;
using System.IO;
using System.Diagnostics;

string file = "main.vptr";

if(args.Length > 0)
{
    if(File.Exists(args[0]))
    {
        file = Path.GetFullPath(args[0]);
    }
    else
    {
        ErrorHandler.Throw($"Input file '{Path.GetFullPath(args[0])}' was not found.");
    }
}
else
{
    if(!File.Exists(file))
    {
        ErrorHandler.Throw("No 'main.vptr' file could be found. Either create one or pass your file.");
    }
}



// --- Generating Tokens ---
Lexer l = new(File.ReadAllText(file),file);

Console.WriteLine("-Lexing");

string toks = "";

List<Token> tokens = l.MakeTokens();

PreProcessor processor = new(tokens,file);

tokens = processor.Process();

foreach(Token t in tokens)
{
    toks+=t.ToString() + "\n";
}
File.WriteAllText("tok_dump.txt",toks);

// --- Generating Instructions ---
Console.WriteLine("-Parsing");

bool doCompile = args.Contains("-compile");

Parser parser = new(tokens, doCompile);

List<Instruction> instructions = parser.MakeInstructs();

string instStr = "";
int instrCount = 0;
foreach(Instruction inst in instructions)
{
    instStr += instrCount +": "+ inst.ToString() + "\n";
    instrCount++;
}
File.WriteAllText("inst_dump.txt",instStr);


if(doCompile)
{
    string fileName = Path.GetFileName(file).Replace(".vptr","");
    Console.WriteLine("Compiling...");
    Compiler.Compiler compiler = new(instructions,new LinuxAsmBuilder(parser.GetLabelDecode()));
    string[] code = compiler.Compile();
    File.WriteAllLines($"{fileName}.asm",code);
    
    Process nasm = Process.Start("nasm", $"-f elf64 {fileName}.asm -O2 -o {fileName}.o");
    nasm.WaitForExit();

    Process.Start("ld", $"{fileName}.o -o {fileName}");

}
else
{
    // --- Running code ---
    Console.WriteLine("Executing.");

    Engine intpr = new(instructions);
    intpr.Run();
}


