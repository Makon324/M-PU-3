using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulator
{
    /// <summary>
    /// Interface for port-mapped I/O devices.
    /// </summary>
    internal interface IOPort
    {
        void PortStore(byte value);

        byte PortLoad();
    }

    internal sealed class PortCollection()
    {
        private readonly Dictionary<byte, IOPort> _ports = new();

        /// <summary>
        /// Register a port device to a specific port number.
        /// </summary>
        /// <returns>True on succesful port register, False when registering fails.</returns>
        public bool RegisterPort(byte portNumber, IOPort device)
        {
            if (_ports.ContainsKey(portNumber))
            {
                return false;
            }
            _ports[portNumber] = device;
            return true;
        }

        public IOPort this[byte index]
        {
            get
            {
                return _ports[index];
            }
            set
            {
                _ports[index] = value;
            }
        }
    }


    /// <summary>
    /// Basic Device mainly for schowcase and testing purposes.
    /// </summary>
    internal sealed class BasicDevice() : IOPort
    {
        private byte _value;

        public void PortStore(byte value)
        {
            _value = value;
        }

        public byte PortLoad()
        {
            return _value;
        }
    }





}
