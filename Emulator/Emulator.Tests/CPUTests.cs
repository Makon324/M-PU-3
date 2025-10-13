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
        public void Constructor_WithProgram_InitializesCorrectly()
        {
            var program = TestHelpers.CreateProgram(new Instruction("NOP"));
            var cpu = new CPU(program);
            Assert.NotNull(cpu);
        }

        [Fact]
        public void Run_WithHaltInstruction_StopsExecution()
        {
            var program = TestHelpers.CreateProgram(new Instruction("HLT"));
            var cpu = new CPU(program);

            cpu.Run();

            Assert.True(cpu.Context.Halted);
        }

        [Fact]
        public void CompleteProgram_ExecutesCorrectly()
        {
            var program = TestHelpers.CreateProgram(
                new Instruction("LDI", new Argument[] {
                new RegisterArgument((byte)Register.R1),
                new NumberArgument(10)
                }),
                new Instruction("LDI", new Argument[] {
                new RegisterArgument((byte)Register.R2),
                new NumberArgument(20)
                }),
                new Instruction("ADD", new Argument[] {
                new RegisterArgument((byte)Register.R1),
                new RegisterArgument((byte)Register.R1),
                new RegisterArgument((byte)Register.R2)
                }),
                new Instruction("MST", new Argument[] {
                new RegisterArgument((byte)Register.R1),
                new NumberArgument(0x00)
                }),
                new Instruction("HLT")
            );

            var cpu = new CPU(program);
            cpu.Run();

            Assert.True(cpu.Context.Halted);
            Assert.Equal(30, cpu.Context.Registers[Register.R1]);
            Assert.Equal(30, cpu.Context.RAM[0x00]);
        }

        [Fact]
        public void ControlFlowInstruction_ExecutesCorrectly()
        {
            var program = TestHelpers.CreateProgram(
                new Instruction("LDI", new Argument[] { new RegisterArgument((byte)Register.R1), new NumberArgument(10) }),
                new Instruction("LDI", new Argument[] { new RegisterArgument((byte)Register.R2), new NumberArgument(20) }),
                new Instruction("JMP", new Argument[] { new AddressArgument(5) }),  // Should skip ADD
                new Instruction("ADD", new Argument[] { new RegisterArgument((byte)Register.R1), new RegisterArgument((byte)Register.R1), new RegisterArgument((byte)Register.R2) }),
                new Instruction("HLT"),
                new Instruction("MOV", new Argument[] { new RegisterArgument((byte)Register.R3), new RegisterArgument((byte)Register.R1) }), // Jump target
                new Instruction("HLT")
            );

            var cpu = new CPU(program);
            cpu.Run();

            Assert.Equal(10, cpu.Context.Registers[Register.R1]);
            Assert.Equal(10, cpu.Context.Registers[Register.R3]);
        }

        [Fact]
        public void ProgramCounterBeyondProgram_ThrowsException()
        {
            var program = TestHelpers.CreateProgram(new Instruction("HLT"));
            var cpu = new CPU(program);

            // Force PC beyond program length
            cpu.Context.ProgramCounter.SetBRH(10);

            Assert.Throws<InvalidOperationException>(() => cpu.Run());
        }





    }
}
