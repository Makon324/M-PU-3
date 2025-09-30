using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulator
{
    internal static class Architecture
    {
        public const int REGISTER_COUNT = 8;
        public const int RAM_SIZE = 256;
        public const int MAX_PROGRAM_SIZE = 1024;
        public const int INSTRUCTION_PIPELINE_SIZE = 3;

        public static readonly ImmutableHashSet<string> INSTRUCTIONS_THAT_FLUSH_PIPELINE = ImmutableHashSet.Create("JMP", "BRH", "CAL", "RET");
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
