namespace Emulator.Tests
{
    public class StackPointerTests
    {
        [Theory]
        [InlineData(0)]
        [InlineData(127)]
        [InlineData(255)]
        public void Constructor_WithInitialValue_SetsCorrectValue(byte initialValue)
        {
            var sp = new StackPointer(initialValue);
            Assert.Equal(initialValue, sp.Value);
        }

        [Theory]
        [InlineData(0, 1, 1)]
        [InlineData(10, 5, 15)]
        [InlineData(100, 50, 150)]
        [InlineData(254, 1, 255)]
        public void Increment_WithValidFrameSize_UpdatesValueCorrectly(byte initialValue, byte frameSize, byte expectedValue)
        {
            var sp = new StackPointer(initialValue);
            sp.Increment(frameSize);
            Assert.Equal(expectedValue, sp.Value);
        }

        [Fact]
        public void Increment_DefaultFrameSize_IncrementsByOne()
        {
            var sp = new StackPointer(10);
            sp.Increment(); // Should use default frameSize = 1
            Assert.Equal(11, sp.Value);
        }

        [Theory]
        [InlineData(255, 1)]
        [InlineData(255, 10)]
        [InlineData(200, 100)]
        public void Increment_WithOverflow_ThrowsException(byte initialValue, byte frameSize)
        {
            var sp = new StackPointer(initialValue);
            Assert.Throws<InvalidOperationException>(() => sp.Increment(frameSize));
        }

        [Theory]
        [InlineData(10, 1, 9)]
        [InlineData(50, 25, 25)]
        [InlineData(100, 100, 0)]
        [InlineData(255, 50, 205)]
        public void Decrement_WithValidFrameSize_UpdatesValueCorrectly(byte initialValue, byte frameSize, byte expectedValue)
        {
            var sp = new StackPointer(initialValue);
            sp.Decrement(frameSize);
            Assert.Equal(expectedValue, sp.Value);
        }

        [Theory]
        [InlineData(0, 1)]
        [InlineData(5, 10)]
        [InlineData(99, 100)]
        public void Decrement_WithUnderflow_ThrowsException(byte initialValue, byte frameSize)
        {
            var sp = new StackPointer(initialValue);
            Assert.Throws<InvalidOperationException>(() => sp.Decrement(frameSize));
        }

        [Fact]
        public void MultipleOperations_WorkCorrectlyInSequence()
        {
            var sp = new StackPointer(100);

            sp.Increment(25);
            Assert.Equal(125, sp.Value);

            sp.Decrement(50);
            Assert.Equal(75, sp.Value);

            sp.Increment(10);
            Assert.Equal(85, sp.Value);

            sp.Decrement(85);
            Assert.Equal(0, sp.Value);
        }
    }
}
