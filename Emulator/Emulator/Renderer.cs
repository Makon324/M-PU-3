   using System.Text;

namespace Emulator
{
    internal interface IRenderer
    {
        public void Render(CPU cpu);
        
        public void WriteToConsole(string content);
    }

    internal sealed class Renderer : IRenderer
    {
        public void Render(CPU cpu)
        {
            // In normal mode, we just pass through without rendering anything.
        }

        public void WriteToConsole(string content)
        {
            Console.CursorVisible = false;

            Console.Write(content);
        }
    }

    /// <summary>
    /// Singleton MVC Renderer for displaying emulator state in console
    /// </summary>
    internal sealed class DebugRenderer : IRenderer
    {
        private const int CPU_STATE_VIEW_WIDTH = 52;
        private const int REGISTERS_PER_ROW = 4;
        private const int RAM_CELLS_PER_ROW = 16;

        private const int PROGRAM_WINDOW_BEFORE_PC = 10;
        private const int PROGRAM_WINDOW_AFTER_PC = 20;

        // Buffer with that is gonna be printed to console
        private string _consoleBuffer = string.Empty;

        private string _oldConsoleBuffer = string.Empty;

        /// <summary>
        /// Renders the complete CPU state to the console using only CPUContext
        /// </summary>
        public void Render(CPU cpu)
        {
            Console.CursorVisible = false;

            CPUContext context = cpu.Context;

            // Build left lines for CPU state
            List<string> leftLines = [];

            leftLines.Add("=== CPU DEBUG STATE ===");

            leftLines.Add($"PC: 0x{context.ProgramCounter.Value:X4}  SP: 0x{context.StackPointer.Value:X2}  Z:{context.ZeroFlag} C:{context.CarryFlag} H:{context.Halted}");

            leftLines.Add("");

            leftLines.Add("Registers:");
            var regs = context.Registers.GetAllRegisters();
            for (int row = 0; row < 2; row++)
            {
                var regLine = new StringBuilder("   ");
                for (int col = 0; col < REGISTERS_PER_ROW; col++)
                {
                    int idx = row * 4 + col;
                    string rname = $"R{idx}";
                    string val = regs[idx].ToString("X2");
                    regLine.Append($"{rname,-3} {val,-3} | ");
                }
                regLine.Length -= 3; // remove trailing " |"
                leftLines.Add(regLine.ToString());
            }

            leftLines.Add("");

            leftLines.Add("RAM:");
            for (int line = 0; line < Architecture.RAM_SIZE / RAM_CELLS_PER_ROW; line++)
            {
                int start = line * (Architecture.RAM_SIZE / RAM_CELLS_PER_ROW);
                var ramLine = new StringBuilder($"{start:X2}: ");
                for (int i = 0; i < RAM_CELLS_PER_ROW; i++)
                {
                    byte b = context.RAM[(byte)(start + i)];
                    ramLine.Append($"{b:X2} ");
                }
                leftLines.Add(ramLine.ToString().TrimEnd());
            }

            leftLines.Add("");

            leftLines.Add("Registered Ports:");
            bool hasPorts = false;
            for (int p = 0; p < Architecture.IO_PORT_COUNT; p++)
            {
                var port = context.Ports[p];
                if (port != null)
                {
                    hasPorts = true;
                    byte val = port.Value;
                    leftLines.Add($"Port 0x{p:X2}: 0x{val:X2}");
                }
            }
            if (!hasPorts)
                leftLines.Add("None");

            // Build right lines for Pipeline and Program
            List<string> rightLines = [];

            rightLines.Add("Instruction Pipeline:");

            var pipeline = cpu.GetPipeline();
            for (int i = 0; i < pipeline.Length; i++)
            {
                string stageLabel = i == 0 ? "Next to execute" : $"In pipeline {i}  ";
                rightLines.Add($"{stageLabel}: {pipeline[i]}");
            }

            rightLines.Add("");

            rightLines.Add("Program Instructions (around PC):");
            var program = cpu.GetProgram();
            ushort pc = context.ProgramCounter.Value;
            int startAddr = Math.Max(0, pc - PROGRAM_WINDOW_BEFORE_PC);
            int endAddr = Math.Min(program.Length, pc + PROGRAM_WINDOW_AFTER_PC);
            for (int addr = startAddr; addr < endAddr; addr++)
            {
                string marker = (addr == pc) ? " <- PC" : "";
                rightLines.Add($"0x{addr:X4}: {program[(ushort)addr]}{marker}");
            }

            // Combine left and right into StringBuilder
            StringBuilder sb = new();
            int maxHeight = Math.Max(leftLines.Count, rightLines.Count);
            for (int i = 0; i < maxHeight; i++)
            {
                string left = i < leftLines.Count ? leftLines[i] : "";
                if (left.Length > CPU_STATE_VIEW_WIDTH)
                {
                    left = left[..CPU_STATE_VIEW_WIDTH];
                }
                left = left.PadRight(CPU_STATE_VIEW_WIDTH);
                string right = i < rightLines.Count ? rightLines[i] : "";
                sb.AppendLine($"{left}| {right}");
            }

            sb.AppendLine("\nConsole Output:");
            sb.Append(_consoleBuffer);
            sb.AppendLine("\n=====================");

            UpdateConsole(sb.ToString());
        }

        private void UpdateConsole(string newContent)
        {
            string[] newLines = newContent.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            string[] oldLines = _oldConsoleBuffer.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

            int maxLines = Math.Max(newLines.Length, oldLines.Length);

            for (int row = 0; row < maxLines; row++)
            {
                bool isNew = row < newLines.Length;
                string newLine = isNew ? newLines[row] : "";
                string oldLine = (row < oldLines.Length) ? oldLines[row] : "";

                int maxCols = Math.Max(newLine.Length, oldLine.Length);

                for (int col = 0; col < maxCols; col++)
                {
                    char newChar = col < newLine.Length ? newLine[col] : ' ';
                    char oldChar = col < oldLine.Length ? oldLine[col] : ' ';

                    if (newChar != oldChar)
                    {
                        Console.SetCursorPosition(col, row);
                        Console.Write(newChar);
                    }
                }
            }

            _oldConsoleBuffer = newContent;
        }

        public void WriteToConsole(string content)
        {
            _consoleBuffer += content;
        }
    }


}