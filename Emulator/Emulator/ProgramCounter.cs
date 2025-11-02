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

        /// <summary>
        /// Increments the program counter by 1.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if the program counter would overflow beyond the maximum program size.</exception>
        public void Increment()
        {
            _programCounter++;

            if (_programCounter >= Architecture.MAX_PROGRAM_SIZE)
                throw new InvalidOperationException("Program counter overflow.");
        }

        /// <summary>
        /// Sets the program counter to the specified address, typically used for branch operations.
        /// </summary>
        /// <param name="address">The target address to set the program counter to.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the address is out of bounds.</exception>
        public void SetBRH(ushort address)
        {
            if (address >= Architecture.MAX_PROGRAM_SIZE)
                throw new ArgumentOutOfRangeException(nameof(address), "Address is out of bounds.");

            _programCounter = address;
        }

        /// <summary>
        /// Pushes the next instruction address onto the call stack and sets the program counter to the specified address, typically used for call operations.
        /// </summary>
        /// <param name="address">The target address to set the program counter to.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if the address is out of bounds.</exception>
        /// <exception cref="InvalidOperationException">Thrown if pushing would result in an invalid address on the stack.</exception>
        public void PushCAL(ushort address)
        {
            if (address >= Architecture.MAX_PROGRAM_SIZE)
                throw new ArgumentOutOfRangeException(nameof(address), "Address is out of bounds.");
            if (_programCounter + 1 >= Architecture.MAX_PROGRAM_SIZE)
                throw new InvalidOperationException("Can't push invalid address onto stack.");

            _callStack.Push((ushort)(_programCounter + 1));
            _programCounter = address;
        }

        /// <summary>
        /// Pops the top address from the call stack and sets the program counter to it, typically used for return operations.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if the call stack is empty.</exception>
        public void PopRET()
        {
            if (_callStack.Count == 0) throw new InvalidOperationException("Call stack is empty.");
            _programCounter = _callStack.Pop();
        }
    }
}
