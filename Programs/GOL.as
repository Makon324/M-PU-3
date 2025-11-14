; Conway Game Of Life (GOL.as)
; Implements Conway's Game on 32x32 grid
; - Uses deterministic start
; - PixelDisplay uses ports 11-15:
;     Port 11: Red
;     Port 12: Green
;     Port 13: Blue
;     Port 14: X coordinate (with bit 7 = 1 to set pixel)
;     Port 15: Y coordinate (with bit 7 = 1 to set pixel)
; - Display size: 128x128 (Architecture.DISPLAY_SIZE.Width/Height)
; - Uses R7 = 32 (grid size)

LDI R7, 32	   ; R7 = 32 -> width/height of the grid

;--- Start arrangement

LDI R1, 0b00000111
MST R1, 0x09

LDI R1, 0b00000111
MST R1, 0x23
LDI R1, 0b00000001
MST R1, 0x1B
LDI R1, 0b00000010
MST R1, 0x13


;-----------------------------------------------
.main_loop:
;-----------------------------------------------

  LDI R2, 0

.loop_y:
  LDI R1, 0      	; R1 = X coordinate, start at 0

.loop_x:  
  CAL .count_neigh_xy	; get (X, Y) into R3 and neigh count into R6  
  CAL .get_xy

  LDI R4, 1		; R5 <- (R6 == 3)
  LDI R5, 0
  SUBI R0, R6, 3
  MOVC R5, R4, 0

  SUBI R0, R6, 2	; R6 <- (R6 == 2)
  MOVC R6, R0, 1
  SHFT R6, R6		; R6 = 0|2 --> 0|1

  AND R3, R3, R6	; R3 <- (r3 && (r6 == 2))

  OR R3, R3, R5		; R3 <- (r6 == 3) || (r3 && (r6 == 2))
  BRH 0, .skip_set  
  CAL .set_xy
.skip_set:
  CAL .write_xy

  ADI R1, R1, 1  	; X = X + 1 -> move to next column
  SUB R0, R1, R7 	; X - 32 -> compute Z flag (Z=1 when X == 32)
  BRH 1, .loop_x 	; If Z == 0 (X != 32), continue row; Otherwise exit inner loop

.end_loop_x:
  ADI R2, R2, 1  	; Y = Y + 1 -> next row
  SUB R0, R2, R7 	; Y - 32 -> set Z flag when Y == 32
  BRH 1, .loop_y 	; If Z == 0 (Y != 32), continue; Otherwise Not

;--- SHFT all RAM 4 bits
  LDI R3, 0
.loop_shft:
  MLP R4, R3, -1	; R4 <- [R3 - (-1) -1] = [R3]
  SHFT R4, R4
  SHFT R4, R4
  SHFT R4, R4
  SHFT R4, R4
  MSP R4, R3, -1	; writeback R4

  ADI R3, R3, 1
  BRH 3, .loop_shft

;--- Wait
  PLD R1, 6
.loop_wait:
  PLD R2, 6
  SUB R2, R2, R1
  SUBI R0, R2, 2	; if at least ~2*255ms passed stop waiting
  BRH, 2, .main_loop
  JMP .loop_wait
  



;=====================================================================
; .write_xy function - writes on the screen cell (X[R1], Y[R2]) 
; alive if R3 == 1 or dead if R3 == 0
; also uses R4, R5, R6, does not modify R1 and R2
;=====================================================================
.write_xy:

  ADD R1, R1, R1  	; 4x times cordinates (base cordinates of a cell on display)	
  ADD R1, R1, R1
  ADD R2, R2, R2
  ADD R2, R2, R2

  MOV R5, R2		; R4, R5 - bounds
  MOV R4, R1	
  ADI R5, R5, 4
  ADI R4, R4, 4

  LDI R6, 0xFF		; set R3 into 0xFF or 0 depending on passed R3
  DPS R3, R6, 0
  PLD R3, 0

  PST R3, 11		; set RGB to R3
  PST R3, 12
  PST R3, 13
 
.loop_y_write:
  PST R2, 15		; Set Y coordinate (no pixel set: bit 7 = 0)	
  SUBI R1, R4, 4      	; R1 = X*4 coordinate, start at offset 0

.loop_x_write:
  LDI R6, 128    	; Load 128 into R6 (bit 7 = 1 -> will trigger pixel set)
  ADD R6, R1, R6 	; R6 = 128 + X -> sets X coordinate AND bit 7=1 -> triggers pixel write
  PST R6, 14     	; Set X coordinate and write pixel

  ADI R1, R1, 1  	; X = X + 1 -> move to next column
  SUB R0, R1, R4 	; set Z flag when X overflow (+4)
  BRH 1, .loop_x_write 	; If Z == 0 (X != 32), continue row; Otherwise exit inner loop

.end_loop_x_write:
  ADI R2, R2, 1  	; Y = Y + 1 -> next row
  SUB R0, R2, R5 	; set Z flag when Y overflown (+4)
  BRH 1, .loop_y_write 	; If Z == 0 (Y != 32), continue; Otherwise HLT 

  SUBI R4, R4, 4	; restore R1, R2
  SUBI R5, R5, 4
  MOV R1, R4
  MOV R2, R5
  SHFT R1, R1
  SHFT R1, R1
  SHFT R2, R2
  SHFT R2, R2

  RET 0



