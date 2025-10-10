using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulator.Tests
{
    public class CPUTests
    {
        [Fact]
        public void SampleTest_ShouldPass()
        {
            Assert.True(true); // Simple test to verify test discovery
        }

        [Fact]
        public void Constructor_WithProgram_InitializesCorrectly()
        {
            var program = TestHelpers.CreateProgram(new Instruction("NOP"));
            var cpu = new CPU(program);
            Assert.NotNull(cpu);
        }

        [Fact]
        public void Run_WithHaltInstruction_StopsExecution()
        {
            var program = TestHelpers.CreateProgram(new Instruction("HALT"));
            var cpu = new CPU(program);

            cpu.Run();

            Assert.True(cpu.Context.Halted);
        }
    }
}
