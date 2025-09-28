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
        private IReadOnlyDictionary<string, ushort> _labels;
        private IReadOnlyList<InstructionStatement> _program;
        private InstructionStatement[] _pipeline = new InstructionStatement[3];


        public CPU(IReadOnlyDictionary<string, ushort> labels, IReadOnlyList<InstructionStatement> program)
        {
            _registers = new RegisterCollection();
            _ram = new RAM();
            _programCounter = new ProgramCounter(0);
            _stackPointer = 0;
            _zeroFlag = false;
            _carryFlag = false;
            _halted = false;

            _labels = labels;
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
