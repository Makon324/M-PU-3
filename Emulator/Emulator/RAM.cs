namespace Emulator
{
    /// <summary>
    /// Represents RAM memory for the emulator.
    /// </summary>
    internal sealed class RAM
    {
        private readonly byte[] _backing;

        public RAM()
        {
            _backing = new byte[Architecture.RAM_SIZE];
        }


        /// <summary>
        /// Gets or sets the byte at the specified index.
        /// </summary>
        public byte this[byte index]
        {
            get
            {
                return _backing[index];
            }
            set
            {
                _backing[index] = value;
            }
        }
        public byte this[Index index]
        {
            get
            {
                int actualIndex = index.GetOffset(Architecture.RAM_SIZE);
                if(actualIndex >= Architecture.RAM_SIZE || actualIndex < 0)
                    throw new ArgumentOutOfRangeException(nameof(index), 
                        $"Index {index} maps to {actualIndex}, which is outside valid range 0..{Architecture.RAM_SIZE - 1}.");

                return this[(byte)actualIndex];
            }
            set
            {
                int actualIndex = index.GetOffset(Architecture.RAM_SIZE);
                if (actualIndex >= Architecture.RAM_SIZE || actualIndex < 0)
                    throw new ArgumentOutOfRangeException(nameof(index),
                        $"Index {index} maps to {actualIndex}, which is outside valid range 0..{Architecture.RAM_SIZE - 1}.");

                this[(byte)actualIndex] = value;
            }
        }

        /// <summary>
        /// Gets a range of memory as an array.
        /// </summary>
        public byte[] this[Range range]
        {
            get
            {
                var (offset, length) = range.GetOffsetAndLength(_backing.Length);
                return _backing[offset..(offset + length)];
            }
        }

        /// <summary>
        /// Gets all memory as an array.
        /// </summary>
        public byte[] GetAllMemory()
        {
            return (byte[])_backing.Clone();
        }
    }
}
