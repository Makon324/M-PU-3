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

        [Fact]
        public void Step_StartPipeline_FillsPipelineWithNops()
        {
            var program = TestHelpers.CreateProgram(new Instruction("HLT"));
            var cpu = new CPU(program);

            // Manually call StartPipeline to simulate initial state
            typeof(CPU).GetMethod("StartPipeline", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.Invoke(cpu, null);

            // Pipeline should be filled with NOPs; no execution yet
            Assert.Equal(0, cpu.Context.ProgramCounter.Value);
            Assert.False(cpu.Context.Halted);
        }

        [Fact]
        public void Step_ControlFlow_FlushesPipelineWithNops()
        {
            var program = TestHelpers.CreateProgram(
                new Instruction("JMP", new Argument[] { new AddressArgument(2) }), // Jump to HLT, skipping NOP
                new Instruction("NOP"),
                new Instruction("HLT")
            );
            var cpu = new CPU(program);

            // Start pipeline
            typeof(CPU).GetMethod("StartPipeline", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.Invoke(cpu, null);

            // Step through start and flush pipeline before control flow
            for (int i = 0; i < Architecture.INSTRUCTION_PIPELINE_SIZE; i++)
            {
                cpu.Step();
            }

            cpu.Step(); // Execute JMP
            Assert.Equal(2, cpu.Context.ProgramCounter.Value); // Jumped to address 2

            // Executes NOPs after JMP as result of flush
            for (int i = 0; i < Architecture.INSTRUCTION_PIPELINE_SIZE; i++)
            {
                cpu.Step();
            }

            // Next Step executes HLT
            cpu.Step();
            Assert.True(cpu.Context.Halted);
        }

        [Fact]
        public void Step_WhenHalted_DoesNothing()
        {
            var program = TestHelpers.CreateProgram(new Instruction("HLT"));
            var cpu = new CPU(program);

            cpu.Run(); // Halt the CPU

            var initialPc = cpu.Context.ProgramCounter.Value;
            cpu.Step();

            Assert.True(cpu.Context.Halted);
            Assert.Equal(initialPc, cpu.Context.ProgramCounter.Value); // No change
        }

        [Fact]
        public void Step_MultipleSteps_ExecutesSequenceCorrectly()
        {
            var program = TestHelpers.CreateProgram(
                new Instruction("LDI", new Argument[] { new RegisterArgument((byte)Register.R1), new NumberArgument(5) }),
                new Instruction("LDI", new Argument[] { new RegisterArgument((byte)Register.R2), new NumberArgument(3) }),
                new Instruction("ADD", new Argument[] { new RegisterArgument((byte)Register.R3), new RegisterArgument((byte)Register.R1), new RegisterArgument((byte)Register.R2) }),
                new Instruction("HLT")
            );
            var cpu = new CPU(program);

            // Start pipeline
            typeof(CPU).GetMethod("StartPipeline", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.Invoke(cpu, null);

            // Step through pipeline stages
            for (int i = 0; i < Architecture.INSTRUCTION_PIPELINE_SIZE; i++)
            {
                cpu.Step();
            }
            cpu.Step(); // Execute LDI R1
            Assert.Equal(5, cpu.Context.Registers[Register.R1]);
            
            cpu.Step(); // Execute LDI R2
            Assert.Equal(3, cpu.Context.Registers[Register.R2]);

            cpu.Step(); // Execute ADD
            Assert.Equal(8, cpu.Context.Registers[Register.R3]);

            // Step to execute HLT
            for (int i = 0; i < Architecture.INSTRUCTION_PIPELINE_SIZE; i++)
            {
                cpu.Step();
            }
            Assert.True(cpu.Context.Halted);
        }


    }
}
