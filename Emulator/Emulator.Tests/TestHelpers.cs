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
    }
}
