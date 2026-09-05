bits 64

%define memory r12 ;heap pointer
%define pc r13d

section .rodata
    callmap: dq _sys.printchar, _sys.printnum, _sys.readchar, _sys.allocbytes, _sys.deallocbytes, _sys.load32, _sys.write32, _sys.savepc, _sys.writepc

    call_map_max equ ($-callmap)/8

section .text

global _start

_sys.dsyscall: ;called when 0 is written indirectly and can't be resolved at compile time.
    
    movzx rax, byte [memory] ;syscall id -> rax
    test rax,rax
    jz .invalidcall

    sub rax,1

    cmp rax,call_map_max ;bound checkings
    jge .invalidcall

    call [rax]
    
    .invalidcall:
    
    ret


_sys.printchar:
    movzx rax, byte [memory+1] ;load pointer value from 1
    lea rsi, [memory + rax] ;load address of char in rsi for syscall

    mov rdi,1
    mov rdx,1

    mov rax,1 ;syswrite
    syscall

    mov byte [memory],0
    ret

_sys.printnum:
    movzx rax, byte [memory+1] ;load pointer value from 1
    lea rsi, [memory + rax] ;load address of char in rsi for syscall

    mov r8b, byte [rsi];save old value
    mov al, r8b

    push r9
    mov r9b, 1 ;print 10 flag

    movzx ax, al
    xor bx,bx ;reset bx
    mov bl, 100 ;divide by 100

    div bl

    mov bx, ax ;store res in bx

    test al,al ;if n*10^2 is not 0 we need to force print 10^1 even if it is 0.
    jnz .forceprint10
    
    mov r9b, 0
    jmp .print10start

    .forceprint10:

    add al, '0' ;add 48
    mov [rsi], al ;move ah to byte at rsi for printing

    ;print 10^2
    mov rdi,1 
    mov rdx,1
    mov rax,1 ;syswrite
    syscall

    .print10start:
    ;print 10^1
    movzx ax, bh ;move result back into ax, move remainder into al and clean ax for 2. division 

    xor bx,bx ;reset bx
    mov bl, 10 ;divide by 10

    div bl

    mov bx, ax ;store res in bx
    
    test al,al
    jnz .print10
    test r9b,r9b
    jz .print_end 
    .print10:

    add al, '0' ;add 48
    mov [rsi], al ;move al to byte at rsi for printing

    mov rdi,1 
    mov rdx,1
    mov rax,1 ;syswrite
    syscall
    
    .print_end:
    ;print 10^0
    mov al, bh ;remainder in bh is our 10^2
    add al, '0'
    mov [rsi], al

    mov rdi,1 
    mov rdx,1
    mov rax,1 ;syswrite
    syscall

    mov [rsi], r8b
    pop r9 ;restore r9 because we are nice.
    mov byte [memory], 0
    ret

_sys.readchar:
    movzx rdx, byte [memory+1] ;load pointer value from 1
    lea rsi, [memory + rdx]

    mov rax, 0 ;read
    mov rdi, 0 ;stdin
    
    mov rdx, 1 ;reading 1 char
    syscall
    mov byte [memory], 0
    ret

_sys.allocbytes:
    movzx rdx, byte [memory+1] ;amount of bytes in 1
    
    mov rdi, 0 ;getting current brk
    mov rax, 12
    syscall

    add rax,rdx ;adding value
    mov rdi,rax ;pass new mem address

    mov rax, 12 ;allocating
    syscall

    mov byte [memory], 0
    ret

_sys.deallocbytes:
    movzx rdx, byte [memory+1] ;amount of bytes in 1

    
    mov rdi, 0 ;getting current brk
    mov rax, 12
    syscall

    sub rax,rdx ;adding value
    mov rdi,rax ;pass new mem address

    mov rax, 12 ;deallocating
    syscall



    mov byte [memory], 0
    ret

_sys.load32:
    ;layout: 1 points to -> [a0] [a1] [a2] [a3]
    movzx rdx, byte [memory+1] ;start address of pointer
    mov eax, [memory+rdx] ;address of 32 bit value
    mov al, byte [memory+rax] ;load byte from address
    mov byte [memory+1],al  ;write to 1

    mov byte [memory], 0
    ret

_sys.write32:
    ;layout: 1 points to -> [write value] [a0] [a1] [a2] [a3] 
    movzx rdx, byte [memory+1] ;pointer to write value
    mov eax, [memory+rdx+1] ;load write ddress
    mov dl, byte [memory+rdx] ;saving write value into dl

    mov [eax], dl ;writing to address
    
    mov byte [memory], 0
    ret

_sys.savepc:
    movzx rdx,byte [memory+1] ;dest. address
    mov [memory+rdx], pc

    mov byte [memory], 0
    ret


_sys.writepc:
    pop rax ;stack leak when dsyscall?
    movzx rax, byte [memory+1] 
    mov eax, [memory+rax] ;load address
    mov eax, [instrcts+rax*4] ;rel instruction adr.
    add rax, _start ;to abs. address

    mov byte [memory], 0
    jmp rax

_start:
    xor pc,pc

    mov rdi, 0 ;getting current brk
    mov rax, 12
    syscall

    mov memory, rax

    mov rdi, memory ;allocating initial 256 bytes
    add rdi, 256
    mov rax, 12
    syscall

