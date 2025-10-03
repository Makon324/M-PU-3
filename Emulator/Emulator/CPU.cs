namespace Emulator
{
    internal sealed class CPU
    {
        private CPUContext _context;
        private readonly InstructionPipeline _pipeline;
        private readonly IReadOnlyList<Instruction> _program;

        public CPU(IReadOnlyList<Instruction> program)
        {
            _context = new CPUContext();
            _pipeline = new InstructionPipeline();

            _program = program;
        }

        /// <summary>
        /// Starts program execution, advancing the instruction pipeline and executing instructions until a Halt instruction is encountered.
        /// </summary>
        public void Run()
        {
            while (!_context.Halted)
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
            if (_context.ProgramCounter.Value >= _program.Count)
                throw new InvalidOperationException("Program counter beyond loaded program");

            return _program[_context.ProgramCounter.Value];
        }

        private static bool RequiresPipelineFlush(Instruction instruction)
        {
            BaseExecute execute = ExecuteFactory.GetExecute(instruction);
            return execute.IsControlFlowInstruction;
        }

        /// <summary>
        /// Executes an instruction coming of the pipeline, advances the pipeline stages.
        /// </summary>
        private void ExecutePipelineInstruction(Instruction instruction, bool advancePC = true)
        {
            var toExecute = _pipeline.Advance(instruction);
            ExecuteInstruction(toExecute, advancePC);
        }

        /// <summary>
        /// Flushes the instruction pipeline with NOP (No Operation) instructions, 
        /// executing instructions in the pipeline.
        /// </summary>
        /// <remarks>Used before branch instructions.</remarks>
        private void FlushPipelineWithNops()
        {
            for (int i = 0; i < Architecture.INSTRUCTION_PIPELINE_SIZE - 1; i++)
            {
                ExecutePipelineInstruction(new Instruction("NOP"), advancePC: false);
            }
        }

        /// <summary>
        /// Executes a single instruction.
        /// </summary>
        /// <param name="instruction">Instruction to execute.</param>
        /// <param name="advancePC">Whether advance PC after executing the instruction.</param>
        private void ExecuteInstruction(Instruction instruction, bool advancePC = true)
        {
            BaseExecute toExecute = ExecuteFactory.GetExecute(instruction);
            toExecute.Execute(ref _context, advancePC);
        }













    }
}
