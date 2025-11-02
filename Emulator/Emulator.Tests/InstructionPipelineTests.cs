namespace Emulator.Tests
{
    public class InstructionPipelineTests
    {
        private readonly InstructionPipeline _pipeline;

        public InstructionPipelineTests()
        {
            _pipeline = new InstructionPipeline();
        }

        [Fact]
        public void Constructor_InitializesCorrectly()
        {
            var pipelineState = _pipeline.GetPipeline();
            Assert.Equal(Architecture.INSTRUCTION_PIPELINE_SIZE, pipelineState.Length);
            Assert.All(pipelineState, instruction =>
                Assert.Equal("NOP", instruction.Mnemonic));
        }

        [Fact]
        public void Advance_MultipleAdvances_MaintainsPipelineSize()
        {
            for (int i = 0; i < 10; i++)
            {
                var newInstruction = new Instruction("ADD");
                _pipeline.Advance(newInstruction);
            }

            var pipelineState = _pipeline.GetPipeline();
            Assert.Equal(Architecture.INSTRUCTION_PIPELINE_SIZE, pipelineState.Length);
        }

        [Fact]
        public void Advance_SequenceOfInstructions_ReturnsInstructionsInFIFOOrder()
        {
            var instructions = new[]
            {
                new Instruction("ADD"),
                new Instruction("SUB"),
                new Instruction("MUL"),
                new Instruction("DIV")
            };

            for (int i = 0; i < instructions.Length; i++)
            {
                var returned = _pipeline.Advance(instructions[i]);

                // The first few returns should be NOPs (from initial pipeline)
                if (i < Architecture.INSTRUCTION_PIPELINE_SIZE)
                {
                    Assert.Equal("NOP", returned.Mnemonic);
                }
                else
                {
                    // After pipeline is filled, should return the oldest new instruction
                    Assert.Equal(instructions[i - Architecture.INSTRUCTION_PIPELINE_SIZE].Mnemonic, returned.Mnemonic);
                }
            }
        }

        [Fact]
        public void GetPipeline_ReturnsCurrentState()
        {
            var newInstruction1 = new Instruction("ADD");
            var newInstruction2 = new Instruction("SUB");

            _pipeline.Advance(newInstruction1);
            _pipeline.Advance(newInstruction2);
            var pipelineState = _pipeline.GetPipeline();

            Assert.Equal(Architecture.INSTRUCTION_PIPELINE_SIZE, pipelineState.Length);
            Assert.Contains(newInstruction1, pipelineState);
            Assert.Contains(newInstruction2, pipelineState);
        }

        [Fact]
        public void GetPipeline_ReturnsCopyNotReference()
        {
            var initialPipeline = _pipeline.GetPipeline();

            initialPipeline[0] = new Instruction("MODIFIED");

            var currentPipeline = _pipeline.GetPipeline();
            Assert.Equal("NOP", currentPipeline[0].Mnemonic); // Should still be original NOP
        }
    }
}
