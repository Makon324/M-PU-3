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

    internal sealed class PortCollection
    {
        private readonly IOPort?[] _ports = new IOPort[Architecture.IO_PORT_COUNT];

        /// <summary>
        /// Register a port device to a specific port number.
        /// </summary>
        /// <returns>True on successful port register, False when registering fails.</returns>
        public bool TryRegisterPort(byte portNumber, IOPort device)
        {
            if (_ports[portNumber] != null) return false;
            _ports[portNumber] = device;
            return true;
        }
        public bool TryRegisterPort(Index portNumber, IOPort device)
        {
            return TryRegisterPort((byte)portNumber.GetOffset(Architecture.IO_PORT_COUNT), device);
        }

        /// <summary>
        /// Gets or sets the device registered to the specified port number.
        /// </summary>
        public IOPort? this[byte index]
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
        public IOPort? this[Index index]
        {
            get
            {
                byte actualIndex = (byte)index.GetOffset(Architecture.IO_PORT_COUNT);
                return _ports[actualIndex];
            }
            set
            {
                byte actualIndex = (byte)index.GetOffset(Architecture.IO_PORT_COUNT);
                _ports[actualIndex] = value;
            }
        }

        /// <summary>
        /// Gets a range of ports as an array.
        /// </summary>
        public IOPort?[] this[Range range]
        {
            get
            {
                var (offset, length) = range.GetOffsetAndLength(_ports.Length);
                return _ports[offset..(offset + length)];
            }
        }

        /// <summary>
        /// Gets all ports as an array.
        /// </summary>
        public IOPort?[] GetAllPorts()
        {
            return (IOPort?[])_ports.Clone();
        }
    }


    /// <summary>
    /// Basic Device mainly for showcase and testing purposes.
    /// </summary>
    internal sealed class BasicDevice : IOPort
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
