namespace Emulator.Tests
{
    internal static class TestHelpers
    {
        public static Program CreateProgram(params Instruction[] instructions)
        {
            return new Program(instructions);
        }

        public static CPUContext CreateContext()
        {
            return new CPUContext();
        }

        /// <summary>
        /// Returns a random register except R0.
        /// </summary>
        public static Register GetRandomRegister()
        {
            return (Register)Random.Next(1, Architecture.REGISTER_COUNT);
        }

        /// <summary>
        /// Returns two distinct random registers except R0 as a tuple.
        /// </summary>
        public static (Register regA, Register regB) GetTwoRandomDistinctRegisters()
        {
            int indexA = Random.Next(1, Architecture.REGISTER_COUNT);
            int indexB;

            do
            {
                indexB = Random.Next(1, Architecture.REGISTER_COUNT);
            } while (indexB == indexA);

            return ((Register)indexA, (Register)indexB);
        }

        private static readonly Random Random = new Random();
    }
}
