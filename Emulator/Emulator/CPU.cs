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
        private byte _stackPointer;
        private bool _zeroFlag;
        private bool _carryFlag;
        private bool _halted;
        private InstructionPipeline _pipeline;
        private IReadOnlyList<Instruction> _program;        


        public CPU(IReadOnlyList<Instruction> program)
        {
            _registers = new RegisterCollection();
            _ram = new RAM();
            _programCounter = new ProgramCounter(0);
            _stackPointer = 0;
            _zeroFlag = false;
            _carryFlag = false;
            _halted = false;
            _pipeline = new InstructionPipeline();

            _program = program;
        }

        /// <summary>
        /// Starts program execution
        /// </summary>
        public void Run()
        {
            while (!_halted)
            {
                // execute instructions
            }
        }

        private InstructionStatement GetCurrentInstruction()
        {
            return _program[_programCounter.Value];
        }











    }
}
