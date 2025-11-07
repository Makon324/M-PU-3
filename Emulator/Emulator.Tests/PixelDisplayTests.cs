namespace Emulator.Tests
{
    public class PixelDisplayTests
    {
        private CPUContext CreateContext()
        {
            return new CPUContext();
        }

        private PixelDisplay CreateDisplay(ref CPUContext context, byte basePort = 0)
        {
            return new PixelDisplay(ref context, basePort);
        }

        [Fact]
        public void Constructor_ThrowsArgumentOutOfRangeException_WhenBasePortTooHigh()
        {
            var context = CreateContext();
            byte invalidBasePort = (byte)(Architecture.IO_PORT_COUNT - 4 + 1);
            Assert.Throws<ArgumentOutOfRangeException>(() => CreateDisplay(ref context, invalidBasePort));
        }

        [Fact]
        public void Constructor_ThrowsInvalidOperationException_WhenPortRegistrationFails()
        {
            var context = CreateContext();
            byte basePort = 0;
            // Force failure by registering something first
            var dummyDevice = new BasicDevice();
            context.Ports.TryRegisterPort(basePort, dummyDevice);
            Assert.Throws<InvalidOperationException>(() => CreateDisplay(ref context, basePort));
        }

        [Fact]
        public void Constructor_RegistersRGBAndXYPortsSuccessfully()
        {
            var context = CreateContext();
            byte basePort = 0;
            var display = CreateDisplay(ref context, basePort);

            Assert.NotNull(context.Ports[basePort]);     // R
            Assert.NotNull(context.Ports[basePort + 1]); // G
            Assert.NotNull(context.Ports[basePort + 2]); // B
            Assert.NotNull(context.Ports[basePort + 3]); // X
            Assert.NotNull(context.Ports[basePort + 4]); // Y
        }

        [Fact]
        public void PortStore_RGB_SetsValuesCorrectly()
        {
            var context = CreateContext();
            byte basePort = 0;
            var display = CreateDisplay(ref context, basePort);

            context.Ports[basePort]!.PortStore(210);    // R
            context.Ports[basePort + 1]!.PortStore(10); // G
            context.Ports[basePort + 2]!.PortStore(0);  // B

            Assert.Equal(210, context.Ports[basePort]!.PortLoad());
            Assert.Equal(10, context.Ports[basePort + 1]!.PortLoad());
            Assert.Equal(0, context.Ports[basePort + 2]!.PortLoad());
        }

        [Fact]
        public void PortStore_XY_WithoutHighBit_UpdatesCoordinatesButDoesNotSetPixel()
        {
            var context = CreateContext();
            byte basePort = 0;
            var display = CreateDisplay(ref context, basePort);

            // Set RGB (but won't be used yet)
            context.Ports[basePort]!.PortStore(255);
            context.Ports[basePort + 1]!.PortStore(128);
            context.Ports[basePort + 2]!.PortStore(64);

            // Set X without high bit
            context.Ports[basePort + 3]!.PortStore(5); // X=5, no set

            // Set Y without high bit
            context.Ports[basePort + 4]!.PortStore(10); // Y=10, no set

            Assert.Equal(5, context.Ports[basePort + 3]!.PortLoad());
            Assert.Equal(10, context.Ports[basePort + 4]!.PortLoad());

            // Check pixel not set (should be default 0,0,0)
            var pixel = display.GetPixel(5, 10);
            Assert.Equal(0, pixel.Red);
            Assert.Equal(0, pixel.Green);
            Assert.Equal(0, pixel.Blue);
        }

        [Fact]
        public void PortStore_X_WithHighBit_SetsCoordinateAndPixel()
        {
            var context = CreateContext();
            byte basePort = 0;
            var display = CreateDisplay(ref context, basePort);

            // Set RGB
            context.Ports[basePort]!.PortStore(255);
            context.Ports[basePort + 1]!.PortStore(128);
            context.Ports[basePort + 2]!.PortStore(64);

            // Set Y first
            context.Ports[basePort + 4]!.PortStore(10); // Y=10, no set

            // Set X with high bit
            context.Ports[basePort + 3]!.PortStore((byte)(5 | 128)); // X=5, set pixel

            Assert.Equal(5, context.Ports[basePort + 3]!.PortLoad());
            Assert.Equal(10, context.Ports[basePort + 4]!.PortLoad());

            // Check pixel set
            var pixel = display.GetPixel(5, 10);
            Assert.Equal(255, pixel.Red);
            Assert.Equal(128, pixel.Green);
            Assert.Equal(64, pixel.Blue);
        }

        [Fact]
        public void PortStore_Y_WithHighBit_SetsCoordinateAndPixel()
        {
            var context = CreateContext();
            byte basePort = 0;
            var display = CreateDisplay(ref context, basePort);

            // Set RGB
            context.Ports[basePort]!.PortStore(255);
            context.Ports[basePort + 1]!.PortStore(128);
            context.Ports[basePort + 2]!.PortStore(64);

            // Set X first
            context.Ports[basePort + 3]!.PortStore(5); // X=5, no set

            // Set Y with high bit
            context.Ports[basePort + 4]!.PortStore((byte)(10 | 128)); // Y=10, set pixel

            Assert.Equal(5, context.Ports[basePort + 3]!.PortLoad());
            Assert.Equal(10, context.Ports[basePort + 4]!.PortLoad());

            // Check pixel set
            var pixel = display.GetPixel(5, 10);
            Assert.Equal(255, pixel.Red);
            Assert.Equal(128, pixel.Green);
            Assert.Equal(64, pixel.Blue);
        }

        [Fact]
        public void PortLoad_XY_ReturnsCurrentCoordinates()
        {
            var context = CreateContext();
            byte basePort = 0;
            var display = CreateDisplay(ref context, basePort);

            // Set X and Y
            context.Ports[basePort + 3]!.PortStore(5);
            context.Ports[basePort + 4]!.PortStore(10);

            Assert.Equal(5, context.Ports[basePort + 3]!.PortLoad());
            Assert.Equal(10, context.Ports[basePort + 4]!.PortLoad());
        }

        [Fact]
        public void SetPixel_UpdatesGridWithCurrentRGB()
        {
            var context = CreateContext();
            byte basePort = 0;
            var display = CreateDisplay(ref context, basePort);

            // Set RGB first
            context.Ports[basePort]!.PortStore(100);
            context.Ports[basePort + 1]!.PortStore(150);
            context.Ports[basePort + 2]!.PortStore(200);

            // Set X and Y with high bit on Y
            context.Ports[basePort + 3]!.PortStore(20);
            context.Ports[basePort + 4]!.PortStore((byte)(30 | (1 << 7)));

            var pixel = display.GetPixel(20, 30);
            Assert.Equal(100, pixel.Red);
            Assert.Equal(150, pixel.Green);
            Assert.Equal(200, pixel.Blue);

            // Change RGB and set another pixel
            context.Ports[basePort]!.PortStore(50);
            context.Ports[basePort + 1]!.PortStore(60);
            context.Ports[basePort + 2]!.PortStore(70);

            context.Ports[basePort + 3]!.PortStore((byte)(40 | (1 << 7))); // Set with X

            pixel = display.GetPixel(40, 30); // Y still 30
            Assert.Equal(50, pixel.Red);
            Assert.Equal(60, pixel.Green);
            Assert.Equal(70, pixel.Blue);
        }
    }
}
