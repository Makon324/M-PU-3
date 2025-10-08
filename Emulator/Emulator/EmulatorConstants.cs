﻿namespace Emulator
{
    internal static class Architecture
    {
        public const int REGISTER_COUNT = 8;
        public const int RAM_SIZE = 256;
        public const int IO_PORT_COUNT = 256;
        public const int MAX_PROGRAM_SIZE = 1024;
        public const int INSTRUCTION_PIPELINE_SIZE = 3;

        public const int BRANCH_IF_ZERO_CODE = 0x00;      // ZeroFlag == 1
        public const int BRANCH_IF_NOT_ZERO_CODE = 0x01;  // ZeroFlag == 0
        public const int BRANCH_IF_CARRY_CODE = 0x02;     // CarryFlag == 1
        public const int BRANCH_IF_NOT_CARRY_CODE = 0x03; // CarryFlag == 0
    }

    /// <summary>
    /// Static class containing constant paths used in the emulator, relative to folder with .git.
    /// </summary>
    internal static class Paths
    {
        public const string INSTRUCTIONS_FILE = "Instructions.json";
        public const string PYTHON_ASSEMBLER = "Assembler";
        public const string PYTHON_TOKENIZER = PYTHON_ASSEMBLER + "/Tokenizer.py";
        public const string PYTHON_PARSER = PYTHON_ASSEMBLER + "/Parser.py";
        public const string PYTHON_VALIDATOR = PYTHON_ASSEMBLER + "/Validator.py";
    }
}
