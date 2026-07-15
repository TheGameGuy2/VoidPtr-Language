import sys
import os
from typing import Literal, Any
from dataclasses import dataclass as dc


def tokenize(path):
    with open(path, 'r') as f:
        src = f.read()

    def get(char):
        match char:
            case x if x.isdigit(): return 'iden'
            case x if x.isalpha(): return 'iden'
            case '_': return 'iden'
            case ' ': return 'space'
            case '\t' | '\n' | '\r': return 'format'
            case ';': return 'comment'
            case '\0': return 'terminator'
            case _: return 'symb'

    class Streamer:
        def __init__(self, toks):
            self.toks = toks

        def space(self):
            if all(c == ' ' for c in self.toks[0]):
                return len(self.toks.pop(0))
            return 0

        def peek(self):
            self.space()
            return self.toks[0]

        def pop(self):
            self.space()
            return self.toks.pop(0)

        def has(self):
            return len(self.toks) > 0

        def expect(self, should):
            be = self.pop()
            if should != be:
                print(f"Error: Expected `{should}` got `{be}`")
                sys.exit(1)



    state = None 
    buffer = ''
    comment = False
    toks = []
    for char in src + '\n':
        kind = get(char)

        if kind == 'comment': 
            comment = not comment
            buffer = ''
            continue

        if kind != state and not comment:
            if state != 'format' and buffer:
                toks.append(buffer)
            buffer = '' 

        buffer += char
        state = kind

    return Streamer(toks)


@dc
class AstDef:
    name : str
    body : "AstProg"

    @classmethod
    def parse(cls, stream):
        name = stream.pop()
        body = AstProg.parse(stream)
        return cls(name, body)

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
            case x:
                return cls(int(x), 'direct')

@dc
class AstAssign:
    a : AstValue
    b : AstValue
    op : Literal['and', 'or', 'xor', 'shl', 'shr', 'none']
    dst : int

    @classmethod
    def parse(cls, stream):
        a = AstValue.parse(stream)
        b = None
        op = 'none'
        
        ops = { '&':'and', '|':'or', '^':'xor', '<':'shl', '>':'shr' }
        if stream.peek() in ops:
            op = ops[stream.pop()]
            a = AstValue.parse(stream)

        stream.expect('->')
        dst = int(stream.pop())

        return cls(a, b, op, dst)




@dc
class AstLabel:
    name : str

    @classmethod
    def parse(cls, stream):
        return cls(stream.pop())

@dc
class AstJump:
    name : str

    @classmethod
    def parse(cls, stream):
        stream.expect("'")
        return cls(stream.pop())

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



@dc
class AstProg:
    nodes : list

    @staticmethod
    def parse_node(stream):
        match stream.peek():
            case '#': 
                stream.expect('#')
                match stream.pop():
                    case 'def': return AstDef.parse(stream)
                    case 'end': return None

            case x if x.startswith('_'): return AstLabel.parse(stream)
            case "'":                    return AstJump.parse(stream)
            case '?':                    return AstBranch.parse(stream)
            case _:                      return AstAssign.parse(stream)


    @classmethod
    def parse(cls, stream):
        nodes = []
        while stream.has() and (
                (node := cls.parse_node(stream))
                is not None
        ): nodes.append(node)
        return nodes

    @staticmethod
    def parse_file(path):
        stream = tokenize(path)
        return AstProg.parse(stream)




def main():
    path = sys.argv[1] if len(sys.argv) > 1 else 'main.vptr'
    if not os.path.isfile(path):
        print(f"Error: No such file: `{path}`")
        sys.exit(1)

    AstProg.parse_file(path)



if __name__ == "__main__":
    main()




