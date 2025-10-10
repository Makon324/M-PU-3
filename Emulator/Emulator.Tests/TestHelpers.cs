using Microsoft.VisualStudio.TestPlatform.TestHost;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulator.Tests
{
    internal static class TestHelpers
    {
        public static Program CreateProgram(params Instruction[] instructions)
        {
            return new Program(instructions);
        }

        public static CPUContext CreateContext()
        {
            return new CPUContext();
        }
    }
}
