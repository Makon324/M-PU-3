using System;
using System.Threading.Tasks;
using Xunit;

namespace Emulator.Tests
{
    public class BuiltInDevicesTests
    {
        [Fact]
        public void Multiplier_RegistersPortsCorrectly()
        {
            var context = new CPUContext();
            var multiplier = new Multiplier(context, 0);
            Assert.NotNull(context.Ports[0]);
            Assert.NotNull(context.Ports[1]);
        }

        [Fact]
        public void Multiplier_ThrowsIfBasePortTooHigh()
        {
            var context = new CPUContext();
            Assert.Throws<ArgumentOutOfRangeException>(() => new Multiplier(context, (byte)(Architecture.IO_PORT_COUNT - 1)));
        }

        [Fact]
        public void Multiplier_ThrowsIfPortAlreadyRegistered()
        {
            var context = new CPUContext();
            context.Ports.TryRegisterPort(0, new BasicDevice());
            Assert.Throws<InvalidOperationException>(() => new Multiplier(context, 0));
        }

        [Theory]
        [InlineData(0, 0, 0, 0)]
        [InlineData(1, 1, 1, 0)]
        [InlineData(2, 3, 6, 0)]
        [InlineData(255, 255, 1, 254)]
        [InlineData(100, 200, 32, 78)] // 100*200=20000, 20000=0x4E20, low=0x20=32, high=0x4E=78
        public void Multiplier_ComputesProductCorrectly(byte a, byte b, byte low, byte high)
        {
            var context = new CPUContext();
            var multiplier = new Multiplier(context, 0);
            context.Ports[0]!.PortStore(a);
            context.Ports[1]!.PortStore(b);
            Assert.Equal(low, context.Ports[0]!.PortLoad());
            Assert.Equal(high, context.Ports[1]!.PortLoad());
        }

        [Fact]
        public void Divider_RegistersPortsCorrectly()
        {
            var context = new CPUContext();
            var divider = new Divider(context, 0);
            Assert.NotNull(context.Ports[0]);
            Assert.NotNull(context.Ports[1]);
        }

        [Fact]
        public void Divider_ThrowsIfBasePortTooHigh()
        {
            var context = new CPUContext();
            Assert.Throws<ArgumentOutOfRangeException>(() => new Divider(context, (byte)(Architecture.IO_PORT_COUNT - 1)));
        }

        [Fact]
        public void Divider_ThrowsIfPortAlreadyRegistered()
        {
            var context = new CPUContext();
            context.Ports.TryRegisterPort(0, new BasicDevice());
            Assert.Throws<InvalidOperationException>(() => new Divider(context, 0));
        }

        [Theory]
        [InlineData(0, 0, 255, 0)]
        [InlineData(0, 5, 255, 5)]
        [InlineData(1, 0, 0, 0)]
        [InlineData(2, 5, 2, 1)]
        [InlineData(255, 255, 1, 0)]
        [InlineData(10, 100, 10, 0)]
        [InlineData(3, 10, 3, 1)]
        public void Divider_ComputesDivisionCorrectly(byte divisor, byte dividend, byte quotient, byte remainder)
        {
            var context = new CPUContext();
            var divider = new Divider(context, 0);
            context.Ports[0]!.PortStore(divisor);
            context.Ports[1]!.PortStore(dividend);
            Assert.Equal(quotient, context.Ports[0]!.PortLoad());
            Assert.Equal(remainder, context.Ports[1]!.PortLoad());
        }

        [Fact]
        public void RNG_LoadReturnsByteInRange()
        {
            var rng = new RNG();
            for (int i = 0; i < 50; i++) // Test multiple times for consistency in range
            {
                var value = rng.PortLoad();
                Assert.InRange(value, 0, 255);
            }
        }

        [Fact]
        public void Timer_RegistersPortsCorrectly()
        {
            var context = new CPUContext();
            var timer = new Timer(context, 0);
            for (int i = 0; i < 4; i++)
            {
                Assert.NotNull(context.Ports[i]);
            }
        }

        [Fact]
        public void Timer_ThrowsIfBasePortTooHigh()
        {
            var context = new CPUContext();
            Assert.Throws<ArgumentOutOfRangeException>(() => new Timer(context, (byte)(Architecture.IO_PORT_COUNT - 3)));
        }

        [Fact]
        public void Timer_ThrowsIfPortAlreadyRegistered()
        {
            var context = new CPUContext();
            context.Ports.TryRegisterPort(0, new BasicDevice());
            Assert.Throws<InvalidOperationException>(() => new Timer(context, 0));
        }

        [Fact]
        public async Task Timer_IncreasesOverTime()
        {
            var context = new CPUContext();
            var timer = new Timer(context, 0);
            uint initial = ReadTimerValue(context);
            await Task.Delay(100);
            uint after = ReadTimerValue(context);
            Assert.True(after > initial);
        }

        private uint ReadTimerValue(CPUContext context)
        {
            uint value = 0;
            for (int i = 0; i < 4; i++)
            {
                value |= (uint)context.Ports[i]!.PortLoad() << (i * 8);
            }
            return value;
        }
    }
}
