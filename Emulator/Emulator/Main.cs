using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SDL2;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Emulator.Tests")]

namespace Emulator
{
    internal enum Mode
    {
        Normal,
        Debug
    }

    public static class Global
    {
        public static IServiceProvider? Services { get; set; }

        // Helper to get any service from anywhere
        public static T GetService<T>() where T : notnull
        {
            if (Services == null)
                throw new InvalidOperationException($"Service {typeof(T)} not registered.");
            return Services.GetRequiredService<T>();
        }
    }

    internal static class App
    {
        private static void Main(string[] args)
        {
            (string programPath, Mode mode) = ParseArguments(args);

            Program program = ProgramLoader.LoadProgram(programPath);

            CPU cpu = InitializeCPU(program, out SDLRenderer SDLrenderer);

            SetupServices(mode);

            if (mode == Mode.Normal)
            {
                RunNormal(cpu, SDLrenderer);
            }
            else
            {
                RunDebug(cpu, SDLrenderer);
            }
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
        /// <param name="mode">The execution mode.</param>
        /// <param name="SDLrenderer">The initialized renderer.</param>
        /// <returns>The constructed CPU.</returns>
        private static CPU InitializeCPU(Program program, out SDLRenderer SDLrenderer)
        {
            ICPUDirector director = new CPUBuildingDirector();

            SDLrenderer = new SDLRenderer();

            CPU cpu = director.Construct(program, SDLrenderer);

            return cpu;
    }

        /// <summary>
        /// Sets up the dependency injection services.
        /// </summary>
        /// <param name="renderer">The renderer to register.</param>
        private static void SetupServices(Mode mode)
        {
            IRenderer renderer = mode switch
            {
                Mode.Normal => new Renderer(),
                Mode.Debug => new DebugRenderer(),
                _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
            };

            var builder = Host.CreateApplicationBuilder();
            builder.Services.AddSingleton(renderer);
            var host = builder.Build();
            Global.Services = host.Services;
        }

        /// <summary>
        /// Runs the emulator in normal mode.
        /// </summary>
        /// <param name="cpu">The CPU instance.</param>
        /// <param name="renderer">The renderer instance.</param>
        private static void RunNormal(CPU cpu, SDLRenderer SDLrenderer)
        {
            cpu.Run();

            AfterRun(SDLrenderer);
        }

        /// <summary>
        /// Runs the emulator in debug mode with step-by-step execution.
        /// </summary>
        /// <param name="cpu">The CPU instance.</param>
        /// <param name="renderer">The renderer instance.</param>
        private static void RunDebug(CPU cpu, SDLRenderer SDLrenderer)
        {
            Global.GetService<IRenderer>().Render(cpu.Context);

            while (!cpu.Context.Halted)
            {
                var key = Console.ReadKey(true);

                if (key.Key == ConsoleKey.F10)
                {
                    cpu.Step();
                    Global.GetService<IRenderer>().Render(cpu.Context);
                    if (SDLrenderer.IsOpen)
                        SDLrenderer.RenderIfNeeded();
                }
                else if (key.Key == ConsoleKey.Escape)
                {
                    break;
                }
            }

            AfterRun(SDLrenderer);
        }

        private static void AfterRun(SDLRenderer SDLrenderer)
        {
            if (!SDLrenderer.IsOpen)
                return;

            do
            {
                SDLrenderer.RenderIfNeeded();
                SDL.SDL_Delay(20);  // Small delay (20ms) to reduce CPU usage while polling events frequently
            } while (SDLrenderer.IsOpen);
        }
    }
}










