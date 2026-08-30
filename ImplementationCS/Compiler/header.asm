bits 64

section .bss
    memory: resb 256

section .rodata
    callmap: dq _sys.printchar, _sys.printnum, _sys.readchar
    call_map_max equ $

section .text

global _start

_sys.dsyscall: ;called when 0 is written indirectly and can't be resolved at compile time.
    
    movzx rax, byte [memory] ;syscall id -> rax
    test rax,rax
    jz .invalidcall

    sub rax,1

    lea rax, [callmap+rax*8]

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

    
    mov bl, byte [rsi] ;save old value
    
    add byte [rsi],48  ;convert to ascii
    

    mov rdi,1 ;TODO: convert multi character numbers
    mov rdx,1

    mov rax,1 ;syswrite
    syscall

    mov [rsi], bl ;restore original value
    mov byte [memory], 0
    ret

_sys.readchar:
    movzx rbx, byte [memory+1] ;load pointer value from 1
    lea rsi, [memory + rbx]

    mov rax, 0 ;read
    mov rdi, 0 ;stdin
    
    mov rdx, 1 ;reading 1 char
    syscall
    mov byte [memory], 0
    ret

_start:
    

