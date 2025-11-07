using System.Diagnostics;

namespace Emulator
{
    /// <summary>
    /// Represents a multiplier component that performs multiplication operations using two I/O ports.
    /// </summary>
    /// <remarks>The <see cref="Multiplier"/> class registers two ports, PortA and PortB. 
    /// These ports are used to perform multiplication operations on byte values. 
    /// The result of the multiplication is split across the two ports, with PortA providing the least significant byte
    /// and PortB providing the highest significant byte of the result.</remarks>
    internal class Multiplier
    {
        private readonly PortA _portA;
        private readonly PortB _portB;
        private byte _inputA;
        private byte _inputB;

        public Multiplier(CPUContext context, byte basePort)
        {
            if (basePort >= Architecture.IO_PORT_COUNT - 1)
                throw new ArgumentOutOfRangeException(nameof(basePort),
                    $"Base port must be <= {Architecture.IO_PORT_COUNT - 2} to allow two consecutive ports.");

            _portA = new PortA(this);
            _portB = new PortB(this);

            if (!(
                context.Ports.TryRegisterPort(basePort, _portA) &&
                context.Ports.TryRegisterPort((byte)(basePort + 1), _portB)
            ))
            {
                throw new InvalidOperationException($"Failed to register multiplier ports at {basePort} and {basePort + 1}.");
            }

        }

        private ushort Product => (ushort)(_inputA * _inputB);

        private sealed class PortA(Multiplier mul) : IOPort
        {
            private readonly Multiplier _mul = mul;

            public void PortStore(byte value) => _mul._inputA = value;
            public byte PortLoad() => (byte)(_mul.Product);
        }

        private sealed class PortB(Multiplier mul) : IOPort
        {
            private readonly Multiplier _mul = mul;

            public void PortStore(byte value) => _mul._inputB = value;
            public byte PortLoad() => (byte)(_mul.Product >> 8);
        }
    }

    /// <summary>
    /// Represents a divider component that performs division operations using two I/O ports.
    /// </summary>
    /// <remarks>The <see cref="Divider"/> class registers two ports, PortA and PortB. 
    /// These ports are used to perform division operations on byte values. 
    /// The result of the division is split across the two ports, with PortA providing the quotient
    /// and PortB providing the remainder. Division by zero returns what real logic gates would have returned.</remarks>
    internal class Divider
    {
        private readonly PortA _portA;
        private readonly PortB _portB;
        private byte _inputA; // Divisor
        private byte _inputB; // Dividend

        public Divider(CPUContext context, byte basePort)
        {
            if (basePort >= Architecture.IO_PORT_COUNT - 1)
                throw new ArgumentOutOfRangeException(nameof(basePort),
                    $"Base port must be <= {Architecture.IO_PORT_COUNT - 2} to allow two consecutive ports.");

            _portA = new PortA(this);
            _portB = new PortB(this);

            if (!(
                context.Ports.TryRegisterPort(basePort, _portA) &&
                context.Ports.TryRegisterPort((byte)(basePort + 1), _portB)
            ))
            {
                throw new InvalidOperationException($"Failed to register divider ports at {basePort} and {basePort + 1}.");
            }
        }

        private byte Quotient => _inputA == 0 ? (byte)0xFF : (byte)(_inputB / _inputA);
        private byte Mod => _inputA == 0 ? (byte)_inputB : (byte)(_inputB % _inputA);

        private sealed class PortA(Divider div) : IOPort
        {
            private readonly Divider _div = div;

            public void PortStore(byte value) => _div._inputA = value;
            public byte PortLoad() => _div.Quotient;
        }

        private sealed class PortB(Divider div) : IOPort
        {
            private readonly Divider _div = div;

            public void PortStore(byte value) => _div._inputB = value;
            public byte PortLoad() => _div.Mod;
        }
    }

    /// <summary>
    /// Represents a random number generator (RNG) I/O port device.
    /// </summary>
    internal sealed class RNG : IOPort
    {
        private readonly Random _random = new Random();
        public void PortStore(byte value)
        {
            // RNG does not support storing values.
        }

        public byte PortLoad()
        {
            return (byte)_random.Next(0, 256);
        }
    }

    /// <summary>
    /// Represents a timer component that stores the elapsed time in milliseconds since start, 
    /// in little-endian format across four consecutive I/O ports.
    /// </summary>
    /// <remarks>The <see cref="Timer"/> class registers four ports. 
    /// These ports provide the bytes of the 32-bit elapsed milliseconds in little-endian order:
    /// base port (LSB), base+1, base+2, base+3 (MSB). 
    /// The timer starts when the object is created. Ports are read-only; storing values has no effect.</remarks>
    internal class Timer
    {
        private readonly TimerPort[] _ports = new TimerPort[4];
        private readonly Stopwatch _stopwatch;

        public Timer(CPUContext context, byte basePort)
        {
            if (basePort >= Architecture.IO_PORT_COUNT - 3)
                throw new ArgumentOutOfRangeException(nameof(basePort),
                    $"Base port must be <= {Architecture.IO_PORT_COUNT - 4} to allow four consecutive ports.");

            _stopwatch = Stopwatch.StartNew();

            bool registered = true;
            for (int i = 0; i < 4; i++)
            {
                _ports[i] = new TimerPort(this, i);
                registered &= context.Ports.TryRegisterPort((byte)(basePort + i), _ports[i]);
            }

            if (!registered)
                throw new InvalidOperationException($"Failed to register timer ports at {basePort} to {basePort + 3}.");
        }

        private uint ElapsedMs => (uint)_stopwatch.ElapsedMilliseconds;

        private sealed class TimerPort(Timer timer, int portNumber) : IOPort
        {
            private readonly Timer _timer = timer;
            private readonly int _portNumber = portNumber;

            public void PortStore(byte value)
            {
                // Timer ports are read-only; storing has no effect.
            }

            public byte PortLoad() => (byte)((_timer.ElapsedMs >> _portNumber * 8) & 0xFF);
        }
    }



}
