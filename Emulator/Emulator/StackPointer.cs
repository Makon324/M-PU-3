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

        /// <summary>
        /// Increments the stack pointer by the specified frame size.
        /// </summary>
        /// <param name="frameSize"></param>
        /// <exception cref="InvalidOperationException">Thrown if incrementing would cause the stack pointer to overflow beyond the maximum byte value.</exception>
        public void Increment(byte frameSize = 1)
        {
            if (_stackPointer + frameSize > byte.MaxValue)
                throw new InvalidOperationException("Stack pointer overflow.");

            _stackPointer += frameSize;
        }

        /// <summary>
        /// Decrements the stack pointer by the specified frame size.
        /// </summary>
        /// <param name="frameSize"></param>
        /// <exception cref="InvalidOperationException">Thrown if incrementing would cause the stack pointer to overflow beyond the maximum byte value.</exception>
        public void Decrement(byte frameSize)
        {
            if (_stackPointer < frameSize)
                throw new InvalidOperationException("Stack pointer underflow.");

            _stackPointer -= frameSize;
        }
    }
}
