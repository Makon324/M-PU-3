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
        private ushort _programCounter;
        private byte _stackPointer;
        private bool _zeroFlag;
        private bool _carryFlag;
        private bool _halted;
        private IReadOnlyDictionary<string, ushort> _labels;
        private IReadOnlyList<InstructionStatement> _program;


        public CPU(string programPath)
        {
            _registers = new RegisterCollection();
            _ram = new RAM();
            _programCounter = 0;
            _stackPointer = 0;
            _zeroFlag = false;
            _carryFlag = false;
            _halted = false;

            (_labels, _program) = ProgramLoader.LoadProgram(programPath);
        }

        /// <summary>
        /// Starts program execution
        /// </summary>
        public void Run()
        {
            while (!_halted)
            {
                ExecuteNextInstruction();
            }
        }











    }
}
