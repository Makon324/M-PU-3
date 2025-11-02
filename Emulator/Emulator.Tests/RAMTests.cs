namespace Emulator.Tests
{
    public class RAMTests
    {
        private readonly RAM _ram;

        public RAMTests()
        {
            // Initialize RAM before each test
            _ram = new RAM();
        }

        [Theory]
        [InlineData(0)]
        [InlineData(127)]
        [InlineData(255)]
        public void ByteIndexer_VariousValidIndices_GetSetWorks(byte index)
        {
            _ram[index] = 0xAA;
            Assert.Equal(0xAA, _ram[index]);
        }

        [Fact]
        public void IndexStruct_FromStart_GetSetWorks()
        {
            // Test Index from start
            Index fromStart = 5;
            _ram[fromStart] = 0xCD;
            Assert.Equal(0xCD, _ram[fromStart]);
        }

        [Fact]
        public void IndexStruct_FromEnd_GetSetWorks()
        {
            // Test Index from end
            Index fromEnd = ^5;
            byte expectedIndex = (byte)(Architecture.RAM_SIZE - 5);

            _ram[expectedIndex] = 0xEF;
            Assert.Equal(0xEF, _ram[fromEnd]);
        }

        [Fact]
        public void RangeIndexer_ValidRange_ReturnsCorrectSlice()
        {
            // Initialize specific indices
            _ram[5] = 0x01;
            _ram[6] = 0x02;
            _ram[7] = 0x03;

            // Test range
            byte[] slice = _ram[5..8];
            Assert.Equal(new byte[] { 0x01, 0x02, 0x03 }, slice);
        }

        [Fact]
        public void RangeIndexer_DoesNotAffectOriginalMemory()
        {
            _ram[0] = 0x01;
            _ram[1] = 0x02;

            var slice = _ram[0..2];
            slice[0] = 0xFF; // Modify the returned array

            // Original memory should be unchanged
            Assert.Equal(0x01, _ram[0]);
            Assert.Equal(0x02, _ram[1]);
        }

        [Fact]
        public void GetAllMemory_ReturnsClone()
        {
            // Modify original memory
            _ram[0] = 0xAA;
            byte[] clone = _ram.GetAllMemory();

            // Modify the clone
            clone[0] = 0xBB;

            // Original should remain unchanged
            Assert.Equal(0xAA, _ram[0]);
        }

        [Fact]
        public void ByteIndexer_IndexOutOfRange_ThrowsException()
        {
            Index outOfRangeIndex = 256;
            Assert.Throws<ArgumentOutOfRangeException>(() => _ram[outOfRangeIndex]);
            Assert.Throws<ArgumentOutOfRangeException>(() => _ram[outOfRangeIndex] = 0xFF);
        }

        [Fact]
        public void RangeIndexer_InvalidRange_ThrowsException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _ram[1000..2000]);
        }
    }
}
