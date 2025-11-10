using System;
using System.Text;

namespace Emulator
{
    internal interface IRenderer
    {
        public void Render(CPUContext context);
        
        public void WriteToConsole(string content);
    }

    internal sealed class Renderer : IRenderer
    {
        public void Render(CPUContext context)
        {
            // In normal mode, we just pass through without rendering anything.
        }

        public void WriteToConsole(string content)
        {
            Console.Write(content);
        }
    }

    /// <summary>
    /// Singleton MVC Renderer for displaying emulator state in console
    /// </summary>
    internal sealed class DebugRenderer : IRenderer
    {
        private const int WINDOW_WIDTH = 80;
        private const int REGISTERS_PER_ROW = 4;
        private const int MEMORY_WINDOW_SIZE = 32;
        private const int PROGRAM_VIEW_SIZE = 5;

        // Buffer with that is gonna be printed to console
        private string _consoleBuffer = string.Empty;

        /// <summary>
        /// Renders the complete CPU state to the console using only CPUContext
        /// </summary>
        public void Render(CPUContext context)
        {
            Console.Clear();

            var sb = new StringBuilder();

            sb.AppendLine("=== CPU DEBUG STATE ===");

            sb.Append($"PC: 0x{context.ProgramCounter.Value:X4}  ");
            sb.Append($"SP: 0x{context.StackPointer.Value:X2}  ");
            sb.Append($"Z:{context.ZeroFlag} C:{context.CarryFlag} ");
            sb.AppendLine($"H:{context.Halted}");

            sb.AppendLine("\nRegisters:");
            var regs = context.Registers.GetAllRegisters();
            for (int row = 0; row < 2; row++)
            {
                sb.Append("   ");
                for (int col = 0; col < 4; col++)
                {
                    int idx = row * 4 + col;
                    string rname = $"R{idx}";
                    string val = regs[idx].ToString("X2");
                    sb.Append($"{rname,-3} {val,-3} | ");
                }
                sb.Length -= 3; // remove trailing " |"
                sb.AppendLine();
            }

            sb.AppendLine("\nRAM (0x00-0x1F):");
            for (int line = 0; line < 2; line++)
            {
                int start = line * 16;
                sb.Append($"{start:X2}: ");
                for (int i = 0; i < 16; i++)
                {
                    byte b = context.RAM[(byte)(start + i)];
                    sb.Append($"{b:X2} ");
                }
                sb.AppendLine();
            }

            sb.AppendLine("\nRegistered Ports:");
            bool hasPorts = false;
            for (int p = 0; p < Architecture.IO_PORT_COUNT; p++)
            {
                var port = context.Ports[p];
                if (port != null)
                {
                    hasPorts = true;
                    byte val = port.PortLoad();
                    sb.AppendLine($"Port 0x{p:X2}: 0x{val:X2}");
                }
            }
            if (!hasPorts)
                sb.AppendLine("None");

            sb.AppendLine("\nConsole Output:");
            sb.Append(_consoleBuffer);
            sb.AppendLine("\n=====================");

            Console.Write(sb.ToString());
        }

        public void WriteToConsole(string content)
        {
            _consoleBuffer += content;
        }
    }


}