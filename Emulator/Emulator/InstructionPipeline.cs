namespace Emulator
{
    /// <summary>
    /// Emulates instruction pipeline behavior.
    /// </summary>
    /// <remarks>Pipeline size should remain <see cref="Architecture.INSTRUCTION_PIPELINE_SIZE"> at all times.</remarks>
    internal sealed class InstructionPipeline
    {
        private readonly Queue<Instruction> _pipeline;

        public InstructionPipeline()
        {
            _pipeline = new Queue<Instruction>();

            for (int i = 0; i < Architecture.INSTRUCTION_PIPELINE_SIZE; i++)
            {
                _pipeline.Enqueue(new Instruction("NOP"));
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
    }
}
