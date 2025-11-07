namespace Emulator.Tests
{
    public class ProgramLoadingTests
    {
        [Fact]
        public void LoadProgram_ThrowsArgumentException_ForInvalidPath()
        {
            Assert.Throws<ArgumentException>(() => ProgramLoader.LoadProgram(null!));
            Assert.Throws<ArgumentException>(() => ProgramLoader.LoadProgram(string.Empty));
            Assert.Throws<ArgumentException>(() => ProgramLoader.LoadProgram("   "));
        }

        [Fact]
        public void LoadProgram_ThrowsFileNotFoundException_ForNonExistentFile()
        {
            var nonExistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".asm");
            Assert.Throws<FileNotFoundException>(() => ProgramLoader.LoadProgram(nonExistentPath));
        }

        [Fact]
        public void ResolveLabels_ResolvesCorrectly()
        {
            var statements = new List<ProgramStatement>
            {
                new LabelStatement("start", 1, 1),
                new InstructionStatement("NOP", Enumerable.Empty<Token>(), 2, 1),
                new LabelStatement("loop", 3, 1),
                new InstructionStatement("ADD", new[] { new Token("REG", "r1", 4, 5), new Token("NUM", "1", 4, 8) }, 4, 1),
                new InstructionStatement("JMP", new[] { new Token("IDENT", "loop", 5, 5) }, 5, 1),
            };

            var (labels, instructions) = ProgramCompiler.ResolveLabels(statements, "test.asm");

            Assert.Equal(2, labels.Count);
            Assert.Equal((ushort)0, labels["start"]);
            Assert.Equal((ushort)1, labels["loop"]);

            Assert.Equal(3, instructions.Count);
            Assert.Equal("NOP", instructions[0].Mnemonic);
            Assert.Empty(instructions[0].Arguments);
            Assert.Equal("ADD", instructions[1].Mnemonic);
            Assert.Equal(2, instructions[1].Arguments.Count);
            Assert.Equal("JMP", instructions[2].Mnemonic);
            Assert.Single(instructions[2].Arguments);
        }

        [Fact]
        public void ResolveLabels_ThrowsOnDuplicateLabel()
        {
            var statements = new List<ProgramStatement>
            {
                new LabelStatement("dup", 1, 1),
                new InstructionStatement("NOP", Enumerable.Empty<Token>(), 2, 1),
                new LabelStatement("dup", 3, 1),
            };

            var ex = Assert.Throws<ProgramLoadException>(() => ProgramCompiler.ResolveLabels(statements, "test.asm"));
            Assert.Contains("Duplicate label", ex.Message);
        }

        [Fact]
        public void ResolveLabels_ThrowsWhenExceedsMaxSize()
        {
            var statements = new List<ProgramStatement>();
            for (int i = 0; i <= Architecture.MAX_PROGRAM_SIZE; i++) // One more than max
            {
                statements.Add(new InstructionStatement("NOP", Enumerable.Empty<Token>(), i + 1, 1));
            }

            var ex = Assert.Throws<ProgramLoadException>(() => ProgramCompiler.ResolveLabels(statements, "test.asm"));
            Assert.Contains("exceeds", ex.Message);
        }

        [Fact]
        public void CompileProgram_CompilesInstructionsCorrectly()
        {
            var labels = new Dictionary<string, ushort> { { "loop", 10 } };
            var instructionStatements = new List<InstructionStatement>
            {
                new InstructionStatement("NOP", Enumerable.Empty<Token>(), 1, 1),
                new InstructionStatement("LDI", new[] { new Token("REGISTER", "r1", 2, 5), new Token("HEX", "0xA", 2, 8) }, 2, 1),
                new InstructionStatement("ADD", new[] { new Token("REGISTER", "r0", 3, 5), new Token("REGISTER", "r1", 3, 8), new Token("REGISTER", "r2", 3, 11) }, 3, 1),
                new InstructionStatement("JMP", new[] { new Token("IDENT", "loop", 4, 5) }, 4, 1),
                new InstructionStatement("JMP", new[] { new Token("DEC", "20", 5, 5) }, 5, 1),
                new InstructionStatement("LDI", new[] { new Token("REGISTER", "r3", 6, 5), new Token("BIN", "0b101", 6, 8) }, 6, 1),
            };

            var compiled = ProgramCompiler.CompileProgram(labels, instructionStatements);

            Assert.Equal(6, compiled.Count);

            Assert.Equal("NOP", compiled[0].Mnemonic);
            Assert.Empty(compiled[0].Arguments);

            Assert.Equal("LDI", compiled[1].Mnemonic);
            var args1 = compiled[1].Arguments;
            Assert.Equal(2, args1.Count);
            Assert.IsType<RegisterArgument>(args1[0]);
            Assert.Equal((byte)1, ((RegisterArgument)args1[0]).Value);
            Assert.IsType<NumberArgument>(args1[1]);
            Assert.Equal((byte)10, ((NumberArgument)args1[1]).Value); // 0xA = 10

            Assert.Equal("ADD", compiled[2].Mnemonic);
            var args2 = compiled[2].Arguments;
            Assert.Equal(3, args2.Count);
            Assert.IsType<RegisterArgument>(args2[0]);
            Assert.Equal((byte)0, ((RegisterArgument)args2[0]).Value);
            Assert.IsType<RegisterArgument>(args2[1]);
            Assert.Equal((byte)1, ((RegisterArgument)args2[1]).Value);
            Assert.IsType<RegisterArgument>(args2[2]);
            Assert.Equal((byte)2, ((RegisterArgument)args2[2]).Value);

            Assert.Equal("JMP", compiled[3].Mnemonic);
            var args3 = compiled[3].Arguments;
            Assert.Single(args3);
            Assert.IsType<AddressArgument>(args3[0]);
            Assert.Equal((ushort)10, ((AddressArgument)args3[0]).Value); // label

            Assert.Equal("JMP", compiled[4].Mnemonic);
            var args4 = compiled[4].Arguments;
            Assert.Single(args4);
            Assert.IsType<AddressArgument>(args4[0]);
            Assert.Equal((ushort)20, ((AddressArgument)args4[0]).Value);

            Assert.Equal("LDI", compiled[5].Mnemonic);
            var args5 = compiled[5].Arguments;
            Assert.Equal(2, args5.Count);
            Assert.IsType<RegisterArgument>(args5[0]);
            Assert.Equal((byte)3, ((RegisterArgument)args5[0]).Value);
            Assert.IsType<NumberArgument>(args5[1]);
            Assert.Equal((byte)5, ((NumberArgument)args5[1]).Value); // 0b101 = 5
        }
    }
}