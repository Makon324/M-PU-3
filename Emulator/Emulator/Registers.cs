namespace Emulator
{
    /// <summary>
    /// Represents adresses of CPU registers.
    /// </summary>
    enum Register
    {
        R0 = 0,
        R1 = 1,
        R2 = 2,
        R3 = 3,
        R4 = 4,
        R5 = 5,
        R6 = 6,
        R7 = 7
    }

    /// <summary>
    /// Represents a collection of CPU registers where register 0 ignores all writes and always reads 0.
    /// </summary>
    internal sealed class RegisterCollection
    {
        private readonly byte[] _backing;

        public RegisterCollection()
        {
            _backing = new byte[Architecture.REGISTER_COUNT];
        }

        /// <summary>
        /// Gets or sets the value of the specified register by byte index (index 0 ignores writes and always reads 0).
        /// </summary>
        public byte this[Register reg]
        {            
            get
            {
                return reg == Register.R0 ? (byte)0 : _backing[(int)reg];
            }
            set
            {
                if (reg == Register.R0) return; // ignore writes to R0
                _backing[(int)reg] = value;
            }
        }
        public byte this[Index index]
        {
            get
            {
                byte actualIndex = (byte)index.GetOffset(Architecture.REGISTER_COUNT);
                return this[actualIndex];
            }
            set
            {
                byte actualIndex = (byte)index.GetOffset(Architecture.REGISTER_COUNT);
                this[actualIndex] = value;
            }
        }

        /// <summary>
        /// Gets a range of register values as an array (R0 behavior is preserved in the result).
        /// </summary>
        public byte[] this[Range range]
        {
            get
            {
                var (offset, length) = range.GetOffsetAndLength(Architecture.REGISTER_COUNT);
                byte[] result = new byte[length];

                for (int i = 0; i < length; i++)
                {
                    byte registerIndex = (byte)(offset + i);
                    result[i] = this[(Register)registerIndex];
                }

                return result;
            }
        }

        /// <summary>
        /// Gets all register values as an array (R0 behavior is preserved in the result).
        /// </summary>
        public byte[] GetAllRegisters()
        {
            byte[] result = new byte[Architecture.REGISTER_COUNT];
            for (int i = 0; i < Architecture.REGISTER_COUNT; i++)
            {
                result[i] = this[(Register)i];
            }
            return result;
        }
    }
}
