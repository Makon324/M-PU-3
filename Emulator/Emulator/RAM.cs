using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}
