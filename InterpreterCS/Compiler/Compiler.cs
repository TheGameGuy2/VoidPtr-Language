using Errors;
using Interpreter;

namespace Compiler;




public class Compiler
{
    private List<Instruction> irInstr;
    private AsmBuilder currentBuilder;

    public Compiler(List<Instruction> labeledInstructions, AsmBuilder builder)
    {
        currentBuilder = builder;
        irInstr = labeledInstructions;
    }

    public string[] Compile()
    {
        currentBuilder.GenerateHeader();
        foreach(Instruction instruction in irInstr)
        {
            switch(instruction.type)
            {
                case Operator.Label:
                    currentBuilder.EmitLabel(instruction.val1.value);
                break;


                default:
                    currentBuilder.Emit(instruction);
                break;
            }
        }
        currentBuilder.End();
        return currentBuilder.finalInstructions.ToArray();
    }
    
}