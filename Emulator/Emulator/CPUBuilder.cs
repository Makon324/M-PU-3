namespace Emulator
{
    /// <summary>
    /// Builder class for constructing a CPU instance with registered devices.
    /// </summary>
    internal sealed class CPUBuilder
    {
        private readonly CPU _cpu;

        /// <summary>
        /// Initializes a new instance of the <see cref="CPUBuilder"/> class.
        /// </summary>
        /// <param name="program">The program to associate with the CPU.</param>
        public CPUBuilder(Program program)
        {
            _cpu = new CPU(program);
        }

        #region Register Methods (one per device type)

        public CPUBuilder RegisterMultiplier(byte basePort)
        {
            CheckPortAvailability(basePort, 2);  // Needs 2 ports
            var multiplier = new Multiplier(_cpu.Context, basePort);
            return this;  // For chaining
        }

        public CPUBuilder RegisterDivider(byte basePort)
        {
            CheckPortAvailability(basePort, 2);  // Needs 2 ports
            var divider = new Divider(_cpu.Context, basePort);
            return this;
        }

        public CPUBuilder RegisterRNG(byte portNumber)
        {
            CheckPortAvailability(portNumber, 1);
            var rng = new RNG();

            if (!_cpu.Context.Ports.TryRegisterPort(portNumber, rng))
            {
                throw new InvalidOperationException($"Failed to register RNG at port {portNumber}.");
            }

            return this;
        }

        public CPUBuilder RegisterConsoleOutputDevice(byte portNumber)
        {
            CheckPortAvailability(portNumber, 1);
            var consoleOutputDevice = new ConsoleOutputDevice();

            if (!_cpu.Context.Ports.TryRegisterPort(portNumber, consoleOutputDevice))
            {
                throw new InvalidOperationException($"Failed to register ConsoleOutputDevice at port {portNumber}.");
            }

            return this;
        }

        public CPUBuilder RegisterTimer(byte basePort)
        {
            CheckPortAvailability(basePort, 4);  // Needs 4 ports
            var timer = new Timer(_cpu.Context, basePort);
            return this;
        }

        public CPUBuilder RegisterPixelDisplay(byte basePort)
        {
            CheckPortAvailability(basePort, 5);  // RGB + X/Y = 5 ports
            var display = new PixelDisplay(_cpu.Context, basePort);
            return this;
        }

        #endregion

        public CPU Build()
        {
            return _cpu;
        }

        /// <summary>
        /// Helper method for checking if a range of ports is available.
        /// </summary>
        private void CheckPortAvailability(byte startPort, int count)
        {
            if (startPort > Architecture.IO_PORT_COUNT - count)
                throw new ArgumentOutOfRangeException(nameof(startPort),
                    $"Start port must be <= {Architecture.IO_PORT_COUNT - count} to allow {count} consecutive ports.");

            for (int i = 0; i < count; i++)
            {
                byte portNumber = (byte)(startPort + i);
                if (_cpu.Context.Ports[portNumber] != null)
                    throw new InvalidOperationException($"Port {portNumber} is already in use.");
            }
        }
    }

    /// <summary>
    /// Defines an interface for directing the construction of a CPU.
    /// </summary>
    internal interface ICPUDirector
    {
        CPU Construct(Program program);
    }

    /// <summary>
    /// Director class for building a CPU with a predefined set of devices registered at specific ports.
    /// </summary>
    internal sealed class CPUBuildingDirector : ICPUDirector
    {
        /// <summary>
        /// Constructs a CPU with standard devices: multiplier (ports 0-1), divider (ports 2-3),
        /// RNG (port 4), timer (ports 5-8), and pixel display (ports 11-15), console output (port 32).
        /// </summary>
        /// <param name="program">The program to associate with the CPU.</param>
        public CPU Construct(Program program)
        {
            return new CPUBuilder(program)
                .RegisterMultiplier(0)                // Ports 0-1
                .RegisterDivider(2)                   // Ports 2-3
                .RegisterRNG(4)                       // Port 4                
                .RegisterTimer(5)                     // Ports 5-8
                .RegisterPixelDisplay(11)             // Ports 11-15
                .RegisterConsoleOutputDevice(32)      // Port 32
                .Build();
        }
    }
}
