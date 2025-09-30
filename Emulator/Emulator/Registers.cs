using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulator
{
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

        public byte this[byte index]
        {
            get
            {
                if (index >= Architecture.REGISTER_COUNT) 
                    throw new ArgumentOutOfRangeException(nameof(index),
                        $"Index {index} is beyond register count {Architecture.REGISTER_COUNT}");

                return index == 0 ? (byte)0 : _backing[index];
            }
            set
            {
                if (index >= _backing.Length) 
                    throw new ArgumentOutOfRangeException(nameof(index),
                        $"Index {index} is beyond register count {Architecture.REGISTER_COUNT}");


                if (index == 0) return; // ignore writes to R0
                _backing[index] = value;
            }
        }
    }
}
