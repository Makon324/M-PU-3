using System.Diagnostics;
using SDL2;

namespace Emulator
{
    struct Pixel
    {
        public byte Red;
        public byte Green;
        public byte Blue;
    }

    /// <remarks>Used to make one PointXY class instead of 2.</remarks>
    public enum PointIndex : byte
    {
        X = 0,
        Y = 1
    }

    /// <summary>
    /// RGB pixel grid controlled via I/O ports (R, G, B + X/Y). Renders to a native window.
    /// </summary>
    internal sealed class PixelDisplay
    {
        private readonly BasicDevice[] _RGBports = new BasicDevice[3];

        private readonly Pixel[,] _grid = new Pixel[Architecture.DISPLAY_SIZE.Width, Architecture.DISPLAY_SIZE.Height];

        private byte _X;
        private byte _Y;

        private long _lastRefreshTimestamp = 0;

        private bool _isWindowOpen = false;

        private void SetPixel()
        {
            _grid[_X, _Y] = new Pixel
            {
                Red = _RGBports[0].PortLoad(),
                Green = _RGBports[1].PortLoad(),
                Blue = _RGBports[2].PortLoad()
            };

            RefreshWindow();
        }

        private void RefreshWindow()
        {
            if (!_isWindowOpen)
            {
                _isWindowOpen = true;

                // start window
            }

            long now = Stopwatch.GetTimestamp();
            if (now - _lastRefreshTimestamp >= Stopwatch.Frequency / Architecture.DISPLAY_HZ_FREQUENCY)
            {
                _lastRefreshTimestamp = now;

                // render grid to window
            }
            else
            {
                return;
            }
        }

        public PixelDisplay(ref CPUContext context, byte basePort)
        {
            if (basePort >= Architecture.IO_PORT_COUNT - 4)
                throw new ArgumentOutOfRangeException(nameof(basePort),
                    $"Base port must be <= {Architecture.IO_PORT_COUNT - 4} to allow four consecutive ports.");

            bool registered = true;

            // Register RGB ports
            for (int i = 0; i < 3; i++)
            {
                _RGBports[i] = new BasicDevice();
                registered &= context.Ports.TryRegisterPort((byte)(basePort + i), _RGBports[i]);
            }

            // Register X, Y ports
            foreach (PointIndex pointIndex in Enum.GetValues<PointIndex>())
            {
                var portXY = new PortXY(this, pointIndex);
                registered &= context.Ports.TryRegisterPort(basePort + 3 + (byte)pointIndex, portXY);
            }

            if (!registered)
                throw new InvalidOperationException($"Failed to register PixelDisplay ports at {basePort} to {basePort + 3}.");
        }

        private sealed class PortXY : IOPort
        {
            private readonly PixelDisplay _display;
            private readonly PointIndex _pointIndex;

            public PortXY(PixelDisplay display, PointIndex pointIndex)
            {
                _display = display;
                _pointIndex = pointIndex;
            }

            /// <summary>
            /// Updates the display coordinate and optionally sets a pixel based on the provided value.
            /// </summary>
            /// <remarks>The method updates either the X or Y coordinate of the display, depending on
            /// point index. If the highest bit of the value is 1, the method will also trigger the
            /// display to set a pixel at the specified coordinates to current RGB ports value.</remarks>
            /// <param name="value">A byte value where the lower 7 bits represent the coordinate to be updated, and the highest bit
            /// indicates whether to set a pixel on the display.</param>
            public void PortStore(byte value)
            {
                byte maskedValue = (byte)(value & ((1 << 7) - 1));

                if (_pointIndex == PointIndex.X)
                {
                    if (maskedValue >= Architecture.DISPLAY_SIZE.Width)
                        throw new ArgumentOutOfRangeException(nameof(value),
                            $"X coordinate {maskedValue} is outside valid range 0..{Architecture.DISPLAY_SIZE.Width - 1}.");

                    _display._X = maskedValue;
                }
                else // _pointIndex == PointIndex.Y
                {
                    if (maskedValue >= Architecture.DISPLAY_SIZE.Height)
                        throw new ArgumentOutOfRangeException(nameof(value),
                            $"Y coordinate {maskedValue} is outside valid range 0..{Architecture.DISPLAY_SIZE.Height - 1}.");

                    _display._Y = maskedValue;
                }

                if ((value & (1 << 7)) != 0)
                {
                    _display.SetPixel();
                }
            }

            /// <summary>
            /// Loads the current value of the X or Y coordinate.
            /// </summary>
            public byte PortLoad()
            {
                if (_pointIndex == PointIndex.X)
                {
                    return (byte)_display._X;
                }
                else // _pointIndex == PointIndex.Y
                {
                    return (byte)_display._Y;
                }
            }
        }
    }

    
}
