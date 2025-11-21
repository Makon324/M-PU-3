; TextEditor.as
; A simple text editor that reads keys from the keyboard device at port 33,
; echoes printable characters (ASCII 32-126) to the console output at port 32,
; handles backspace (deletes last char visually), enter (newline), and ESC (halt).
; Ignores other control codes and keys with codes >=127 (e.g., modifiers like shift).


.loop:
  PLD R1, 33         ; Load key code from keyboard port
  BRH 0, .zero_ram
  MLP R2, R1, 0	   ; If key already processed - loop
  BRH 1, .loop
  LDI R2, 1
  MSP R2, R1, 0
; Handle special keys
  SUBI R0, R1, 0x08  ; Check for backspace (0x08)
  BRH 0, .handle_backspace
  SUBI R0, R1, 0x0D  ; Check for enter (0x0D)
  BRH 0, .handle_enter
  SUBI R0, R1, 0x1B  ; Check for ESC (0x1B)
  BRH 0, .exit
; Skip if not printable ( <32 or >126 )
  LDI R7, 32
  SUB R0, R1, R7
  BRH 3, .loop       ; If R1 < 32 (C==0), skip
  LDI R7, 127
  SUB R0, R1, R7
  BRH 2, .loop       ; If R1 >=127 (C==1), skip
; Print the character
  PST R1, 32
  JMP .loop
.handle_backspace:
  LDI R2, 0x08       ; \b - move back
  PST R2, 32
  LDI R2, 0x20       ; space - overwrite
  PST R2, 32
  LDI R2, 0x08       ; \b - move back again
  PST R2, 32
  JMP .loop
.handle_enter:
  LDI R2, 0x0A       ; \n - newline
  PST R2, 32
  JMP .loop
.exit:
  HLT


.zero_ram:
  LDI R7, 0
.loop_zero_ram:
  MSP R0, R7, -1
  ADI R7, R7, 1
  BRH 2, .loop
  JMP .loop_zero_ram