using System.Reflection.Emit;

namespace Compiler;


public class LinuxAsmBuilder
{
    private List<string> finalInstructions = new();
    private Dictionary<int,string> labelDecode;

    public AsmBuilder(Dictionary<int,string> labelDecode)
    {
        this.labelDecode = labelDecode;
    }

    public void GenerateHeader()
    {
        finalInstructions.Add( 
        "format ELF64 executable\n"+
        "entry start\n"+
        "mem: rb 256\n"+
        "segment readable executable\n"
        );
    }

    public void Emit(string instruction)
    {
        finalInstructions.Add(instruction+"\n");
    }

    public void EmitLabel(int instruction_index)
    {
        finalInstructions.Add(labelDecode[instruction_index]);
    }

    public void EmitJump(int jumpToLabel)
    {
        finalInstructions.Add($"jmp {labelDecode[jumpToLabel]}\n");
    }

    public void EmitSyscall(int syscallID)
    {
        
    }
    
}