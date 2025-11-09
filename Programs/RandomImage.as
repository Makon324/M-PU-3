; RandomImage.as
; Generates a 128x128 random color image using the RNG and PixelDisplay devices.
; - RNG is at port 4 (read-only, returns random byte)
; - PixelDisplay uses ports 11-15:
;     Port 11: Red
;     Port 12: Green
;     Port 13: Blue
;     Port 14: X coordinate (with bit 7 = 1 to set pixel)
;     Port 15: Y coordinate (with bit 7 = 1 to set pixel)
; - Display size: 128x128 (Architecture.DISPLAY_SIZE.Width/Height)
; - Uses R7 = 128 (display size), R6 = Y, R5 = X

  LDI R7, 128    ; R7 = 128 -> width/height of display
  LDI R6, 0      ; R6 = Y coordinate, start at 0

.loop_y:
  PST R6, 15     ; Set Y coordinate (no pixel set: bit 7 = 0)
  LDI R5, 0      ; R5 = X coordinate, start at 0

.loop_x:
  PLD R1, 4      ; Load random byte from RNG -> R1 (Red)
  PST R1, 11     ; Store Red value to port 11

  PLD R1, 4      ; Load another random byte -> R1 (Green)
  PST R1, 12     ; Store Green value to port 12

  PLD R1, 4      ; Load another random byte -> R1 (Blue)
  PST R1, 13     ; Store Blue value to port 13

  LDI R2, 128    ; Load 128 into R2 (bit 7 = 1 -> will trigger pixel set)
  ADD R2, R2, R5 ; R2 = 128 + X -> sets X coordinate AND bit 7=1 -> triggers pixel write
  PST R2, 14     ; Set X coordinate and write pixel

  ADI R5, R5, 1  ; X = X + 1 -> move to next column
  SUB R0, R5, R7 ; X - 128 -> compute Z flag (Z=1 when X == 128)
  BRH 1, .loop_x ; If Z == 0 (X != 128), continue row; Otherwise exit inner loop

.end_loop_x:
  ADI R6, R6, 1  ; Y = Y + 1 -> next row
  SUB R0, R6, R7 ; Y - 128 -> set Z flag when Y == 128
  BRH 1, .loop_y ; If Z == 0 (Y != 128), continue; Otherwise HLT 

  HLT            ; Halt CPU - image fully rendered