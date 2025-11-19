# Custom 8-Bit CPU Project

## Overview

This project involves the design and implementation of a custom 8-bit CPU architecture, complete with an assembler written in Python and an emulator developed in C#. The goal is to create a simple yet functional 8-bit processor from scratch, allowing for experimentation with low-level computing concepts, assembly language programming, and emulation techniques. It features a bespoke instruction set architecture (ISA) with support for arithmetic, logical, control flow, and memory operations. The project includes:

- A standalone **Python assembler** that compiles `.as` assembly files into binary representations (output as `.txt` files with 0s and 1s).
- A **C# emulator** that simulates CPU execution, loading programs directly from `.as` files by embedding parts of the Python assembler via `Python.Runtime`. The emulator also includes a debug mode that provides detailed CPU state inspection and supports stepping through instructions for easier testing and troubleshooting.
- Sample programs to demonstrate functionality.

The emulator provides a full simulation environment with 3-stage execution pipeline, I/O devices, debugging functionality, and peripherals like a pixel display. This setup enables experimentation with assembly programming, CPU design, and emulation techniques.

## Important Notes

* **Do not delete the `.git` folder.**
  The project determines its root directory by searching upward for a .git folder, which it uses to locate `config.yaml`, `instructions.json`, and the `Assembler` directory.

* **Windows-only (for now).**
  The project currently supports **Windows**. Linux compatibility coming soon.

## Features

- **Custom 8-Bit ISA**: Defined in `instructions.json`, supporting instructions for ALU operations, branching, subroutines, I/O, and more.
- **Python Assembler**: Standalone tool for compiling assembly to text files representing binary.
- **C# Emulator**:
  - Loads and processes `.as` files using embedded Python components for tokenization, parsing, validation, and label resolution.
  - Simulates a multi-stage instruction pipeline with flushing for control-flow instructions.
  - Includes peripherals: SFML-based pixel display, console I/O, keyboard input, timer, RNG, multiplier, divider, and extensible I/O ports.
  - Execution modes: Full-speed run or interactive debug with stepping, step-over, CPU state view, and pipeline and program inspection.
  - Configurable debugging via `config.yaml`.
- **Extensibility**: Add new instructions via `instructions.json`, or custom devices by registering I/O ports.

## C# Emulator

The emulator is a .NET console application (`Emulator.exe`) that simulates the 8-bit CPU:

- **Program Loading**: Directly loads `.as` files by invoking embedded Python modules (tokenizer, parser, validator) via `Python.Runtime`. Resolves labels, compiles to an in-memory instruction list, and loads into program memory. No external binary generation is required for emulation.
- **CPU Simulation**:
  - **Pipeline**: 3-stage instruction pipeline (fetch, decode, execute) with automatic NOP flushing on branches, jumps, and calls to simulate realistic control-flow delays and hazards.
  - **Registers and Memory**: Includes 8 registers (R0 always reads as 0 and ignores writes; R1-R7 are general-purpose 8-bit registers), a flags register for conditionals, a dedicated 8-bit Stack Pointer (SP) with overflow/underflow protection, and a Program Counter (PC). Memory consists of 256 bytes of RAM and up to 1024 instructions in program memory (2 KiB program memory).
  - **Execution**: Runs in a continuous loop until encountering a `HLT` instruction or reaching the end of the program. Supports interactive debugging with single-step execution (advances one instruction at a time) and step-over functionality for subroutines (skips over calls and automatically handles post-`RET` NOP flushes, configurable via `config.yaml`).
- **Peripherals and I/O**:
  - **Pixel Display**: A 128x128 RGB pixel grid rendered in a resizable SFML window with aspect ratio preservation. Controlled via five consecutive I/O ports (ports 11-15): three for color channels (R at 11, G at 12, B at 13) and two for coordinates (X at 14, Y at 15, where the high bit on write triggers pixel updates at the specified position).
  - **Console Output**: A write-only I/O port (port 32) that interprets stored bytes as ASCII characters and prints them directly to the console.
  - **Keyboard Input**: Polls for key presses (including letters, digits, symbols, arrows, function keys, and modifiers) using the Windows API, maintaining a unique FIFO queue to prevent duplicates. Reading from the port dequeues and returns the next key code (as a byte); writing 0 clears the queue.
  - **Built-in Devices**:
    - **Multiplier**: Two consecutive read/write ports (ports 0-1) for input factors (8-bit each); reading returns the 16-bit product split across the ports (low byte on port 0, high byte on port 1).
    - **Divider**: Two consecutive read/write ports (ports 2-3; first for divisor/quotient at port 2, second for dividend/remainder at port 3); handles 8-bit division with quotient and modulus results (division by zero yields hardware-realistic values like 0xFF quotient).
    - **RNG**: A read-only port (port 4) that generates and returns a random 8-bit value on each load.
    - **Timer**: Four consecutive read-only ports (ports 5-8) providing the elapsed milliseconds since emulator start as a 32-bit little-endian value (least significant byte on port 5).
  - **Port Management**: Supports up to 256 individually addressable I/O ports, with dynamic registration for devices (including custom extensions) and error handling for conflicts or out-of-range access.
