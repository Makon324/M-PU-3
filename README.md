\# Custom 8-Bit CPU Project



\## Overview



This project involves the design and implementation of a custom 8-bit CPU architecture, complete with an assembler written in Python and an emulator developed in C#. The goal is to create a simple yet functional 8-bit processor from scratch, allowing for experimentation with low-level computing concepts, assembly language programming, and emulation techniques.



\## Features



\- \*\*Custom 8-Bit CPU Architecture\*\*: A bespoke instruction set with support for arithmetic, logic, control flow, and memory operations.

\- \*\*Python Assembler\*\*: A lightweight assembler that parses assembly source files (.asm) and generates txt files with 0 and 1s.

\- \*\*C# Emulator\*\*: A full-featured emulator with debug mode, display and console write devices.





\## Emulator (C#)



The emulator is a console application (`Emulator.exe`) built with .NET, simulating the CPU execution:

\- Loads binary files into memory.

\- Executes instructions cycle-by-cycle.

\- Provides interactive debugging: step-through, breakpoints, register views.

\- Simulates basic I/O (e.g., console output for print operations).

\- Logs execution traces for analysis.



\### Requirements

\- .NET SDK/Runtime (version 6.0 or later)

\#### NuGet Packages:
- SFML.Net
- YamlDotNet

\- Microsoft.Extensions.Hosting
- Python.Runtime



Options:

\- `--debug`: Enable debug mode (step through assembly code and see CPU state).





