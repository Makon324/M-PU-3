using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulator
{
    /// <summary>
    /// Emulates instruction pipeline behavior.
    /// </summary>
    /// <remarks>Pipeline size should remain <see cref="Architecture.INSTRUCTION_PIPELINE_SIZE"> at all times.</remarks>
    internal class InstructionPipeline
    {
        private Queue<Instruction> _pipeline;

        public InstructionPipeline()
        {
            _pipeline = new Queue<Instruction>();

            for (int i = 0; i < Architecture.INSTRUCTION_PIPELINE_SIZE; i++)
            {
                _pipeline.Enqueue(GetNOP());
            }
        }

        /// <summary>
        /// Simulates advancing the pipeline by one instruction.
        /// </summary>
        public Instruction Advance(Instruction nextInstruction)
        {
            _pipeline.Enqueue(nextInstruction);
            return _pipeline.Dequeue();
        }

        private Instruction GetNOP()
        {
            return new Instruction("NOP", Array.Empty<Argument>());
        }
    }
}
