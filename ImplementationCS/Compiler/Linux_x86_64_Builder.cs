using System.Runtime.CompilerServices;
using Errors;
using Interpreter;

namespace Compiler;
public sealed class LinuxAsmBuilder : AsmBuilder
{
    //This is a mess
    private Dictionary<int,string> labelDecode;
    private Dictionary<int, string> sysCallLabels = [];
    private bool endWithSyscall = true; //global state for adding dynamic syscall after instruction.
    private bool addBranchLabel = false; //Whether to add a branch label after the next instruction.
    private uint branchLabelId = 0; //label id used for compare to skip next instruction.
    private uint currentInstruction = 0;
    public LinuxAsmBuilder(Dictionary<int,string> labelDecode)
    {
        this.labelDecode = labelDecode;

        sysCallLabels.Add(1,"_sys.printnum");
        sysCallLabels.Add(2,"_sys.printchar");
        sysCallLabels.Add(3,"_sys.readchar");
        sysCallLabels.Add(4,"_sys.allocbytes");
        sysCallLabels.Add(5,"_sys.deallocbytes");
        sysCallLabels.Add(6,"_sys.load32");
        sysCallLabels.Add(7,"_sys.write32");
        sysCallLabels.Add(8,"_sys.savepc");
        sysCallLabels.Add(9,"_sys.writepc");

    }

    public override void GenerateHeader()
    {
        
        foreach(string line in File.ReadAllLines(Path.Combine(Path.GetDirectoryName(Environment.ProcessPath),"header.asm")))
        {
            finalInstructions.Add(line);
        }
    }

    private void Add(string instr)
    {
         finalInstructions.Add($"{instr}");
    }

    

    private void LoadValue(InstrValue instValue, string reg)
    {
        switch(instValue.mode)
        {
            case AddressMode.Const:
                Add($"mov {reg}, {instValue.value}");
            break;

            case AddressMode.Direct:
                Add($"movzx {reg}, byte [memory+{instValue.value}]");
            break;

            case AddressMode.Indirect:
                Add($"movzx {reg}, byte [memory+{instValue.value}]");
                Add($"movzx {reg}, byte [memory+{reg}]");
            break;
        }
    }

    private void LoadAddress(InstrValue instrValue, string reg)
    {
        switch(instrValue.mode)
        {
            case AddressMode.Direct:
                Add($"mov {reg}, {instrValue.value}");
            break;

            case AddressMode.Indirect:
                Add($"movzx {reg}, byte [memory+{instrValue.value}]");
            break;
        }
    }

    

    public override void Emit(Instruction instruction)
    {
        endWithSyscall = true;


        bool branch = addBranchLabel;
        addBranchLabel = false;
        
        Add($"; {instruction}");
        Add($"_i.{currentInstruction}:");
        Add($"mov pc, {currentInstruction}");
        currentInstruction++;
        switch(instruction.type)
        {
            case Operator.Assign:
                MakeAssign(instruction);
            break;

            case Operator.Not:
                MakeNot(instruction);
            break;

            case Operator.And or Operator.Or or Operator.Xor or Operator.ShiftLeft or Operator.ShiftRight:
                MakeLogical(instruction);
            break;

            case Operator.Cmp:
                MakeCompare(instruction);
            break;

            case Operator.Jmp:
                EmitJump(instruction.val1.value);
            break;
        
        }
        if(endWithSyscall)
        {    
            MakeDynSyscall();
        }
        if(branch)
        {
            Add($"_sys.bL{branchLabelId}:");
            branchLabelId++;
        }
        
    }

    private void MakeCompare(Instruction instruction)
    {
        endWithSyscall = false;

        LoadValue(instruction.val1, "rax");
        Add("cmp rax, 0");
        Add($"jne _sys.bL{branchLabelId}");

        addBranchLabel = true;
    }
    
    private void MakeLogical(Instruction instruction)
    {
        

        if(instruction.val3.mode == AddressMode.Direct && instruction.val3.value != 0){ endWithSyscall = false; }

        string op = "";
        switch(instruction.type)
        {
            case Operator.And:
                op = "and";
            break;

            case Operator.Or:
                op = "or";
            break;

            case Operator.Xor:
                op = "xor";
            break;

            case Operator.ShiftLeft:
                op = "shl";
            break;

            case Operator.ShiftRight:
                op = "shr";
            break;
        }


        //Very unoptimised
        LoadValue(instruction.val1,"rax");
        LoadValue(instruction.val2, "rcx");
        LoadAddress(instruction.val3, "rbx");

        if(op == "shl" || op == "shr")
        {
            
            Add($"{op} rax,cl");
            Add($"mov [memory+rbx], al");
            return;
        }

        
        Add($"{op} rax,rcx");
        Add($"mov [memory+rbx], al");

    }

    private void MakeNot(Instruction instruction)
    {
        if(instruction.val1.mode == instruction.val2.mode && instruction.val1.value == instruction.val2.value)
        {   
            if(instruction.val1.mode == AddressMode.Indirect)
            {
                LoadAddress(instruction.val1, "rax");
                Add($"not byte [memory+rax]");
            }
            else
            {     
                Add($"not byte [memory+{instruction.val2.value}]");
            }

        }
        else if(instruction.val1.mode == AddressMode.Const)
        {
            LoadAddress(instruction.val2,"rax");
            Add($"mov byte [memory+rax], {instruction.val1.value}");
            Add($"not byte [memory+rax]");
        }
        else
        {
            LoadValue(instruction.val1,"rax");
            Add($"not rax");
            LoadAddress(instruction.val2,"rbx");
            Add($"mov [memory+rbx], rax");
        }
    }

    private void MakeDynSyscall()
    {
        Add("call _sys.dsyscall");
    }

    private void MakeAssign(Instruction instruction)
    {
        if(instruction.val1.mode == AddressMode.Const && instruction.val2.mode == AddressMode.Direct)
        {
            endWithSyscall = false; 
            if(instruction.val2.value == 0)
            {
                EmitSyscall(instruction.val1.value);
                return;
            }

            Add($"mov byte [memory+{instruction.val2.value}], {instruction.val1.value}");
            
        }
        else if(instruction.val1.mode == AddressMode.Const)
        {
            LoadAddress(instruction.val2,"rax");
            Add($"mov byte [memory+rax],{instruction.val1.value}");
        }
        else
        {
            LoadAddress(instruction.val2, "rax");
            LoadValue(instruction.val1, "rbx");
            
            Add($"mov [memory+rax], bl");
        }

        if(instruction.val2.mode == AddressMode.Direct && instruction.val2.value != 0) {endWithSyscall = false;}
    }

    
    

    public override void EmitLabel(int instruction_index)
    {
        Add("_usr."+labelDecode[instruction_index]+":\n");
    }

    public override void EmitJump(int jumpToLabel)
    {
        endWithSyscall = false;
        Add($"jmp {"_usr."+labelDecode[jumpToLabel]}");
    }

    public override void EmitSyscall(int syscallID)
    {
        try
        {
            Add("call " + sysCallLabels[syscallID]);
        }
        catch(Exception)
        {
            ErrorHandler.Throw($"Unrecognized direct syscall {syscallID}.");   
        }
    }

    public override void End()
    {
        Add("_exit:");
        Add("mov rdi, 0");
        Add("mov rax, 60");
        Add("syscall");

        Add("section .rodata");
        Add("instrcts:");
        for(uint i = 0; i<currentInstruction; i++)
        {
            Add($"dd _i.{i} - _start");
        }

        
    }
    
}
