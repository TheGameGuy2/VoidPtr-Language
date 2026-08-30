bits 64

%define memory r12 ;heap pointer

section .rodata
    callmap: dq _sys.printchar, _sys.printnum, _sys.readchar, _sys.allocbytes, _sys.deallocbytes
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

_start:
    mov rdi, 0 ;getting current brk
    mov rax, 12
    syscall

    mov memory, rax

    mov rdi, memory ;allocating initial 256 bytes
    add rdi, 256
    mov rax, 12
    syscall

