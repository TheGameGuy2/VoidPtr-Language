import sys
import os



def tokenize(path):
    with open(path, 'r') as f:
        src = f.read()

    def get(char):
        match char:
            case x if x.isdigit(): return 'numb'
            case x if x.isalpha(): return 'iden'
            case '_': return 'iden'
            case ' ': return 'space'
            case '\t' | '\n' | '\r': return 'format'
            case ';': return 'comment'
            case '\0': return 'terminator'
            case _: return 'symb'

    state = None 
    buffer = ''
    comment = False
    for char in src + '\n':
        kind = get(char)

        if kind == 'comment': 
            comment = not comment
            buffer = ''
            continue

        if kind != state and not comment:
            if state != 'format' and buffer:
                print(f'"{buffer}"')
            buffer = '' 

        buffer += char
        state = kind



def main():
    path = sys.argv[1] if len(sys.argv) > 1 else 'main.vptr'
    if not os.path.isfile(path):
        print(f"Error: No such file: `{path}`")
        sys.exit(1)

    tokenize(path)



if __name__ == "__main__":
    main()




