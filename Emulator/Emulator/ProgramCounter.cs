namespace Emulator
{
    /// <summary>
    /// Emulates program counter and call stack behavior.
    /// </summary>
    internal sealed class ProgramCounter (ushort address = 0)
    {
        private ushort _programCounter = address;

        private readonly Stack<ushort> _callStack = new();

        public ushort Value
        {
            get => _programCounter;
        }

        public void Increment()
        {
            _programCounter++;

            if (_programCounter >= Architecture.MAX_PROGRAM_SIZE)
                throw new InvalidOperationException("Program counter overflow.");
        }

        public void SetBRH(ushort address)
        {
            if (address >= Architecture.MAX_PROGRAM_SIZE)
                throw new ArgumentOutOfRangeException(nameof(address), "Address is out of bounds.");

            _programCounter = address;
        }

        public void PushCAL(ushort address)
        {
            if (address >= Architecture.MAX_PROGRAM_SIZE)
                throw new ArgumentOutOfRangeException(nameof(address), "Address is out of bounds.");
            if (_programCounter + 1 >= Architecture.MAX_PROGRAM_SIZE)
                throw new InvalidOperationException("Can't push invalid adress onto stack.");

            _callStack.Push((ushort)(_programCounter + 1));
            _programCounter = address;
        }

        public void PopRET()
        {
            if (_callStack.Count == 0) throw new InvalidOperationException("Call stack is empty.");
            _programCounter = _callStack.Pop();
        }
    }
}
