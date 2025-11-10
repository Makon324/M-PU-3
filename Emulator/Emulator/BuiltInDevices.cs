using System.Diagnostics;

namespace Emulator
{
    /// <summary>
    /// Represents a multiplier component that performs multiplication operations using two consecutive I/O ports.
    /// </summary>
    /// <remarks>The <see cref="Multiplier"/> class registers two consecutive ports starting at the base port.
    /// These ports are used to perform multiplication operations on byte values.
    /// Writing to the base port sets the first factor, and writing to base+1 sets the second factor.
    /// The result of the multiplication is split across the two ports, with the base port providing the least significant byte
    /// and base+1 providing the most significant byte of the result.</remarks>
    internal sealed class Multiplier
    {
        private readonly MultiplierPort[] _ports = new MultiplierPort[2];
        private readonly byte[] _factors = new byte[2];

        public Multiplier(CPUContext context, byte basePort)
        {
            if (basePort >= Architecture.IO_PORT_COUNT - 1)
                throw new ArgumentOutOfRangeException(nameof(basePort),
                    $"Base port must be <= {Architecture.IO_PORT_COUNT - 2} to allow two consecutive ports.");

            bool registered = true;
            for (int i = 0; i < 2; i++)
            {
                _ports[i] = new MultiplierPort(this, i);
                registered &= context.Ports.TryRegisterPort((byte)(basePort + i), _ports[i]);
            }

            if (!registered)
            {
                throw new InvalidOperationException($"Failed to register multiplier ports at {basePort} and {basePort + 1}.");
            }
        }

        private ushort Product => (ushort)(_factors[0] * _factors[1]);

        private sealed class MultiplierPort(Multiplier mul, int portNumber) : IOPort
        {
            private readonly Multiplier _mul = mul;
            private readonly int _portNumber = portNumber;

            public void PortStore(byte value)
            {
                _mul._factors[_portNumber] = value;
            }

            public byte Value { get => (byte)(_mul.Product >> (_portNumber * 8)); }

            public byte PortLoad()
            {
                return Value;
            }
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
        private readonly PortA _portA;  // Divisor and Quotient port
        private readonly PortB _portB;  // Dividend and Modulus port
        private byte _divisor;
        private byte _dividend;

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

        private byte Quotient => _divisor == 0 ? (byte)0xFF : (byte)(_dividend / _divisor);

        private byte Mod => _divisor == 0 ? (byte)_dividend : (byte)(_dividend % _divisor);

        private sealed class PortA(Divider div) : IOPort
        {
            private readonly Divider _div = div;

            public void PortStore(byte value) => _div._divisor = value;

            public byte Value { get => _div.Quotient; }

            public byte PortLoad() => Value;
        }

        private sealed class PortB(Divider div) : IOPort
        {
            private readonly Divider _div = div;

            public void PortStore(byte value) => _div._dividend = value;

            public byte Value { get => _div.Mod; }

            public byte PortLoad() => Value;
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

        public byte Value { get => (byte)_random.Next(0, 256); }

        public byte PortLoad()
        {
            return Value;
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

            public byte Value => (byte)((_timer.ElapsedMs >> _portNumber * 8) & 0xFF);

            public byte PortLoad() => Value;
        }
    }



}
