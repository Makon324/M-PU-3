namespace Emulator
{
    /// <summary>
    /// Represents program memory containing instructions for the emulator.
    /// </summary>
    internal sealed class Program
    {
        private readonly IReadOnlyList<Instruction> _instructions;

        public Program(IReadOnlyList<Instruction> instructions)
        {
            _instructions = instructions;
        }

        /// <summary>
        /// Gets the instruction at the specified index.
        /// </summary>
        public Instruction this[ushort index]
        {
            get
            {
                if (index >= _instructions.Count)
                    throw new ArgumentOutOfRangeException(nameof(index),
                        $"Index {index} is beyond program memory capacity {Architecture.MAX_PROGRAM_SIZE}");

                return _instructions[index];
            }
        }
        public Instruction this[Index index]
        {
            get
            {
                int actualIndex = index.GetOffset(Architecture.MAX_PROGRAM_SIZE);
                if (actualIndex > ushort.MaxValue)
                    throw new ArgumentOutOfRangeException(nameof(index), "Index exceeds ushort range");

                return this[(ushort)actualIndex];
            }
        }

        /// <summary>
        /// Gets a range of instructions as an array.
        /// </summary>
        public Instruction[] this[Range range]
        {
            get
            {
                var (offset, length) = range.GetOffsetAndLength(_instructions.Count);
                var result = new Instruction[length];

                for (int i = 0; i < length; i++)
                {
                    result[i] = _instructions[offset + i];
                }

                return result;
            }
        }

        /// <summary>
        /// Gets all instructions as a list.
        /// </summary>
        public IReadOnlyList<Instruction> GetAllInstructions()
        {
            return _instructions;
        }

        /// <summary>
        /// Gets the number of instructions in program memory.
        /// </summary>
        public int Length => _instructions.Count;
    }
}
