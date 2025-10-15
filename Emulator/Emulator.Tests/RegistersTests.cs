using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulator.Tests
{
    public class RegistersTests
    {
        private readonly RegisterCollection _registers;

        public RegistersTests()
        {
            _registers = new RegisterCollection();
        }

        [Theory]
        [InlineData(Register.R1)]
        [InlineData(Register.R2)]
        [InlineData(Register.R3)]
        [InlineData(Register.R4)]
        [InlineData(Register.R5)]
        [InlineData(Register.R6)]
        [InlineData(Register.R7)]
        public void RegisterIndexer_VariousRegisters_GetSetWorks(Register register)
        {
            _registers[register] = 0xAA;
            Assert.Equal(0xAA, _registers[register]);
        }

        [Fact]
        public void RegisterIndexer_R0_AlwaysReturnsZeroAndIgnoresWrites()
        {
            Assert.Equal(0, _registers[Register.R0]);

            _registers[Register.R0] = 0xFF;
            Assert.Equal(0, _registers[Register.R0]);
        }

        [Fact]
        public void IndexStruct_FromStart_GetSetWorks()
        {
            Index fromStart = 1;
            _registers[fromStart] = 0xCD;
            Assert.Equal(0xCD, _registers[fromStart]);
        }

        [Fact]
        public void IndexStruct_FromEnd_GetSetWorks()
        {
            // Test Index from end (^1 = R7, since there are 8 registers)
            Index fromEnd = ^1;
            _registers[Register.R7] = 0xEF;
            Assert.Equal(0xEF, _registers[fromEnd]);
        }

        [Fact]
        public void RangeIndexer_ValidRange_ReturnsCorrectSlice()
        {
            // Set some register values
            _registers[Register.R1] = 0x01;
            _registers[Register.R2] = 0x02;

            // Test range that includes R0 (should return 0 for R0)
            byte[] slice = _registers[0..3];
            Assert.Equal(new byte[] { 0, 0x01, 0x02 }, slice);
        }

        [Fact]
        public void RangeIndexer_DoesNotAffectOriginalRegisters()
        {
            _registers[Register.R1] = 0x01;
            _registers[Register.R2] = 0x02;

            var slice = _registers[1..3];
            slice[0] = 0xFF; // Modify the returned array

            // Original registers should be unchanged
            Assert.Equal(0x01, _registers[Register.R1]);
            Assert.Equal(0x02, _registers[Register.R2]);
        }

        [Fact]
        public void GetAllRegisters_ReturnsClone()
        {
            // Modify some registers
            _registers[Register.R1] = 0xAA;
            _registers[Register.R2] = 0xBB;
            byte[] clone = _registers.GetAllRegisters();

            // Modify the clone
            clone[1] = 0xCC;
            clone[2] = 0xDD;

            // Original should remain unchanged
            Assert.Equal(0xAA, _registers[Register.R1]);
            Assert.Equal(0xBB, _registers[Register.R2]);
        }

        [Fact]
        public void ByteIndexer_IndexOutOfRange_ThrowsException()
        {
            Index outOfRangeIndex = 8; // Only 0-7 are valid
            Assert.Throws<ArgumentOutOfRangeException>(() => _registers[outOfRangeIndex]);
            Assert.Throws<ArgumentOutOfRangeException>(() => _registers[outOfRangeIndex] = 0xFF);
        }

        [Fact]
        public void RangeIndexer_InvalidRange_ThrowsException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _registers[10..20]);
        }
    }
}
