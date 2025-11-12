namespace Emulator.Tests
{
    public class InstructionExecutionTests
    {
        [Fact]
        public void ExecuteNOP_Test()
        {
            // Arrange
            var context = new CPUContext();
            ushort initialPC = context.ProgramCounter.Value;
            var nop = new ExecuteNOP();

            // Act
            nop.Execute(ref context);

            // Assert
            Assert.Equal(initialPC + 1, context.ProgramCounter.Value);
            Assert.False(context.Halted);
        }

        [Fact]
        public void ExecuteHLT_Test()
        {
            // Arrange
            var context = new CPUContext();
            ushort initialPC = context.ProgramCounter.Value;
            var hlt = new ExecuteHLT();

            // Act
            hlt.Execute(ref context);

            // Assert
            Assert.Equal(initialPC, context.ProgramCounter.Value); // No advance for control flow
            Assert.True(context.Halted);
        }

        [Theory]
        [InlineData(1, 2, 3, false)]
        [InlineData(100, 100, 200, false)]
        [InlineData(0, 0, 0, false)]
        [InlineData(200, 100, 44, true)]
        public void ExecuteADD_Tests(byte valA, byte valB, byte expected, bool expectedCarry)
        {
            // Arrange
            var (srcA, srcB) = TestHelpers.GetTwoRandomDistinctRegisters();
            var dest = TestHelpers.GetRandomRegister();
            var args = new List<Argument> { new RegisterArgument(dest), new RegisterArgument(srcA), new RegisterArgument(srcB) };
            var add = new ExecuteADD(args);
            var context = new CPUContext();
            context.Registers[srcA] = valA;
            context.Registers[srcB] = valB;
            ushort initialPC = context.ProgramCounter.Value;

            // Act
            add.Execute(ref context);

            // Assert
            Assert.Equal(expected, context.Registers[dest]);
            Assert.Equal(expected == 0, context.ZeroFlag);
            Assert.Equal(expectedCarry, context.CarryFlag);
            Assert.Equal(initialPC + 1, context.ProgramCounter.Value);
        }

        [Theory]
        [InlineData(1, 2, false, 3, false)]
        [InlineData(100, 100, true, 201, false)]
        [InlineData(255, 0, true, 0, true)]
        public void ExecuteADC_Tests(byte valA, byte valB, bool initialCarry, byte expected, bool expectedCarry)
        {
            // Arrange
            var (srcA, srcB) = TestHelpers.GetTwoRandomDistinctRegisters();
            var dest = TestHelpers.GetRandomRegister();
            var args = new List<Argument> { new RegisterArgument(dest), new RegisterArgument(srcA), new RegisterArgument(srcB) };
            var adc = new ExecuteADC(args);
            var context = new CPUContext();
            context.Registers[srcA] = valA;
            context.Registers[srcB] = valB;
            context.CarryFlag = initialCarry;
            ushort initialPC = context.ProgramCounter.Value;

            // Act
            adc.Execute(ref context);

            // Assert
            Assert.Equal(expected, context.Registers[dest]);
            Assert.Equal(expected == 0, context.ZeroFlag);
            Assert.Equal(expectedCarry, context.CarryFlag);
            Assert.Equal(initialPC + 1, context.ProgramCounter.Value);
        }

        [Theory]
        [InlineData(5, 3, 2, true)] // In SUB carry inversed (inline with specification)
        [InlineData(1, 2, 255, false)]
        [InlineData(0, 0, 0, true)]
        public void ExecuteSUB_Tests(byte valA, byte valB, byte expected, bool expectedCarry)
        {
            // Arrange
            var (srcA, srcB) = TestHelpers.GetTwoRandomDistinctRegisters();
            var dest = TestHelpers.GetRandomRegister();
            var args = new List<Argument> { new RegisterArgument(dest), new RegisterArgument(srcA), new RegisterArgument(srcB) };
            var sub = new ExecuteSUB(args);
            var context = new CPUContext();
            context.Registers[srcA] = valA;
            context.Registers[srcB] = valB;
            ushort initialPC = context.ProgramCounter.Value;

            // Act
            sub.Execute(ref context);

            // Assert
            Assert.Equal(expected, context.Registers[dest]);
            Assert.Equal(expected == 0, context.ZeroFlag);
            Assert.Equal(expectedCarry, context.CarryFlag);
            Assert.Equal(initialPC + 1, context.ProgramCounter.Value);
        }

        [Theory]
        [InlineData(5, 3, false, 1, true)] // In SUB carry inversed (inline with specification)
        [InlineData(1, 2, false, 254, false)]
        [InlineData(1, 2, true, 255, false)]
        public void ExecuteSUBC_Tests(byte valA, byte valB, bool initialCarry, byte expected, bool expectedCarry)
        {
            // Arrange
            var (srcA, srcB) = TestHelpers.GetTwoRandomDistinctRegisters();
            var dest = TestHelpers.GetRandomRegister();
            var args = new List<Argument> { new RegisterArgument(dest), new RegisterArgument(srcA), new RegisterArgument(srcB) };
            var subc = new ExecuteSUBC(args);
            var context = new CPUContext();
            context.Registers[srcA] = valA;
            context.Registers[srcB] = valB;
            context.CarryFlag = initialCarry;
            ushort initialPC = context.ProgramCounter.Value;

            // Act
            subc.Execute(ref context);

            // Assert
            Assert.Equal(expected, context.Registers[dest]);
            Assert.Equal(expected == 0, context.ZeroFlag);
            Assert.Equal(expectedCarry, context.CarryFlag);
            Assert.Equal(initialPC + 1, context.ProgramCounter.Value);
        }

        [Theory]
        [InlineData(0xFF, 0x0F)]
        [InlineData(0, 0)]
        [InlineData(0xAA, 0x55)]
        public void ExecuteAND_Tests(byte valA, byte valB)
        {
            // Arrange
            var (srcA, srcB) = TestHelpers.GetTwoRandomDistinctRegisters();
            var dest = TestHelpers.GetRandomRegister();
            var args = new List<Argument> { new RegisterArgument(dest), new RegisterArgument(srcA), new RegisterArgument(srcB) };
            var andInst = new ExecuteAND(args);
            var context = new CPUContext();
            context.Registers[srcA] = valA;
            context.Registers[srcB] = valB;
            ushort initialPC = context.ProgramCounter.Value;

            // Act
            andInst.Execute(ref context);

            // Assert
            byte expected = (byte)(valA & valB);
            Assert.Equal(expected, context.Registers[dest]);
            Assert.Equal(expected == 0, context.ZeroFlag);
            Assert.False(context.CarryFlag); // Always false for logical ops
            Assert.Equal(initialPC + 1, context.ProgramCounter.Value);
        }

        [Theory]
        [InlineData(0xFF, 0x0F)]
        [InlineData(0, 0)]
        [InlineData(0xAA, 0x55)]
        public void ExecuteOR_Tests(byte valA, byte valB)
        {
            // Arrange
            var (srcA, srcB) = TestHelpers.GetTwoRandomDistinctRegisters();
            var dest = TestHelpers.GetRandomRegister();
            var args = new List<Argument> { new RegisterArgument(dest), new RegisterArgument(srcA), new RegisterArgument(srcB) };
            var orInst = new ExecuteOR(args);
            var context = new CPUContext();
            context.Registers[srcA] = valA;
            context.Registers[srcB] = valB;
            ushort initialPC = context.ProgramCounter.Value;

            // Act
            orInst.Execute(ref context);

            // Assert
            byte expected = (byte)(valA | valB);
            Assert.Equal(expected, context.Registers[dest]);
            Assert.Equal(expected == 0, context.ZeroFlag);
            Assert.False(context.CarryFlag);
            Assert.Equal(initialPC + 1, context.ProgramCounter.Value);
        }

        [Theory]
        [InlineData(0xFF, 0x0F)]
        [InlineData(0, 0)]
        [InlineData(0xAA, 0x55)]
        public void ExecuteXOR_Tests(byte valA, byte valB)
        {
            // Arrange
            var (srcA, srcB) = TestHelpers.GetTwoRandomDistinctRegisters();
            var dest = TestHelpers.GetRandomRegister();
            var args = new List<Argument> { new RegisterArgument(dest), new RegisterArgument(srcA), new RegisterArgument(srcB) };
            var xorInst = new ExecuteXOR(args);
            var context = new CPUContext();
            context.Registers[srcA] = valA;
            context.Registers[srcB] = valB;
            ushort initialPC = context.ProgramCounter.Value;

            // Act
            xorInst.Execute(ref context);

            // Assert
            byte expected = (byte)(valA ^ valB);
            Assert.Equal(expected, context.Registers[dest]);
            Assert.Equal(expected == 0, context.ZeroFlag);
            Assert.False(context.CarryFlag);
            Assert.Equal(initialPC + 1, context.ProgramCounter.Value);
        }

        [Theory]
        [InlineData(0xFF)]
        [InlineData(0x00)]
        [InlineData(0xAA)]
        public void ExecuteNOT_Tests(byte valA)
        {
            // Arrange
            var dest = TestHelpers.GetRandomRegister();
            var srcA = TestHelpers.GetRandomRegister();
            var args = new List<Argument> { new RegisterArgument(dest), new RegisterArgument(srcA) };
            var notInst = new ExecuteNOT(args);
            var context = new CPUContext();
            context.Registers[srcA] = valA;
            ushort initialPC = context.ProgramCounter.Value;

            // Act
            notInst.Execute(ref context);

            // Assert
            byte expected = (byte)(~valA);
            Assert.Equal(expected, context.Registers[dest]);
            Assert.Equal(expected == 0, context.ZeroFlag);
            Assert.False(context.CarryFlag);
            Assert.Equal(initialPC + 1, context.ProgramCounter.Value);
        }

        [Theory]
        [InlineData(0x02)]
        [InlineData(0x01)]
        [InlineData(0x80)]
        public void ExecuteSHFT_Tests(byte valA)
        {
            // Arrange
            var dest = TestHelpers.GetRandomRegister();
            var srcA = TestHelpers.GetRandomRegister();
            var args = new List<Argument> { new RegisterArgument(dest), new RegisterArgument(srcA) };
            var shft = new ExecuteSHFT(args);
            var context = new CPUContext();
            context.Registers[srcA] = valA;
            ushort initialPC = context.ProgramCounter.Value;

            // Act
            shft.Execute(ref context);

            // Assert
            byte expected = (byte)(valA >> 1);
            bool expectedCarry = (valA & 0x01) != 0;
            Assert.Equal(expected, context.Registers[dest]);
            Assert.Equal(expected == 0, context.ZeroFlag);
            Assert.Equal(expectedCarry, context.CarryFlag);
            Assert.Equal(initialPC + 1, context.ProgramCounter.Value);
        }

        [Theory]
        [InlineData(0x02, false)]
        [InlineData(0x01, false)]
        [InlineData(0x02, true)]
        public void ExecuteSHFC_Tests(byte valA, bool initialCarry)
        {
            // Arrange
            var dest = TestHelpers.GetRandomRegister();
            var srcA = TestHelpers.GetRandomRegister();
            var args = new List<Argument> { new RegisterArgument(dest), new RegisterArgument(srcA) };
            var shfc = new ExecuteSHFC(args);
            var context = new CPUContext();
            context.Registers[srcA] = valA;
            context.CarryFlag = initialCarry;
            ushort initialPC = context.ProgramCounter.Value;

            // Act
            shfc.Execute(ref context);

            // Assert
            byte carryInBit = (byte)(initialCarry ? 0x80 : 0);
            byte expected = (byte)((valA >> 1) | carryInBit);
            bool expectedCarry = (valA & 0x01) != 0;
            Assert.Equal(expected, context.Registers[dest]);
            Assert.Equal(expected == 0, context.ZeroFlag);
            Assert.Equal(expectedCarry, context.CarryFlag);
            Assert.Equal(initialPC + 1, context.ProgramCounter.Value);
        }

        [Theory]
        [InlineData(0x80)]
        [InlineData(0xF5)]
        [InlineData(0x01)]
        public void ExecuteSHFE_Tests(byte valA)
        {
            // Arrange
            var dest = TestHelpers.GetRandomRegister();
            var srcA = TestHelpers.GetRandomRegister();
            var args = new List<Argument> { new RegisterArgument(dest), new RegisterArgument(srcA) };
            var shfe = new ExecuteSHFE(args);
            var context = new CPUContext();
            context.Registers[srcA] = valA;
            ushort initialPC = context.ProgramCounter.Value;

            // Act
            shfe.Execute(ref context);

            // Assert
            byte signBit = (byte)(valA & 0x80);
            byte expected = (byte)((valA >> 1) | (signBit >> 0));
            bool expectedCarry = (valA & 0x01) != 0;
            Assert.Equal(expected, context.Registers[dest]);
            Assert.Equal(expected == 0, context.ZeroFlag);
            Assert.Equal(expectedCarry, context.CarryFlag);
            Assert.Equal(initialPC + 1, context.ProgramCounter.Value);
        }

        [Theory]
        [InlineData(0x80)]
        [InlineData(0x7F)]
        [InlineData(0x00)]
        public void ExecuteSEX_Tests(byte valA)
        {
            // Arrange
            var dest = TestHelpers.GetRandomRegister();
            var srcA = TestHelpers.GetRandomRegister();
            var args = new List<Argument> { new RegisterArgument(dest), new RegisterArgument(srcA) };
            var sex = new ExecuteSEX(args);
            var context = new CPUContext();
            context.Registers[srcA] = valA;
            ushort initialPC = context.ProgramCounter.Value;

            // Act
            sex.Execute(ref context);

            // Assert
            byte signBit = (byte)(valA & 0x80);
            byte expected = signBit == 0 ? (byte)0 : (byte)0xFF;
            Assert.Equal(expected, context.Registers[dest]);
            Assert.Equal(expected == 0, context.ZeroFlag);
            Assert.False(context.CarryFlag);
            Assert.Equal(initialPC + 1, context.ProgramCounter.Value);
        }

        [Theory]
        [InlineData(42)]
        [InlineData(0)]
        [InlineData(255)]
        public void ExecuteMOV_Tests(byte valA)
        {
            // Arrange
            var dest = TestHelpers.GetRandomRegister();
            var srcA = TestHelpers.GetRandomRegister();
            var args = new List<Argument> { new RegisterArgument(dest), new RegisterArgument(srcA) };
            var mov = new ExecuteMOV(args);
            var context = new CPUContext();
            context.Registers[srcA] = valA;
            ushort initialPC = context.ProgramCounter.Value;

            // Act
            mov.Execute(ref context);

            // Assert
            Assert.Equal(valA, context.Registers[dest]);
            Assert.Equal(valA == 0, context.ZeroFlag);
            Assert.False(context.CarryFlag);
            Assert.Equal(initialPC + 1, context.ProgramCounter.Value);
        }

        [Theory]
        [InlineData(Architecture.BRANCH_IF_ZERO_CODE, 42, 0, true, true)] // Zero true -> move, Zero false (42 != 0)
        [InlineData(Architecture.BRANCH_IF_ZERO_CODE, 42, 0, false, false)] // Zero false -> no move, Zero remains false even though dest=0
        [InlineData(Architecture.BRANCH_IF_NOT_ZERO_CODE, 0, 1, true, false)] // Zero true -> no move, Zero remains true even though dest=1
        [InlineData(Architecture.BRANCH_IF_NOT_ZERO_CODE, 0, 1, false, true)] // Zero false -> move, Zero true (0 == 0)
        [InlineData(Architecture.BRANCH_IF_CARRY_CODE, 0, 1, true, true)] // Carry true -> move, Zero true (0 == 0)
        [InlineData(Architecture.BRANCH_IF_CARRY_CODE, 0, 1, false, false)] // Carry false -> no move, Zero remains false even though dest=1? Wait, initialDest=1, but Zero initial false
        [InlineData(Architecture.BRANCH_IF_NOT_CARRY_CODE, 42, 0, true, false)] // Carry true -> no move, Zero remains false even though dest=0
        [InlineData(Architecture.BRANCH_IF_NOT_CARRY_CODE, 42, 0, false, true)] // Carry false -> move, Zero false (42 != 0)
        public void ExecuteMOVC_Tests(byte cond, byte sourceValue, byte initialDestValue, bool flagSet, bool shouldMove)
        {
            // Arrange
            (var source, var dest) = TestHelpers.GetTwoRandomDistinctRegisters();
            var args = new List<Argument> { new RegisterArgument(dest), new RegisterArgument(source), new NumberArgument(cond) };
            var movc = new ExecuteMOVC(args);
            var context = new CPUContext();
            context.Registers[source] = sourceValue;
            context.Registers[dest] = initialDestValue;
            context.ZeroFlag = false; // Initial Zero false
            if (cond == Architecture.BRANCH_IF_ZERO_CODE || cond == Architecture.BRANCH_IF_NOT_ZERO_CODE)
            {
                context.ZeroFlag = flagSet;
            }
            else
            {
                context.CarryFlag = flagSet;
            }
            bool initialZero = context.ZeroFlag;
            ushort initialPC = context.ProgramCounter.Value;

            // Act
            movc.Execute(ref context);

            // Assert
            if (shouldMove)
            {
                Assert.Equal(sourceValue, context.Registers[dest]);
                Assert.Equal(sourceValue == 0, context.ZeroFlag);
            }
            else
            {
                Assert.Equal(initialDestValue, context.Registers[dest]);
                Assert.Equal(initialZero, context.ZeroFlag);
            }
            Assert.Equal(initialPC + 1, context.ProgramCounter.Value);
        }

        [Theory]
        [InlineData(42, false)]
        [InlineData(0, true)]
        [InlineData(255, false)]
        public void ExecuteLDI_Tests(byte immediate, bool expectedZero)
        {
            // Arrange
            var dest = TestHelpers.GetRandomRegister();
            var args = new List<Argument> { new RegisterArgument(dest), new NumberArgument(immediate) };
            var ldi = new ExecuteLDI(args);
            var context = new CPUContext();
            ushort initialPC = context.ProgramCounter.Value;

            // Act
            ldi.Execute(ref context);

            // Assert
            Assert.Equal(immediate, context.Registers[dest]);
            Assert.Equal(expectedZero, context.ZeroFlag);
            Assert.Equal(initialPC + 1, context.ProgramCounter.Value);
        }

        [Theory]
        [InlineData(1, 2, 3, false)]
        [InlineData(100, 100, 200, false)]
        [InlineData(200, 100, 44, true)] // 200 + 100 = 300 -> 44 with carry
        [InlineData(0, 0, 0, false)]
        public void ExecuteADI_Tests(byte sourceValue, byte immediate, byte expected, bool expectedCarry)
        {
            // Arrange
            var source = TestHelpers.GetRandomRegister();
            var dest = TestHelpers.GetRandomRegister();
            var args = new List<Argument> { new RegisterArgument(dest), new RegisterArgument(source), new NumberArgument(immediate) };
            var adi = new ExecuteADI(args);
            var context = new CPUContext();
            context.Registers[source] = sourceValue;
            ushort initialPC = context.ProgramCounter.Value;

            // Act
            adi.Execute(ref context);

            // Assert
            Assert.Equal(expected, context.Registers[dest]);
            Assert.Equal(expected == 0, context.ZeroFlag);
            Assert.Equal(expectedCarry, context.CarryFlag);
            Assert.Equal(initialPC + 1, context.ProgramCounter.Value);
        }

        [Theory]
        [InlineData(5, 3, 2, true)]
        [InlineData(1, 2, 255, false)]
        [InlineData(0, 0, 0, true)]
        public void ExecuteSUBI_Tests(byte sourceValue, byte immediate, byte expected, bool expectedCarry)
        {
            // Arrange
            var source = TestHelpers.GetRandomRegister();
            var dest = TestHelpers.GetRandomRegister();
            var args = new List<Argument> { new RegisterArgument(dest), new RegisterArgument(source), new NumberArgument(immediate) };
            var subi = new ExecuteSUBI(args);
            var context = new CPUContext();
            context.Registers[source] = sourceValue;
            ushort initialPC = context.ProgramCounter.Value;

            // Act
            subi.Execute(ref context);

            // Assert
            Assert.Equal(expected, context.Registers[dest]);
            Assert.Equal(expected == 0, context.ZeroFlag);
            Assert.Equal(expectedCarry, context.CarryFlag);
            Assert.Equal(initialPC + 1, context.ProgramCounter.Value);
        }

        [Theory]
        [InlineData(42, 10)]
        [InlineData(0, 255)]
        public void ExecuteMST_Tests(byte sourceValue, byte address)
        {
            // Arrange
            var source = TestHelpers.GetRandomRegister();
            var args = new List<Argument> { new RegisterArgument(source), new NumberArgument(address) };
            var mst = new ExecuteMST(args);
            var context = new CPUContext();
            context.Registers[source] = sourceValue;
            ushort initialPC = context.ProgramCounter.Value;

            // Act
            mst.Execute(ref context);

            // Assert
            Assert.Equal(sourceValue, context.RAM[address]);
            Assert.Equal(initialPC + 1, context.ProgramCounter.Value);
        }

        [Theory]
        [InlineData(42, 10, 5)]
        [InlineData(0, 0, -1)]
        public void ExecuteMSP_Tests(byte sourceValue, byte pointerValue, sbyte offset)
        {
            // Arrange
            (var source, var pointer) = TestHelpers.GetTwoRandomDistinctRegisters();
            var args = new List<Argument> { new RegisterArgument(source), new RegisterArgument(pointer), new NumberArgument((byte)offset) };
            var msp = new ExecuteMSP(args);
            var context = new CPUContext();
            context.Registers[source] = sourceValue;
            context.Registers[pointer] = pointerValue;
            ushort initialPC = context.ProgramCounter.Value;

            // Act
            msp.Execute(ref context);

            // Assert
            byte address = (byte)(pointerValue - offset - 1);
            Assert.Equal(sourceValue, context.RAM[address]);
            Assert.Equal(initialPC + 1, context.ProgramCounter.Value);
        }

        [Theory]
        [InlineData(42, 10, 5)]
        [InlineData(0, 0, 0)]
        public void ExecuteMSS_Tests(byte sourceValue, byte spValue, byte addressOffset)
        {
            // Arrange
            var source = TestHelpers.GetRandomRegister();
            var args = new List<Argument> { new RegisterArgument(source), new NumberArgument(addressOffset) };
            var mss = new ExecuteMSS(args);
            var context = new CPUContext();
            context.Registers[source] = sourceValue;
            context.StackPointer.Increment(spValue);
            ushort initialPC = context.ProgramCounter.Value;

            // Act
            mss.Execute(ref context);

            // Assert
            byte address = (byte)(spValue - addressOffset - 1);
            Assert.Equal(sourceValue, context.RAM[address]);
            Assert.Equal(initialPC + 1, context.ProgramCounter.Value);
        }

        [Theory]
        [InlineData(42, 10, 20, 5)]
        [InlineData(0, 0, 0, 0)]
        public void ExecuteMSPS_Tests(byte sourceValue, byte pointerValue, byte spValue, sbyte offset)
        {
            // Arrange
            (var source, var pointer) = TestHelpers.GetTwoRandomDistinctRegisters();
            var args = new List<Argument> { new RegisterArgument(source), new RegisterArgument(pointer), new NumberArgument((byte)offset) };
            var msps = new ExecuteMSPS(args);
            var context = new CPUContext();
            context.Registers[source] = sourceValue;
            context.Registers[pointer] = pointerValue;
            context.StackPointer.Increment(spValue);
            ushort initialPC = context.ProgramCounter.Value;

            // Act
            msps.Execute(ref context);

            // Assert
            byte address = (byte)((spValue - offset - 1) - pointerValue - 1);
            Assert.Equal(sourceValue, context.RAM[address]);
            Assert.Equal(initialPC + 1, context.ProgramCounter.Value);
        }

        [Theory]
        [InlineData(10, 42)]
        [InlineData(0, 0)]
        public void ExecuteMLD_Tests(byte addr, byte memValue)
        {
            // Arrange
            var dest = TestHelpers.GetRandomRegister();
            var args = new List<Argument> { new RegisterArgument(dest), new NumberArgument(addr) };
            var mld = new ExecuteMLD(args);
            var context = new CPUContext();
            context.RAM[addr] = memValue;
            ushort initialPC = context.ProgramCounter.Value;

            // Act
            mld.Execute(ref context);

            // Assert
            Assert.Equal(memValue, context.Registers[dest]);
            Assert.Equal(memValue == 0, context.ZeroFlag);
            Assert.Equal(initialPC + 1, context.ProgramCounter.Value);
        }

        [Theory]
        [InlineData(5, 10, 42)]
        [InlineData(-1, 0, 10)]
        public void ExecuteMLP_Tests(sbyte offset, byte ptrVal, byte memValue)
        {
            // Arrange
            var dest = TestHelpers.GetRandomRegister();
            var ptr = TestHelpers.GetRandomRegister();
            var args = new List<Argument> { new RegisterArgument(dest), new RegisterArgument(ptr), new NumberArgument((byte)offset) };
            var mlp = new ExecuteMLP(args);
            var context = new CPUContext();
            context.Registers[ptr] = ptrVal;
            byte addr = (byte)(ptrVal - offset - 1);
            context.RAM[addr] = memValue;
            ushort initialPC = context.ProgramCounter.Value;

            // Act
            mlp.Execute(ref context);

            // Assert
            Assert.Equal(memValue, context.Registers[dest]);
            Assert.Equal(memValue == 0, context.ZeroFlag);
            Assert.Equal(initialPC + 1, context.ProgramCounter.Value);
        }

        [Theory]
        [InlineData(5, 10, 42)]
        [InlineData(0, 0, 0)]
        public void ExecuteMLS_Tests(byte addrOffset, byte spVal, byte memValue)
        {
            // Arrange
            var dest = TestHelpers.GetRandomRegister();
            var args = new List<Argument> { new RegisterArgument(dest), new NumberArgument(addrOffset) };
            var mls = new ExecuteMLS(args);
            var context = new CPUContext();
            context.StackPointer.Increment(spVal); // Set SP
            byte addr = (byte)(spVal - addrOffset - 1);
            context.RAM[addr] = memValue;
            ushort initialPC = context.ProgramCounter.Value;

            // Act
            mls.Execute(ref context);

            // Assert
            Assert.Equal(memValue, context.Registers[dest]);
            Assert.Equal(memValue == 0, context.ZeroFlag);
            Assert.Equal(initialPC + 1, context.ProgramCounter.Value);
        }

        [Theory]
        [InlineData(5, 10, 20, 42)]
        [InlineData(0, 0, 0, 0)]
        public void ExecuteMLPS_Tests(sbyte offset, byte ptrVal, byte spVal, byte memValue)
        {
            // Arrange
            var dest = TestHelpers.GetRandomRegister();
            var ptr = TestHelpers.GetRandomRegister();
            var args = new List<Argument> { new RegisterArgument(dest), new RegisterArgument(ptr), new NumberArgument((byte)offset) };
            var mlps = new ExecuteMLPS(args);
            var context = new CPUContext();
            context.Registers[ptr] = ptrVal;
            context.StackPointer.Increment(spVal); // Set SP
            byte addr = (byte)((spVal - offset - 1) - ptrVal - 1);
            context.RAM[addr] = memValue;
            ushort initialPC = context.ProgramCounter.Value;

            // Act
            mlps.Execute(ref context);

            // Assert
            Assert.Equal(memValue, context.Registers[dest]);
            Assert.Equal(memValue == 0, context.ZeroFlag);
            Assert.Equal(initialPC + 1, context.ProgramCounter.Value);
        }

        [Theory]
        [InlineData(42, 10)] // Push 42 at SP=10, SP becomes 11
        [InlineData(0, 0)]
        public void ExecutePSH_Tests(byte value, byte initialSP)
        {
            // Arrange
            var args = new List<Argument> { new NumberArgument(value) };
            var psh = new ExecutePSH(args);
            var context = new CPUContext();
            context.StackPointer.Increment(initialSP); // Set SP
            ushort initialPC = context.ProgramCounter.Value;

            // Act
            psh.Execute(ref context);

            // Assert
            Assert.Equal(value, context.RAM[initialSP]);
            Assert.Equal(initialSP + 1, context.StackPointer.Value);
            Assert.Equal(initialPC + 1, context.ProgramCounter.Value);
        }

        [Theory]
        [InlineData(42, 10)] // PushR =42 at SP=10, SP becomes 11
        [InlineData(0, 0)]
        public void ExecutePSHR_Tests(byte regValue, byte initialSP)
        {
            // Arrange
            var src = TestHelpers.GetRandomRegister();
            var args = new List<Argument> { new RegisterArgument(src) };
            var pshr = new ExecutePSHR(args);
            var context = new CPUContext();
            context.Registers[src] = regValue;
            context.StackPointer.Increment(initialSP); // Set SP
            ushort initialPC = context.ProgramCounter.Value;

            // Act
            pshr.Execute(ref context);

            // Assert
            Assert.Equal(regValue, context.RAM[initialSP]);
            Assert.Equal(initialSP + 1, context.StackPointer.Value);
            Assert.Equal(initialPC + 1, context.ProgramCounter.Value);
        }

        [Theory]
        [InlineData(5, 10)]
        [InlineData(0, 0)] // No change
        public void ExecutePOP_Tests(byte frameSize, byte initialSP)
        {
            // Arrange
            var args = new List<Argument> { new NumberArgument(frameSize) };
            var pop = new ExecutePOP(args);
            var context = new CPUContext();
            context.StackPointer.Increment(initialSP); // Set SP
            ushort initialPC = context.ProgramCounter.Value;

            // Act
            pop.Execute(ref context);

            // Assert
            Assert.Equal(initialSP - frameSize, context.StackPointer.Value);
            Assert.Equal(initialPC + 1, context.ProgramCounter.Value);
        }

        [Theory]
        [InlineData(5, 10)]
        [InlineData(0, 0)] // No change
        public void ExecutePSHM_Tests(byte frameSize, byte initialSP)
        {
            // Arrange
            var args = new List<Argument> { new NumberArgument(frameSize) };
            var pshm = new ExecutePSHM(args);
            var context = new CPUContext();
            context.StackPointer.Increment(initialSP); // Set SP
            ushort initialPC = context.ProgramCounter.Value;

            // Act
            pshm.Execute(ref context);

            // Assert
            Assert.Equal(initialSP + frameSize, context.StackPointer.Value);
            Assert.Equal(initialPC + 1, context.ProgramCounter.Value);
        }

        [Theory]
        [InlineData(100)]
        [InlineData(0)]
        public void ExecuteJMP_Tests(ushort address)
        {
            // Arrange
            var args = new List<Argument> { new AddressArgument(address) };
            var jmp = new ExecuteJMP(args);
            var context = new CPUContext();
            ushort initialPC = context.ProgramCounter.Value;

            // Act
            jmp.Execute(ref context);

            // Assert
            Assert.Equal(address, context.ProgramCounter.Value);
        }

        [Theory]
        [InlineData(Architecture.BRANCH_IF_ZERO_CODE, 100, true, true)] // Zero true -> branch
        [InlineData(Architecture.BRANCH_IF_ZERO_CODE, 100, false, false)] // Zero false -> no branch
        [InlineData(Architecture.BRANCH_IF_NOT_ZERO_CODE, 100, true, false)] // Zero true -> no branch
        [InlineData(Architecture.BRANCH_IF_NOT_ZERO_CODE, 100, false, true)] // Zero false -> branch
        [InlineData(Architecture.BRANCH_IF_CARRY_CODE, 100, true, true)] // Carry true -> branch
        [InlineData(Architecture.BRANCH_IF_CARRY_CODE, 100, false, false)] // Carry false -> no branch
        [InlineData(Architecture.BRANCH_IF_NOT_CARRY_CODE, 100, true, false)] // Carry true -> no branch
        [InlineData(Architecture.BRANCH_IF_NOT_CARRY_CODE, 100, false, true)] // Carry false -> branch
        public void ExecuteBRH_Tests(byte cond, ushort address, bool flagSet, bool shouldBranch)
        {
            // Arrange
            var args = new List<Argument> { new NumberArgument(cond), new AddressArgument(address) };
            var brh = new ExecuteBRH(args);
            var context = new CPUContext();
            ushort initialPC = context.ProgramCounter.Value;
            if (cond == Architecture.BRANCH_IF_ZERO_CODE || cond == Architecture.BRANCH_IF_NOT_ZERO_CODE)
            {
                context.ZeroFlag = flagSet;
            }
            else
            {
                context.CarryFlag = flagSet;
            }

            // Act
            brh.Execute(ref context, advancePC: false); // Test without advancePC as is for control flow instructions

            // Assert
            if (shouldBranch)
            {
                Assert.Equal(address, context.ProgramCounter.Value);
            }
            else
            {
                Assert.Equal(initialPC + 1, context.ProgramCounter.Value);
            }
        }

        [Theory]
        [InlineData(100, 10)] // Call to 100, push 11 (initialPC+1)
        public void ExecuteCAL_Tests(ushort address, ushort initialPC)
        {
            // Arrange
            var args = new List<Argument> { new AddressArgument(address) };
            var cal = new ExecuteCAL(args);
            var context = new CPUContext();
            context.ProgramCounter.SetBRH(initialPC); // Set PC

            // Act
            cal.Execute(ref context, advancePC: false);

            // Assert
            Assert.Equal(address, context.ProgramCounter.Value);
            context.ProgramCounter.PopRET(); // Pop to verify what was pushed
            Assert.Equal(initialPC + 1, context.ProgramCounter.Value);
        }

        [Theory]
        [InlineData(5, 100, 10, 10)] // Decrement SP by 5, pop to 10
        public void ExecuteRET_Tests(byte frameSize, ushort subRoutineAddr, byte initialSP, ushort initialPC)
        {
            // Arrange
            var args = new List<Argument> { new NumberArgument(frameSize) };
            var ret = new ExecuteRET(args);
            var context = new CPUContext();
            context.StackPointer.Increment(initialSP); // Set SP
            context.ProgramCounter.SetBRH(initialPC); // Set PC
            context.ProgramCounter.PushCAL((ushort)(subRoutineAddr)); // Push initialPC+1 to call stack

            // Act
            ret.Execute(ref context, advancePC: false);

            // Assert
            Assert.Equal(initialPC + 1, context.ProgramCounter.Value);
            Assert.Equal(initialSP - frameSize, context.StackPointer.Value);
        }

        [Theory]
        [InlineData(5, 42)]
        public void ExecutePST_Tests(byte portNumber, byte value)
        {
            // Arrange
            var src = TestHelpers.GetRandomRegister();
            var args = new List<Argument> { new RegisterArgument(src), new NumberArgument(portNumber) };
            var pst = new ExecutePST(args);
            var context = new CPUContext();
            var device = new BasicDevice();
            context.Ports.TryRegisterPort(portNumber, device);
            context.Registers[src] = value;
            ushort initialPC = context.ProgramCounter.Value;

            // Act
            pst.Execute(ref context);

            // Assert
            Assert.Equal(value, device.PortLoad());
            Assert.Equal(initialPC + 1, context.ProgramCounter.Value);
        }

        [Theory]
        [InlineData(5, 42, 24)] // Store to port 5 and 6
        public void ExecuteDPS_Tests(byte portNumber, byte valueA, byte valueB)
        {
            // Arrange
            var (srcA, srcB) = TestHelpers.GetTwoRandomDistinctRegisters();
            var args = new List<Argument> { new RegisterArgument(srcA), new RegisterArgument(srcB), new NumberArgument(portNumber) };
            var dps = new ExecuteDPS(args);
            var context = new CPUContext();
            var deviceA = new BasicDevice();
            var deviceB = new BasicDevice();
            context.Ports.TryRegisterPort(portNumber, deviceA);
            context.Ports.TryRegisterPort((byte)(portNumber + 1), deviceB);
            context.Registers[srcA] = valueA;
            context.Registers[srcB] = valueB;
            ushort initialPC = context.ProgramCounter.Value;

            // Act
            dps.Execute(ref context);

            // Assert
            Assert.Equal(valueA, deviceA.PortLoad());
            Assert.Equal(valueB, deviceB.PortLoad());
            Assert.Equal(initialPC + 1, context.ProgramCounter.Value);
        }

        [Theory]
        [InlineData(5, 42)]
        [InlineData(5, 0)]
        public void ExecutePLD_Tests(byte portNumber, byte value)
        {
            // Arrange
            var dest = TestHelpers.GetRandomRegister();
            var args = new List<Argument> { new RegisterArgument(dest), new NumberArgument(portNumber) };
            var pld = new ExecutePLD(args);
            var context = new CPUContext();
            var device = new BasicDevice();
            device.PortStore(value);
            context.Ports.TryRegisterPort(portNumber, device);
            ushort initialPC = context.ProgramCounter.Value;

            // Act
            pld.Execute(ref context);

            // Assert
            Assert.Equal(value, context.Registers[dest]);
            Assert.Equal(value == 0, context.ZeroFlag);
            Assert.Equal(initialPC + 1, context.ProgramCounter.Value);
        }
    }
}