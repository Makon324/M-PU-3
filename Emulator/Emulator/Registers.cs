using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulator
{
    /// <summary>
    /// Represents a collection of CPU registers where register 0 ignores all writea and always reads 0.
    /// </summary>
    internal sealed class RegisterCollection
    {
        public const int RegisterCount = 8;

        private byte[] _backing;

        public RegisterCollection()
        {
            _backing = new byte[RegisterCount];
        }

        public byte this[byte index]
        {
            get
            {
                if (index >= _backing.Length) throw new ArgumentOutOfRangeException(nameof(index));
                return index == 0 ? (byte)0 : _backing[index];
            }
            set
            {
                if (index >= _backing.Length) throw new ArgumentOutOfRangeException(nameof(index));
                if (index == 0) return; // ignore writes to R0
                _backing[index] = value;
            }
        }
    }
}
