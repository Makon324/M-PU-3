using SFML.Graphics;
using SFML.System;
using SFML.Window;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Emulator
{
    internal struct Pixel
    {
        public byte Red;
        public byte Green;
        public byte Blue;
    }

    /// <summary>
    /// RGB pixel grid controlled via I/O ports (R, G, B + X/Y). Renders to a native window.
    /// </summary>
    internal sealed class PixelDisplay
    {
        private readonly BasicDevice[] _RGBports = new BasicDevice[3];

        private readonly Pixel[,] _grid = new Pixel[Architecture.DISPLAY_SIZE.Height, Architecture.DISPLAY_SIZE.Width];

        private byte _X;
        private byte _Y;

        private SFMLRenderer _renderer;

        /// <summary>
        /// Retrieves the pixel at specified coordinates. Used for testing.
        /// </summary>
        internal Pixel GetPixel(int x, int y)
        {
            return _grid[y, x];
        }

        private void SetPixel()
        {
            _grid[_Y, _X] = new Pixel
            {
                Red = _RGBports[0].PortLoad(),
                Green = _RGBports[1].PortLoad(),
                Blue = _RGBports[2].PortLoad()
            };

            _renderer.UpdateGrid();
        }

        public PixelDisplay(CPUContext context, byte basePort)
        {
            if (basePort >= Architecture.IO_PORT_COUNT - 4)
                throw new ArgumentOutOfRangeException(nameof(basePort),
                    $"Base port must be <= {Architecture.IO_PORT_COUNT - 4} to allow four consecutive ports.");

            _renderer = new SFMLRenderer(_grid);

            bool registered = true;

            // Register RGB ports
            for (int i = 0; i < 3; i++)
            {
                _RGBports[i] = new BasicDevice();
                registered &= context.Ports.TryRegisterPort((byte)(basePort + i), _RGBports[i]);
            }

            // Register X, Y ports
            foreach (PortXY.PointIndex pointIndex in Enum.GetValues<PortXY.PointIndex>())
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

            internal enum PointIndex : byte
            {
                X = 0,
                Y = 1
            }

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

            public byte Value
            {
                get
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

            /// <summary>
            /// Loads the current value of the X or Y coordinate.
            /// </summary>
            public byte PortLoad()
            {
                return Value;
            }
        }
    }

    /// <summary>
    /// Renders a pixel grid to an SFML window.
    /// </summary>
    internal sealed class SFMLRenderer : IDisposable
    {
        private RenderWindow? _window;
        private Texture? _texture;
        private Sprite? _sprite;

        private bool _isOpen = false;

        private Pixel[,]? _grid = null;

        private int _currentWidth;
        private int _currentHeight;
        private double _aspectRatio;

        private readonly byte[] _pixelsBuffer = new byte[Architecture.DISPLAY_SIZE.Width * Architecture.DISPLAY_SIZE.Height * 4]; // RGBA

        public bool IsOpen => _isOpen;

        private long _lastRefreshTimestamp = 0;

        private Thread? _renderThread;
        private readonly object _syncLock = new object();
        private bool _needsRender = false;  // Flag for when grid updates

        // P/Invoke for Windows API to modify window styles
        private const int GWL_STYLE = -16;
        private const int WS_MAXIMIZEBOX = 0x10000;

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
        private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
        private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        public SFMLRenderer(Pixel[,] grid)
        {
            _grid = grid;  // grid reference
        }

        private void Start()
        {
            if (_isOpen) return;

            _isOpen = true;
            _aspectRatio = Architecture.DISPLAY_SIZE.Width / (double)Architecture.DISPLAY_SIZE.Height;
            _currentWidth = Architecture.DISPLAY_SIZE.Width;
            _currentHeight = Architecture.DISPLAY_SIZE.Height;

            _renderThread = new Thread(RenderLoop);
            _renderThread.IsBackground = true;  // Ensures it exits with app
            _renderThread.Start();
        }

        private void RenderLoop()
        {
            // Create window and resources on this thread
            _window = new RenderWindow(new VideoMode((uint)Architecture.DISPLAY_SIZE.Width, (uint)Architecture.DISPLAY_SIZE.Height), "Pixel Display", Styles.Default);
            _window.SetActive(true);

            // Remove maximize button (Windows-specific)
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                IntPtr handle = _window.SystemHandle;
                IntPtr style = GetWindowLongPtr(handle, GWL_STYLE);
                style = new IntPtr(style.ToInt64() & ~WS_MAXIMIZEBOX);
                SetWindowLongPtr(handle, GWL_STYLE, style);
            }

            _texture = new Texture((uint)Architecture.DISPLAY_SIZE.Width, (uint)Architecture.DISPLAY_SIZE.Height);
            _sprite = new Sprite(_texture);

            _window.Closed += (sender, e) =>
            {
                _isOpen = false;
                Environment.Exit(0);  // Exit the entire program
            };
            _window.Resized += HandleResize;

            while (_isOpen)
            {
                _window.DispatchEvents();  // Handle events

                long now = Stopwatch.GetTimestamp();
                if (now - _lastRefreshTimestamp >= Stopwatch.Frequency / Architecture.DISPLAY_HZ_FREQUENCY)
                {
                    _lastRefreshTimestamp = now;

                    lock (_syncLock)
                    {
                        if (_grid != null && _needsRender)
                        {
                            UpdateTexture();
                            _needsRender = false;
                        }
                    }

                    Render();
                }

                Thread.Sleep(5);  // Light sleep to avoid 100% CPU
            }

            // Cleanup on this thread
            _sprite.Dispose();
            _texture.Dispose();
            _window.Dispose();
        }

        private void HandleResize(object? sender, SizeEventArgs e)
        {
            uint newWidth = e.Width;
            uint newHeight = e.Height;

            if ((int)newWidth == _currentWidth && (int)newHeight == _currentHeight) return;

            // Detect which dimension was primarily resized and adjust the other to maintain aspect ratio
            int adjustedWidth = (int)newWidth;
            int adjustedHeight = (int)newHeight;

            if (newWidth != (uint)_currentWidth && newHeight == (uint)_currentHeight)
            {
                // Width was resized (side drag), adjust height
                adjustedHeight = (int)(newWidth / _aspectRatio);
            }
            else if (newHeight != (uint)_currentHeight && newWidth == (uint)_currentWidth)
            {
                // Height was resized (top/bottom drag), adjust width
                adjustedWidth = (int)(newHeight * _aspectRatio);
            }
            else
            {
                // Corner drag or both changed: adjust based on the larger proportional change
                double deltaWidth = Math.Abs(newWidth - _currentWidth) / (double)_currentWidth;
                double deltaHeight = Math.Abs(newHeight - _currentHeight) / (double)_currentHeight;

                if (deltaWidth > deltaHeight)
                {
                    // Width changed more, adjust height
                    adjustedHeight = (int)(newWidth / _aspectRatio);
                }
                else
                {
                    // Height changed more (or equal), adjust width
                    adjustedWidth = (int)(newHeight * _aspectRatio);
                }
            }

            // Clamp to minimum size if needed
            if (adjustedWidth < Architecture.DISPLAY_SIZE.Width || adjustedHeight < Architecture.DISPLAY_SIZE.Height)
            {
                adjustedWidth = Architecture.DISPLAY_SIZE.Width;
                adjustedHeight = Architecture.DISPLAY_SIZE.Height;
            }

            // Apply the adjusted size
            _window!.Size = new Vector2u((uint)adjustedWidth, (uint)adjustedHeight);

            // Update current size
            _currentWidth = adjustedWidth;
            _currentHeight = adjustedHeight;

            // Render again if needed
        }

        /// <summary>
        /// Updates the pixel grid, rendering it if needed.
        /// </summary>
        public void UpdateGrid()
        {
            if (!IsOpen)
            {
                Start();
            }

            _needsRender = true;
        }

        private void UpdateTexture()
        {
            if (_grid == null) return;

            // Fill the pixel buffer from the grid (RGBA)
            for (int y = 0; y < Architecture.DISPLAY_SIZE.Height; y++)
            {
                for (int x = 0; x < Architecture.DISPLAY_SIZE.Width; x++)
                {
                    Pixel p = _grid[y, x];
                    int index = (y * Architecture.DISPLAY_SIZE.Width + x) * 4;
                    _pixelsBuffer[index] = p.Red;
                    _pixelsBuffer[index + 1] = p.Green;
                    _pixelsBuffer[index + 2] = p.Blue;
                    _pixelsBuffer[index + 3] = 255; // Alpha
                }
            }

            _texture!.Update(_pixelsBuffer);
        }

        /// <summary>
        /// Renders the current pixel grid to the SFML window.
        /// </summary>
        private void Render()
        {
            if (!_isOpen) return;

            _window!.Clear();
            _window.Draw(_sprite);
            _window.Display();
        }

        public void Dispose()
        {
            lock (_syncLock)
            {
                _isOpen = false;
            }
            _renderThread?.Join();  // Wait for thread to exit
        }
    }
}