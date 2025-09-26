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
        public const int MemoryCapacity = 256;

        private byte[] _backing;

        public RAM()
        {
            _backing = new byte[MemoryCapacity];
        }

        public byte this[byte index]
        {
            get
            {
                if (index >= _backing.Length) throw new ArgumentOutOfRangeException(nameof(index));
                return _backing[index];
            }
            set
            {
                if (index >= _backing.Length) throw new ArgumentOutOfRangeException(nameof(index));
                _backing[index] = value;
            }
        }
    }
}
