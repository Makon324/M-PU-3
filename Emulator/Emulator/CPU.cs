using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulator
{
    internal sealed class CPU
    {
        private RegisterCollection _registers;
        private RAM _ram;
        private ProgramCounter _programCounter;
        private InstructionPipeline _pipeline;
        private byte _stackPointer;
        private bool _zeroFlag;
        private bool _carryFlag;
        private bool _halted;
        private readonly IReadOnlyList<Instruction> _program;


        public CPU(IReadOnlyList<Instruction> program)
        {
            _registers = new RegisterCollection();
            _ram = new RAM();
            _programCounter = new ProgramCounter(0);
            _pipeline = new InstructionPipeline();
            _stackPointer = 0;
            _zeroFlag = false;
            _carryFlag = false;
            _halted = false;

            _program = program;
        }

        /// <summary>
        /// Starts program execution, advancing the instruction pipeline and executing instructions until a HALT instruction is encountered.
        /// </summary>
        public void Run()
        {
            while (!_halted)
            {
                Instruction nextInstruction = FetchInstruction();

                if (RequiresPipelineFlush(nextInstruction))
                {
                    ExecutePipelineInstruction(nextInstruction, advancePC: false);

                    FlushPipelineWithNops();

                    ExecutePipelineInstruction(new Instruction("NOP"), advancePC: true);
                }
                else
                {
                    ExecutePipelineInstruction(nextInstruction);
                }
            }
        }

        private Instruction FetchInstruction()
        {
            if (_programCounter.Value >= _program.Count)
                throw new InvalidOperationException("Program counter beyond loaded program");

            return _program[_programCounter.Value];
        }

        private static bool RequiresPipelineFlush(Instruction instruction)
        {
            return Architecture.INSTRUCTIONS_THAT_FLUSH_PIPELINE.Contains(instruction.Mnemonic);
        }

        private void ExecutePipelineInstruction(Instruction instruction, bool advancePC = true)
        {
            var toExecute = _pipeline.Advance(instruction);
            ExecuteInstruction(toExecute, advancePC);
        }

        private void FlushPipelineWithNops()
        {
            for (int i = 0; i < Architecture.INSTRUCTION_PIPELINE_SIZE - 1; i++)
            {
                ExecutePipelineInstruction(new Instruction("NOP"), advancePC: false);
            }
        }


        /// <summary>
        /// Execute instruction, changing program counter at the end.
        /// </summary>
        /// <param name="instruction">Instruction to execute.</param>
        /// <param name="advancePC">Whether advance PC after executing the instruction.</param>
        private void ExecuteInstruction(Instruction instruction, bool advancePC = true)
        {
            switch (instruction.Mnemonic)
            {
                case "NOP":
                    if(advancePC) _programCounter.Increment();
                    break;
                case "HALT":
                    _halted = true;
                    if (advancePC) _programCounter.Increment();
                    break;
            }
        }












    }
}
