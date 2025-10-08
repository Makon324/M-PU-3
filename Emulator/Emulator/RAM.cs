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
                if (index >= _backing.Length)
                    throw new ArgumentOutOfRangeException(nameof(index),
                        $"Index {index} is beyond memory capacity {Architecture.RAM_SIZE}");

                return _backing[index];
            }
            set
            {
                if (index >= _backing.Length)
                    throw new ArgumentOutOfRangeException(nameof(index),
                        $"Index {index} is beyond memory capacity {Architecture.RAM_SIZE}");

                _backing[index] = value;
            }
        }
        public byte this[Index index]
        {
            get
            {
                byte actualIndex = (byte)index.GetOffset(Architecture.RAM_SIZE);
                return this[actualIndex];
            }
            set
            {
                byte actualIndex = (byte)index.GetOffset(Architecture.RAM_SIZE);
                this[actualIndex] = value;
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
