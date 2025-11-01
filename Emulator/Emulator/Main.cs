using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Emulator.Tests")]


namespace Emulator
{
    internal sealed class MainClass
    {
        private static void Main(string[] args)
        {
            if (args.Length == 0)
                throw new ArgumentException("No program file specified.");

            string programPath = args[0];

            Program program = ProgramLoader.LoadProgram(programPath);

            CPU cpu = new CPU(program);

            cpu.Context.Ports.TryRegisterPort(32, new ConsoleOutputDevice());

            cpu.Run();
        }
    }
}














