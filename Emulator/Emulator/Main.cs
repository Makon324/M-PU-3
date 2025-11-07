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

            ICPUDirector director = new CPUBuildingDirector();

            CPU cpu = director.Construct(program);

            cpu.Run();
        }
    }
}














