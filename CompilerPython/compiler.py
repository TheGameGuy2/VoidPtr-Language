# VoidPtr compiler implementation by: S1monr3dst0ne07 <https://github.com/S1monr3dst0ne07>


import sys
import os
from typing import Literal, Any
from dataclasses import dataclass as dc
import itertools
import subprocess

# 64-bit compiler => 8 bytes per word
WORD_SIZE = 8
MEM_SIZE  = 1 << 16 # 65535 words should be enough

def lex(path):
    class Streamer:
        def __init__(self, toks):
            self.toks = toks

        def peek(self, offset=0):
            return self.toks[offset] if len(self.toks) > offset else None

        def pop(self):
            return str(self.toks.pop(0))

        def has(self):
            return len(self.toks) > 0

        def expect(self, should):
            be = self.pop()
            if should != be:
                print(f"Error: Expected `{should}` got `{be}`")
                sys.exit(1)

    def tokenize(path):
        with open(path, 'r') as f:
            src = f.read()

        def get(char):
            match char:
                case x if x.isdigit(): return 'iden'
                case x if x.isalpha(): return 'iden'
                case '_': return 'iden'
                case '.': return 'iden'
                case '=': return 'iden'
                case ' ' | '\t' | '\n' | '\r': return 'format'
                case ';': return 'comment'
                case '\0': return 'terminator'
                case '"': return 'quote'
                case _: return 'symb'

        state = None 
        buffer = ''
        comment = False
        string  = False
        toks = []
        for char in src + '\n':
            kind = get(char)

            if kind == 'comment': 
                comment = not comment
                buffer = ''
                continue

            if kind == 'quote':
                if not string: buffer = ''
                state = 'quote'
                string = not string

            if kind != state and not comment and not string:
                if state != 'format' and buffer:
                    toks.append(buffer)
                buffer = '' 

            buffer += char
            state = kind

        return toks

    def preprocess(raw):
        TOKS = 0
        USAGE = 1
        defs : dict[str, list[list, int]] = {}

        def instance(name): #create instance of macro 
            out = []
            usage = defs[name][USAGE]
            for tok in defs[name][TOKS]:
                if tok.startswith('__'):
                    tok += f"_inst{usage}"
                out.append(tok)
            defs[name][USAGE] += 1
            return out

        def get():
            nonlocal raw, defs
            tok = raw.pop(0)

            #auto expand
            if tok in defs:
                raw[:0] = instance(tok)
                return get()

            return tok

        def run():
            nonlocal raw, defs
            out = []
            while raw:
                tok = get()
                if tok != '#': #normal token
                    out.append(tok)
                    continue

                #proprocessor prefix
                match get():
                    case 'def':
                        name = get()
                        defs[name] = [None, None]
                        defs[name][TOKS] = run()
                        defs[name][USAGE] = 0
                    case 'end':
                        break
                    case 'stream':
                        base = int(get())
                        content = run()
                        while content:
                            first = content.pop(0)
                            if '"' in first:
                                for char in first.strip('"'):
                                    out += ['$', ord(char), '->', base]
                                    base += 1
                                continue

                            assert first == '$'
                            out += ['$', content.pop(0), '->', base]
                            base += 1
                    case 'paste':
                        rel_path = get()
                        abs_path = os.path.join(os.path.dirname(path), rel_path)
                        tokens, subdefs = full(abs_path)
                        defs.update(subdefs)
                        out += tokens

            return out

        return run(), defs

    def full(path):
        return preprocess(tokenize(path))

    tokens, _ = full(path)
    print(f"Tokens: {len(tokens)}")
    return Streamer(tokens)




@dc
class AstValue:
    number : int
    kind   : Literal['direct', 'indirect', 'lit']

    @classmethod
    def parse(cls, stream):
        match stream.pop():
            case '[':
                x = stream.pop()
                stream.expect(']')
                return cls(int(x), 'indirect')
            case '$':
                x = stream.pop()
                return cls(int(x), 'lit')
            case x if '"' in x:
                return cls(ord(x.strip('"')[0]), 'lit')
            case x:
                return cls(int(x), 'direct')

    def load(self, emit, reg):
        addr = self.number * WORD_SIZE
        match self.kind:
            case 'lit':      emit(f'mov {reg}, {self.number}')
            case 'direct':   emit(f'mov {reg}, [mem + {addr}]')
            case 'indirect': 
                emit(f'mov {reg}, [mem + {addr}]')
                emit(f'mov {reg}, [mem + {reg}*{WORD_SIZE}]')

    def store(self, emit, reg):
        addr = self.number * WORD_SIZE
        match self.kind:
            case 'lit':
                print("Error: Storing into literal value")
                sys.exit(1)
            case 'direct':
                emit(f'mov [mem + {addr}], {reg}')
            case 'indirect':
                emit(f'mov rdi, [mem + {addr}]')
                emit(f'mov [mem + rdi*{WORD_SIZE}], {reg}')

        if self.number == 0: #syscall
            emit('call sys')


@dc
class AstNot:
    src : AstValue
    dst : AstValue

    @classmethod
    def parse(cls, stream):
        stream.expect("!")
        src = AstValue.parse(stream)
        stream.expect("->")
        dst = AstValue.parse(stream)
        return cls(src, dst)

    def compile(self, emit):
        self.src.load(emit, 'rax')
        emit('not rax')
        self.dst.store(emit, 'rax')


ops = { '&':'and', '|':'or', '^':'xor', '<':'shl', '>':'shr' }


