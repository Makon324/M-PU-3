using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Emulator.Tests")]

namespace Emulator
{
    internal enum Mode
    {
        Normal,
        Debug
    }

    internal static class App
    {
        private static void Main(string[] args)
        {
            (string programPath, Mode mode) = ParseArguments(args);

            Program program = ProgramLoader.LoadProgram(programPath);

            CPU cpu = InitializeCPU(program);

            SetupServices(mode);

            if (mode == Mode.Normal)
            {
                RunNormal(cpu);
            }
            else
            {
                RunDebug(cpu);
            }

            AfterRun();
        }

        /// <summary>
        /// Parses command-line arguments to extract the program path and execution mode.
        /// </summary>
        /// <param name="args">The command-line arguments.</param>
        /// <returns>A tuple containing the program path and the mode.</returns>
        private static (string programPath, Mode mode) ParseArguments(string[] args)
        {
            if (args.Length < 1 || args.Length > 2)
            {
                Usage();
            }            

            string? programPath = null;
            bool debug = false;

            foreach (var arg in args)
            {
                if (arg.Equals("--debug", StringComparison.OrdinalIgnoreCase))
                {
                    debug = true;
                }
                else if (programPath == null)
                {
                    programPath = arg;
                }
                else
                {
                    Usage();
                }
            }

            if (programPath == null)
            {
                Usage();
            }

            return (programPath!, debug ? Mode.Debug : Mode.Normal);
        }

        private static void Usage()
        {
            Console.WriteLine("No program file specified.");
            Environment.Exit(1);
        }

        /// <summary>
        /// Initializes the CPU and renderer based on the execution mode.
        /// </summary>
        /// <param name="program">The loaded program.</param>
        /// <returns>The constructed CPU.</returns>
        private static CPU InitializeCPU(Program program)
        {
            ICPUDirector director = new CPUBuildingDirector();

            CPU cpu = director.Construct(program);

            return cpu;
    }

        /// <summary>
        /// Sets up the dependency injection services.
        /// </summary>
        private static void SetupServices(Mode mode)
        {
            IRenderer renderer = mode switch
            {
                Mode.Normal => new Renderer(),
                Mode.Debug => new DebugRenderer(),
                _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
            };

            YAMLConfig config = new YAMLConfig();

            var builder = Host.CreateApplicationBuilder();
            builder.Services.AddSingleton(renderer);
            builder.Services.AddSingleton(config);
            var host = builder.Build();
            Global.Services = host.Services;
        }

        /// <summary>
        /// Runs the emulator in normal mode.
        /// </summary>
        /// <param name="cpu">The CPU instance.</param>
        private static void RunNormal(CPU cpu)
        {
            cpu.Run();
        }

        /// <summary>
        /// Runs the emulator in debug mode with step-by-step execution.
        /// </summary>
        /// <param name="cpu">The CPU instance.</param>
        private static void RunDebug(CPU cpu)
        {
            Console.Clear();

            Global.GetService<IRenderer>().Render(cpu);

            while (!cpu.Context.Halted)
            {
                var key = Console.ReadKey(true);

                var config = Global.GetService<YAMLConfig>();

                if (key.Key == config.stepKey)
                {
                    cpu.Step();                    
                }
                else if (key.Key == config.stepOverKey)
                {
                    cpu.StepOverCall();
                }
                else if (key.Key == ConsoleKey.Escape)
                {
                    break;
                }

                Global.GetService<IRenderer>().Render(cpu);
            }            
        }

        private static void AfterRun()
        {
            Console.ReadKey();  // Wait for any key press before closing
        }
    }
}










