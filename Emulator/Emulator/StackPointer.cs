namespace Emulator
{
    internal sealed class StackPointer
    {
        private byte _stackPointer;

        public byte Value { get => _stackPointer; }

        public StackPointer(byte value = 0)
        {
            _stackPointer = value;
        }

        public void Increment()
        {
            if (_stackPointer == byte.MaxValue)
                throw new InvalidOperationException("Stack pointer overflow.");

            _stackPointer++;
        }

        // Decrement stack pointer by frame size (number of bytes to pop)
        public void Decrement(byte frameSize)
        {
            if (_stackPointer < frameSize)
                throw new InvalidOperationException("Stack pointer underflow.");

            _stackPointer -= frameSize;
        }
    }
}
