
using Interpreter;

namespace Compiler;

public abstract class AsmBuilder
{
    public List<string> finalInstructions = new();
    public abstract void GenerateHeader();
    public abstract void Emit(Instruction instruction);
    public abstract void EmitLabel(int labelInstructionIndex);
    public abstract void EmitJump(int jumpToAddress);
    public abstract void EmitSyscall(int syscallID);
    public abstract void End();

}