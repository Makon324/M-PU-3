using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulator
{
    /// <summary>
    /// Emulates program counter and call stack behavior.
    /// </summary>
    internal sealed class ProgramCounter
    {
        private ushort _programCounter;

        private Stack<ushort> _callStack = new Stack<ushort>();

        public ProgramCounter(ushort address = 0)
        {
            _programCounter = address;
        }
        public ushort Value
        {
            get => _programCounter;
            set => _programCounter = value;
        }
        public void Increment()
        {
            _programCounter++;
        }

        public void SetBRH(ushort address)
        {
            _programCounter = address;
        }

        public void PushJMP(ushort adress)
        {
            _callStack.Push((ushort)(_programCounter + 1));
            _programCounter = adress;
        }

        public void PopRET()
        {
            if (_callStack.Count == 0) throw new InvalidOperationException("Call stack is empty.");
            _programCounter = _callStack.Pop();
        }
    }
}