;=====================================================================
; .count_neigh_xy function - counts neighbours for (X[R1], Y[R2])
; returns in R6, also uses R3, R4, R5, does not modify R1 and R2
;=====================================================================
.count_neigh_xy:
  LDI R6, 0		; count neighbours in R6
  SUBI R1, R1, 1	; start counting in (X-1, Y-1)
  SUBI R2, R2, 1

  SUB R0, R7, R1
  BRH 3, .skip_count1
  SUB R0, R7, R2
  BRH 3, .skip_count1
  CAL .get_xy
  ADD R6, R6, R3
.skip_count1:
  ADI R1, R1, 1

  SUB R0, R7, R1
  BRH 3, .skip_count2
  SUB R0, R7, R2
  BRH 3, .skip_count2
  CAL .get_xy
  ADD R6, R6, R3
.skip_count2:
  ADI R1, R1, 1

  SUB R0, R7, R1
  BRH 3, .skip_count3
  SUB R0, R7, R2
  BRH 3, .skip_count3
  CAL .get_xy
  ADD R6, R6, R3
.skip_count3:
  ADI R2, R2, 1

  SUB R0, R7, R1
  BRH 3, .skip_count4
  SUB R0, R7, R2
  BRH 3, .skip_count4
  CAL .get_xy
  ADD R6, R6, R3
.skip_count4:
  ADI R2, R2, 1

  SUB R0, R7, R1
  BRH 3, .skip_count5
  SUB R0, R7, R2
  BRH 3, .skip_count5
  CAL .get_xy
  ADD R6, R6, R3
.skip_count5:
  SUBI R1, R1, 1

  SUB R0, R7, R1
  BRH 3, .skip_count6
  SUB R0, R7, R2
  BRH 3, .skip_count6
  CAL .get_xy
  ADD R6, R6, R3
.skip_count6:
  SUBI R1, R1, 1

  SUB R0, R7, R1
  BRH 3, .skip_count7
  SUB R0, R7, R2
  BRH 3, .skip_count7
  CAL .get_xy
  ADD R6, R6, R3
.skip_count7:
  SUBI R2, R2, 1

  SUB R0, R7, R1
  BRH 3, .skip_count8
  SUB R0, R7, R2
  BRH 3, .skip_count8
  CAL .get_xy
  ADD R6, R6, R3
.skip_count8:

  ADI R1, R1, 1		; restore original R1, R2

  RET 0			; return function with count in R6
  
  

;=====================================================================
; .get_xy function - gets (X[R1], Y[R2]) cordinate out of RAM - 0 or 1
; returns in R3, uses also R4, R5, does not modify R1 and R2
;=====================================================================
.get_xy:  
  ADD R3, R2, R2	; Multiply Y by 8 -> R3
  ADD R3, R3, R3
  ADD R3, R3, R3

  SHFT R4, R1		; Divide X by 4 -> R4
  SHFT R4, R4

  ADD R3, R3, R4	; Get RAM cell address -> R3

  LDI R4, 0x03		; lowest 2 bits mask -> R4
  AND R5, R1, R4	; lowest 2 bits of X -> R5
 
  LDI R4, 1 		; get bit mask for the necessary bit -> R4
  SHFT R5, R5
  BRH 3, .skip_times2
  ADD R4, R4, R4	; R4 * 2
.skip_times2:
  SHFT R5, R5
  BRH 3, .skip_times4
  ADD R4, R4, R4	; R4 * 4
  ADD R4, R4, R4
.skip_times4:

  LDI R5, 1
  MLP R3, R3, -1	; R3 <- [R3 - (-1) - 1] = [R3]
  AND R3, R3, R4	; > 0 -> 1 | =0 -> 0
  MOVC R3, R5, 1	; if Z == 0 (last !=0) --> R3 <- 1

  RET 0


;=====================================================================
; .set_xy function - sets (x[R1], y[R2]) cordinate to RAM - 1
; uses R4, R5, R6 does not modify R1 and R2; 
; sets out of 4 older bits of a given cell (unlike .get_xy)
;=====================================================================
.set_xy:  
  ADD R6, R2, R2	; Multiply Y by 8 -> R6
  ADD R6, R6, R6
  ADD R6, R6, R6

  SHFT R4, R1		; Divide X by 4 -> R4
  SHFT R4, R4

  ADD R6, R6, R4	; Get RAM cell address -> R6

  LDI R4, 0x03		; lowest 2 bits mask -> R4
  AND R5, R1, R4	; lowest 2 bits of X -> R5
 
  LDI R4, 1 		; get bit mask for the necessary bit -> R4
  SHFT R5, R5
  BRH 3, .skip_times2_
  ADD R4, R4, R4	; R4 * 2
.skip_times2_:
  SHFT R5, R5
  BRH 3, .skip_times4_
  ADD R4, R4, R4	; R4 * 4
  ADD R4, R4, R4
.skip_times4_:

  ADD R4, R4, R4	; shift left another 4 bits to set 4 older bits
  ADD R4, R4, R4
  ADD R4, R4, R4
  ADD R4, R4, R4

  MLP R5, R6, -1	; R5 <- [R6 - (-1) - 1] = [R6]
  OR R5, R5, R4		; write given bit to R5
  MSP R5, R6, -1   	; write back

  RET 0
  