- **Debugging**:
  - Interactive mode (`--debug` flag): Displays CPU state, registers, pipeline, and program disassembly.
  - Key bindings from `config.yaml`: Step (e.g., F10), step-over (e.g., F9).
  - Option to auto-execute pipeline NOPs after RET in step-over.
- **Dependencies**: .NET 6+, SFML.Net (rendering), YamlDotNet (config), Microsoft.Extensions.Hosting (DI), Python.Runtime (assembler integration).

### Requirements

- **.NET SDK/Runtime**: Version 6.0 or later.
- **Python**: >=3.12 installed (for `Python.Runtime`, must be in system PATH).
- **NuGet Packages**:
  - `SFML.Net`
  - `YamlDotNet`
  - `Microsoft.Extensions.Hosting`
  - `Python.Runtime`
  - Optional (for testing):
    - `xunit`
    - `xunit.runner.visualstudio`

### Usage

1. **Build**:
   ```
   dotnet build
   ```

2. **Run**:
   - Normal: `dotnet run -- <path_to_program.as>`
   - Debug:  `dotnet run -- --debug <path_to_program.as>`

   In debug mode, use configured keys to step through program execution.

### Configuration

Handled via `config.yaml` in the root:

- `stepKey`: Step one instruction (default: F10).
- `stepOverKey`: Step over calls (default: F9).
- `doStepOverNOPsAfterRET`: Execute NOPs after RET in step-over (default: true).

## Python Assembler

The Python assembler is a standalone, command-line tool that compiles assembly source files (`.as`) into machine code as human-readable binary text files (`.txt`). It performs a complete two-pass assembly process with detailed error reporting, making it easy to write, debug, and experiment with programs for the custom 8-bit CPU.

### How It Works
1. **Tokenization** – Breaks the source into tokens (mnemonics, registers, numbers, labels, comments, etc.) while handling decimal, hexadecimal (`0x…`), and binary (`0b…`) literals.
2. **Parsing** – Builds a structured program representation, recognizing labels (e.g. `.mylabel:`) and instructions with their operands.
3. **Validation** – Checks everything against the ISA defined in `instructions.json`: valid mnemonics, correct operand count, types, ranges, register numbers (R0-R7), address bounds (0-1023), and program size (max 1024 instructions).
4. **Code Generation** – Resolves labels on the first pass, then 16-bit instruction templates from `instructions.json` are populated to produce binary machine code. Before insertion, operands undergo any required transformations (`neq` for negation, `div2` for halving, `dec` for decrement) as defined by the ISA.

### Features
- **Labels** – Define with `.mylabel:` (must start with a `.`) and use anywhere an address is expected (e.g., `JMP .mylabel`).
- **Comments** – Start with `;` (full line or inline).
- **Flexible literals** – Decimal (42), hex (`0x2A`), binary (`0b101010`), positive or negative.
- **Clear error messages** – Include exact line and column numbers for syntax errors, invalid instructions, out-of-range values, duplicate/undefined labels, etc.
- **Extensible ISA** – Add or modify instructions simply by editing `instructions.json` (no Python changes required).
- **Standalone** – Works independently of the emulator.
- **Embedded in emulator** – The C# emulator uses parts of this assembler (via `Python.Runtime`) to load `.as` files directly, without needing a separate binary file.

### Requirements
- **Python**: >=3.12 (standard library only; no external dependencies required, `pytest` for testing).

### Usage
Run:
```
python assembler.py program.as          # outputs program.txt
python assembler.py program.as -o out.txt
python assembler.py program.as -v       # verbose debugging output
```
- Input files must have the `.as` extension.
- Output is a `.txt` file with one 16-bit instruction per line, formatted as two 8-bit groups separated by a space (e.g., `00000001 00101010`).


## Programs

This section provides examples of assembly programs written for the custom 8-bit CPU. Each program demonstrates different aspects of the ISA, such as basic I/O, random number generation, pixel manipulation, and algorithmic computation. The programs are provided as `.as` files and can be run directly in the emulator (e.g., `dotnet run HelloWorld.as` or in debug mode with `--debug`). Outputs may include console text or visual rendering on the SFML pixel display.

### HelloWorld.as
A simple program that prints "Hello World!" followed by a newline to the console using the console output port (port 32). It loads ASCII values into a register and posts them sequentially.

**Expected Output:**  
Hello World!

![Hello World program with `--debug` flag](assets/HelloWorldDebug1.png)

### RandomImage.as
This program generates a 128x128 random color image by fetching random bytes from the RNG device (port 4) and setting RGB pixels on the display (ports 11-15). It loops over each pixel, assigning random colors and triggering updates.

**Expected Output:** A static 128x128 window displaying a random noise pattern in full color.

![Random Image program window after run](assets/RandomImage1.png)

### GOL.as
An implementation of Conway's Game Of Life on a 32x32 grid, rendered on the pixel display (ports 11-15). It uses RAM to store the grid state, counts neighbors for each cell, updates the grid according to Game of Life rules, and redraws the display. Includes a simple delay using the timer device (ports 5-8).


**Expected Output:** An animated 128x128 window showing the evolution of a 32x32 Game of Life grid, with cells scaled to 4x4 pixels (white for alive, black for dead). The initial pattern evolves over generations with a short delay between updates.

![Game Of Life gif of window during execution](assets/GOLgif1.gif)