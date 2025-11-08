using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulator
{
    /// <summary>
    /// A simple output device that prints the stored byte as a character to the command line.
    /// </summary>
    internal sealed class ConsoleOutputDevice : IOPort
    {
        public void PortStore(byte value)
        {
            Console.Write((char)value);
        }

        public byte PortLoad()
        {
            return 0; // Loading from this device is not supported; return 0 as a default.
        }
    }
}