@dc
class AstAssign:
    a : AstValue
    b : AstValue
    op : Literal['and', 'or', 'xor', 'shl', 'shr', 'none']
    dst : AstValue

    @classmethod
    def parse(cls, stream):
        a = AstValue.parse(stream)
        b = None
        op = 'none'
        
        if stream.peek() in ops:
            op = ops[stream.pop()]
            b = AstValue.parse(stream)

        stream.expect('->')
        dst = AstValue.parse(stream)

        return cls(a, b, op, dst)

    def compile(self, emit):
        self.a.load(emit ,"rax")

        if self.b is not None:
            reg = "cl" if self.op in ('shl', 'shr') else "rbx"
            self.b.load(emit, reg)
            emit(f"{self.op} rax, {reg}")

        self.dst.store(emit, 'rax')




@dc
class AstLabel:
    name : str

    @classmethod
    def parse(cls, stream):
        return cls(stream.pop())

    def compile(self, emit):
        emit(f'{self.name}:')


@dc
class AstJump:
    name : str

    @classmethod
    def parse(cls, stream):
        stream.expect("'")
        return cls(stream.pop())

    def compile(self, emit):
        emit(f'jmp {self.name}')


skip_gen = itertools.count(0)

@dc
class AstBranch:
    cond : AstValue
    target : Any

    @classmethod
    def parse(cls, stream):
        stream.expect('?')
        cond = AstValue.parse(stream)
        target = AstProg.parse_node(stream)
        return cls(cond, target)

    def compile(self, emit):
        skip_label = f"skip{next(skip_gen)}"

        self.cond.load(emit, 'rax')
        emit(f"cmp rax, 0")
        emit(f"jne {skip_label}")
        self.target.compile(emit)
        emit(f"{skip_label}:")


@dc
class AstProg:
    nodes : list

    @staticmethod
    def parse_node(stream):
        match stream.peek():
            case x if x.startswith('_'): return AstLabel.parse(stream)
            case "'":                    return AstJump.parse(stream)
            case '?':                    return AstBranch.parse(stream)
            case '!':                    return AstNot.parse(stream)
            case _:                      return AstAssign.parse(stream)


    @classmethod
    def parse(cls, stream):
        nodes = []
        while stream.has() and (
                (node := cls.parse_node(stream))
                is not None
        ): nodes.append(node)
        return cls(nodes)

    def compile(self, emit):
        for node in self.nodes:
            node.compile(emit)




def main():
    path = sys.argv[1] if len(sys.argv) > 1 else 'main.vptr'
    if not os.path.isfile(path):
        print(f"Error: No such file: `{path}`")
        sys.exit(1)

    asm = []
    emitter = lambda x: asm.append(x)

    #actual compile 
    stream = lex(path)
    root = AstProg.parse(stream)

    #fasm header
    emitter("format ELF64 executable")
    emitter("entry start")
    emitter(f"mem: rq {MEM_SIZE}")
    emitter(f"buf: rb 4096 \n db 10")
    emitter("segment readable executable")
    emitter(f"""
sys:
    cmp qword [mem], 1
    je sys_print
    cmp qword [mem], 2
    je sys_char_out
    cmp qword [mem], 10
    je sys_file
    ret

sys_print:
    mov rax, [mem + {WORD_SIZE}]        ; load pointer
    mov rax, [mem + rax*{WORD_SIZE}]    ; load value
    mov rsi, 10                         ; divisor = 10
    mov rdi, 4095                       ; digit index

sys_print_build:
    xor rdx, rdx                        ; clear high register
    div rsi                             ; extract digit
    add dl, 48                          ; convert to ascii
    mov [buf + rdi], dl                 ; save digit
    dec rdi

    cmp rax, 0
    jne sys_print_build                 ; check loop exit
    inc rdi

    mov rdx, 4095
    sub rdx, rdi
    inc rdx
    inc rdx

    lea rsi, byte [rdi+buf]             ; buf = buffer + index
    mov rdi, 1                          ; fd = stdout
    mov rax, 1                          ; sys_write
    syscall
    ret

sys_char_out:
    mov rax, [mem + {WORD_SIZE}]
    mov rax, [mem + rax*{WORD_SIZE}]
    mov [buf], rax

    mov rdx, 1
    mov rsi, buf
    mov rdi, 1
    mov rax, 1
    syscall
    ret

sys_file:
    mov rsi, [mem + {WORD_SIZE}]
    mov rdi, buf
sys_file_path_loop:
    mov rax, [mem + rsi*{WORD_SIZE}]
    mov [rdi], rax
    inc rsi
    inc rdi

    cmp rax, 0
    jne sys_file_path_loop
    mov r10, [mem + rsi*{WORD_SIZE}]


    mov rdi, buf    ; filename = buf
    mov rsi, 0      ; no flags
    mov rdx, 0      ; mode = readonly
    mov rax, 2      ; sys_open
    syscall

    mov rdi, rax    ; file pointer
    mov rsi, buf    ; load file into buffer
    mov rdx, 4096   ; max read
    mov rax, 0      ; sys_read
    syscall
    mov rdx, rax    ; save number of read bytes

    mov rax, 3      ; sys_close
    syscall


    mov rdi, r10    ; copy destination
    mov rsi, buf    ; copy source
sys_file_read_loop: 
    xor rax, rax
    mov al, byte [rsi]
    mov [mem+rdi*{WORD_SIZE}], rax
    inc rsi
    inc rdi

    dec rdx
    jne sys_file_read_loop

    ret


            """)

    emitter("start:")

    root.compile(emitter)

    emitter("mov rdi, 0")
    emitter("mov rax, 60")
    emitter("syscall")

    build_path = 'build.asm'
    out_path   = 'build.out'
    with open(build_path, 'w') as f:
        f.write('\n'.join(asm))


if __name__ == "__main__":
    main()




