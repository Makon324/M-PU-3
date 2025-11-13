namespace Emulator
{
    internal sealed class CPU
    {
        private CPUContext _context;
        private readonly InstructionPipeline _pipeline;
        private readonly Program _program;

        // State variables for step-by-step execution
        private int _flushSteps = 0;
        private bool _finalNopPending = false;

        public CPU(Program program)
        {
            _context = new CPUContext();
            _pipeline = new InstructionPipeline();

            _program = program;
        }

        public ref CPUContext Context => ref _context;

        internal Instruction[] GetPipeline() => _pipeline.GetPipeline();

        internal Program GetProgram() => _program;

        /// <summary>
        /// Starts program execution, advancing the instruction pipeline and executing instructions until a Halt instruction is encountered.
        /// </summary>
        /// <remarks>This is an infinite spinning loop that runs as fast as possible. 
        /// It is also possible to step through the program one instruction at a time using the Step() method.</remarks>
        public void Run()
        {
            while (!_context.Halted)
            {
                Step();
            }
        }

        /// <summary>
        /// Advances the pipeline until the function used using current CAL is completed.
        /// </summary>
        public void StepOverCall()
        {
            string currentMnemonic = _pipeline.GetPipeline()[0].Mnemonic;

            // Execute Called function
            int? callDepth = null;

            while ((callDepth ?? int.MaxValue) > 0)
            {
                callDepth ??= 0;

                if (currentMnemonic == "CAL")
                {
                    callDepth++;
                }
                else if (currentMnemonic == "RET")
                {
                    callDepth--;
                }

                Step();
                currentMnemonic = _pipeline.GetPipeline()[0].Mnemonic;
            }

            // Execute NOPs after RET
            if (Global.GetService<YAMLConfig>().doStepOverNOPsAfterRET)
            {
                for (int i = 0; i < Architecture.INSTRUCTION_PIPELINE_SIZE; i++)
                {
                    if (currentMnemonic == "NOP")
                    {
                        Step();
                    }
                    currentMnemonic = _pipeline.GetPipeline()[0].Mnemonic;
                }
            }
        }

        /// <summary>
        /// Advances the pipeline by 1 stage, executing 1 instruction.
        /// </summary>
        public void Step()
        {
            if (_context.Halted) return;

            Instruction toPush;
            bool advancePC;

            if (_flushSteps > 0)
            {
                toPush = new Instruction("NOP");
                advancePC = false;
                _flushSteps--;
            }
            else if (_finalNopPending)
            {
                toPush = new Instruction("NOP");
                advancePC = true;
                _finalNopPending = false;
            }
            else
            {
                Instruction next = FetchInstruction();
                bool isControlFlow = IsControlFlow(next);
                toPush = next;
                if (isControlFlow)
                {
                    advancePC = false;
                    _flushSteps = Architecture.INSTRUCTION_PIPELINE_SIZE - 1;
                    _finalNopPending = true;
                }
                else
                {
                    advancePC = true;
                }
            }

            ExecutePipelineInstruction(toPush, advancePC);
        }

        /// <summary>
        /// Fetches the instruction at the address of the current Program Counter (PC).
        /// </summary>
        private Instruction FetchInstruction()
        {
            if (_context.ProgramCounter.Value >= _program.Length)
                throw new InvalidOperationException("Program counter beyond loaded program");

            return _program[_context.ProgramCounter.Value];
        }

        private static bool IsControlFlow(Instruction instruction)
        {
            BaseExecute execute = ExecuteFactory.GetExecute(instruction);
            return execute.IsControlFlowInstruction;
        }

        /// <summary>
        /// Executes an instruction coming off the pipeline, advances the pipeline stages.
        /// </summary>
        /// <param name="instruction">Instruction to execute.</param>
        /// <param name="advancePC">Whether advance Program Counter (PC) after executing the instruction.</param>
        private void ExecutePipelineInstruction(Instruction instruction, bool advancePC = true)
        {
            var toExecute = _pipeline.Advance(instruction);
            ExecuteInstruction(toExecute, advancePC);
        }

        /// <summary>
        /// Executes a single instruction.
        /// </summary>
        /// <param name="instruction">Instruction to execute.</param>
        /// <param name="advancePC">Whether advance Program Counter (PC) after executing the instruction.</param>
        private void ExecuteInstruction(Instruction instruction, bool advancePC = true)
        {
            BaseExecute toExecute = ExecuteFactory.GetExecute(instruction);
            toExecute.Execute(ref _context, advancePC);
        }




    }
}