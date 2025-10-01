namespace Emulator
{
    internal sealed class CPUContext
    {
        public RegisterCollection Registers { get; }
        public RAM RAM { get; }
        public ProgramCounter ProgramCounter { get; }
        public StackPointer StackPointer { get; }
        public bool ZeroFlag { get; set; }
        public bool CarryFlag { get; set; }
        public bool Halted { get; set; }

        public CPUContext()
        {
            Registers = new RegisterCollection();
            RAM = new RAM();
            ProgramCounter = new ProgramCounter(0);
            StackPointer = new StackPointer(0);
            ZeroFlag = false;
            CarryFlag = false;
            Halted = false;
        }
    }
}
