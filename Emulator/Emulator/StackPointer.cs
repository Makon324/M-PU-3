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

        public void Increment(byte frameSize = 1)
        {
            if (_stackPointer + frameSize > byte.MaxValue)
                throw new InvalidOperationException("Stack pointer overflow.");

            _stackPointer += frameSize;
        }

        public void Decrement(byte frameSize)
        {
            if (_stackPointer < frameSize)
                throw new InvalidOperationException("Stack pointer underflow.");

            _stackPointer -= frameSize;
        }
    }
}
