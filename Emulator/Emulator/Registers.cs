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
    }
}
