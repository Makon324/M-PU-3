namespace Emulator.Tests
{
    public class ProgramCounterTests
    {
        [Theory]
        [InlineData(0)]
        [InlineData(100)]
        [InlineData(Architecture.MAX_PROGRAM_SIZE - 1)]
        public void Constructor_WithInitialAddress_SetsCorrectValue(ushort initialAddress)
        {
            var pc = new ProgramCounter(initialAddress);
            Assert.Equal(initialAddress, pc.Value);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(100)]
        [InlineData(Architecture.MAX_PROGRAM_SIZE - 2)]
        public void Increment_ValidCounter_IncrementsCorrectly(ushort initialValue)
        {
            var pc = new ProgramCounter(initialValue);
            pc.Increment();
            Assert.Equal(initialValue + 1, pc.Value);
        }

        [Fact]
        public void Increment_AtMaxProgramSize_ThrowsException()
        {
            var pc = new ProgramCounter(Architecture.MAX_PROGRAM_SIZE - 1);

            var exception = Assert.Throws<InvalidOperationException>(() => pc.Increment());
            Assert.Contains("Program counter overflow", exception.Message);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(100)]
        [InlineData(Architecture.MAX_PROGRAM_SIZE - 1)]
        public void SetBRH_ValidAddress_SetsCorrectValue(ushort address)
        {
            var pc = new ProgramCounter();
            pc.SetBRH(address);
            Assert.Equal(address, pc.Value);
        }

        [Fact]
        public void SetBRH_AddressOutOfBounds_ThrowsException()
        {
            var pc = new ProgramCounter();
            ushort invalidAddress = Architecture.MAX_PROGRAM_SIZE;

            var exception = Assert.Throws<ArgumentOutOfRangeException>(() => pc.SetBRH(invalidAddress));
            Assert.Equal("address", exception.ParamName);
            Assert.Contains("Address is out of bounds", exception.Message);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(100)]
        [InlineData(Architecture.MAX_PROGRAM_SIZE - 2)]
        public void PushCAL_ValidAddress_PushesReturnAddressAndJumps(ushort callAddress)
        {
            var pc = new ProgramCounter(0x100);
            pc.PushCAL(callAddress);
            Assert.Equal(callAddress, pc.Value);
        }

        [Fact]
        public void PushCAL_AddressOutOfBounds_ThrowsException()
        {
            var pc = new ProgramCounter(0x100);
            ushort invalidAddress = Architecture.MAX_PROGRAM_SIZE;

            var exception = Assert.Throws<ArgumentOutOfRangeException>(() => pc.PushCAL(invalidAddress));
            Assert.Equal("address", exception.ParamName);
            Assert.Contains("Address is out of bounds", exception.Message);
        }

        [Fact]
        public void PushCAL_NextAddressWouldOverflow_ThrowsException()
        {
            var pc = new ProgramCounter(Architecture.MAX_PROGRAM_SIZE - 1);

            var exception = Assert.Throws<InvalidOperationException>(() => pc.PushCAL(0x200));
            Assert.Contains("Can't push invalid adress onto stack", exception.Message);
        }

        [Fact]
        public void PushCAL_And_PopRET_WorkTogetherCorrectly()
        {
            var pc = new ProgramCounter(0x100);
            ushort callAddress = 0x200;

            pc.PushCAL(callAddress);
            Assert.Equal(callAddress, pc.Value);

            pc.PopRET();
            Assert.Equal(0x101, pc.Value);
        }

        [Fact]
        public void PushCAL_MultipleCalls_MaintainsCallStack()
        {
            var pc = new ProgramCounter(0x10);

            // Multiple nested calls
            pc.PushCAL(0x20); // First call
            Assert.Equal(0x20, pc.Value);

            pc.Increment(); // Simulate some instructions

            pc.PushCAL(0x30); // Second call
            Assert.Equal(0x30, pc.Value);

            pc.Increment(); // Simulate some instructions

            pc.PushCAL(0x40); // Third call
            Assert.Equal(0x40, pc.Value);

            // Return in reverse order
            pc.PopRET(); // Return from third call
            Assert.Equal(0x32, pc.Value);

            pc.PopRET(); // Return from second call
            Assert.Equal(0x22, pc.Value);

            pc.PopRET(); // Return from first call
            Assert.Equal(0x11, pc.Value);
        }

        [Fact]
        public void PopRET_EmptyCallStack_ThrowsException()
        {
            var pc = new ProgramCounter();
            var exception = Assert.Throws<InvalidOperationException>(() => pc.PopRET());
            Assert.Contains("Call stack is empty", exception.Message);
        }
    }
}