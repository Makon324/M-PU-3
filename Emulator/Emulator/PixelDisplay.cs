using SDL2;
using System.Diagnostics;
using System.Runtime.InteropServices;

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

        private readonly Pixel[,] _grid = new Pixel[Architecture.DISPLAY_SIZE.Height, Architecture.DISPLAY_SIZE.Width];

        private byte _X;
        private byte _Y;

        private long _lastRefreshTimestamp = 0;

        private SDLRenderer? _renderer = null;

        private void SetPixel()
        {
            _grid[_Y, _X] = new Pixel
            {
                Red = _RGBports[0].PortLoad(),
                Green = _RGBports[1].PortLoad(),
                Blue = _RGBports[2].PortLoad()
            };

            RefreshWindow();
        }

        private void RefreshWindow()
        {
            if (_renderer == null)
            {
                _renderer = new SDLRenderer();
            }

            long now = Stopwatch.GetTimestamp();
            if (now - _lastRefreshTimestamp >= Stopwatch.Frequency / Architecture.DISPLAY_HZ_FREQUENCY)
            {
                _lastRefreshTimestamp = now;

                _renderer.Render(_grid);
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

    /// <summary>
    /// Renders a pixel grid to an SDL2 window.
    /// </summary>
    internal sealed class SDLRenderer : IDisposable
    {
        private IntPtr _window;
        private IntPtr _rendererPtr;
        private IntPtr _texture;

        private bool _isOpen = true;

        private int _currentWidth;
        private int _currentHeight;
        private double _aspectRatio;

        private byte[] _pixelsBuffer = new byte[Architecture.DISPLAY_SIZE.Width * Architecture.DISPLAY_SIZE.Height * 3];

        public bool IsOpen => _isOpen;

        public SDLRenderer()
        {
            if (SDL.SDL_Init(SDL.SDL_INIT_VIDEO) < 0)
            {
                throw new InvalidOperationException($"SDL_Init failed: {SDL.SDL_GetError()}");
            }

            // Create window
            _window = SDL.SDL_CreateWindow(
                "Pixel Display",
                SDL.SDL_WINDOWPOS_CENTERED,
                SDL.SDL_WINDOWPOS_CENTERED,
                Architecture.DISPLAY_SIZE.Width,
                Architecture.DISPLAY_SIZE.Height,
                SDL.SDL_WindowFlags.SDL_WINDOW_RESIZABLE
            );

            if (_window == IntPtr.Zero)
            {
                throw new InvalidOperationException($"SDL_CreateWindow failed: {SDL.SDL_GetError()}");
            }

            _rendererPtr = SDL.SDL_CreateRenderer(_window, -1, SDL.SDL_RendererFlags.SDL_RENDERER_ACCELERATED);

            if (_rendererPtr == IntPtr.Zero)
            {
                throw new InvalidOperationException($"SDL_CreateRenderer failed: {SDL.SDL_GetError()}");
            }

            SDL.SDL_SetHint(SDL.SDL_HINT_RENDER_SCALE_QUALITY, "nearest");

            _texture = SDL.SDL_CreateTexture(
                _rendererPtr,
                SDL.SDL_PIXELFORMAT_RGB24,
                (int)SDL.SDL_TextureAccess.SDL_TEXTUREACCESS_STREAMING,
                Architecture.DISPLAY_SIZE.Width,
                Architecture.DISPLAY_SIZE.Height
            );

            if (_texture == IntPtr.Zero)
            {
                throw new InvalidOperationException($"SDL_CreateTexture failed: {SDL.SDL_GetError()}");
            }

            // Initialize aspect ratio and current size
            _currentWidth = Architecture.DISPLAY_SIZE.Width;
            _currentHeight = Architecture.DISPLAY_SIZE.Height;
            _aspectRatio = _currentWidth / (double)_currentHeight;
        }

        public void PollEvents()
        {
            SDL.SDL_Event sdlEvent;
            while (SDL.SDL_PollEvent(out sdlEvent) != 0)
            {
                switch (sdlEvent.type)
                {
                    case SDL.SDL_EventType.SDL_QUIT:
                        _isOpen = false;
                        break;
                    case SDL.SDL_EventType.SDL_WINDOWEVENT:
                        if (sdlEvent.window.windowEvent == SDL.SDL_WindowEventID.SDL_WINDOWEVENT_RESIZED)
                        {
                            HandleResize(sdlEvent);
                        }
                        break;
                }
            }
        }

        private void HandleResize(SDL.SDL_Event sdlEvent)
        {
            int newWidth = sdlEvent.window.data1;
            int newHeight = sdlEvent.window.data2;

            if (newWidth == _currentWidth && newHeight == _currentHeight) return;

            // Detect which dimension was primarily resized and adjust the other to maintain aspect ratio
            int adjustedWidth = newWidth;
            int adjustedHeight = newHeight;

            if (newWidth != _currentWidth && newHeight == _currentHeight)
            {
                // Width was resized (side drag), adjust height
                adjustedHeight = (int)(newWidth / _aspectRatio);
            }
            else if (newHeight != _currentHeight && newWidth == _currentWidth)
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
            SDL.SDL_SetWindowSize(_window, adjustedWidth, adjustedHeight);

            // Update current size
            _currentWidth = adjustedWidth;
            _currentHeight = adjustedHeight;
        }

        public void Render(Pixel[,] grid)
        {
            if (!_isOpen) return;

            PollEvents();

            // MAIN RENDERING LOGIC

            // Fill the pixel buffer from the grid
            for (int y = 0; y < Architecture.DISPLAY_SIZE.Height; y++)
            {
                for (int x = 0; x < Architecture.DISPLAY_SIZE.Width; x++)
                {
                    Pixel p = grid[y, x];
                    int index = (y * Architecture.DISPLAY_SIZE.Width + x) * 3;
                    _pixelsBuffer[index] = p.Red;
                    _pixelsBuffer[index + 1] = p.Green;
                    _pixelsBuffer[index + 2] = p.Blue;
                }
            }

            // Lock the texture to get a pointer for writing
            IntPtr lockedPixels;
            int pitch;
            if (SDL.SDL_LockTexture(_texture, IntPtr.Zero, out lockedPixels, out pitch) != 0)
            {
                throw new InvalidOperationException($"SDL_LockTexture failed: {SDL.SDL_GetError()}");
            }

            // Copy the managed array to the texture
            unsafe
            {
                byte* dst = (byte*)lockedPixels;
                for (int y = 0; y < Architecture.DISPLAY_SIZE.Height; y++)
                {                    
                    int srcOffset = y * Architecture.DISPLAY_SIZE.Width * 3; // Source row start

                    byte* dstRow = dst + y * pitch; // Destination row start
                    
                    Marshal.Copy(_pixelsBuffer, srcOffset, (IntPtr)dstRow, Architecture.DISPLAY_SIZE.Width * 3);
                }
            }

            SDL.SDL_UnlockTexture(_texture);
            SDL.SDL_RenderClear(_rendererPtr);
            SDL.SDL_RenderCopy(_rendererPtr, _texture, IntPtr.Zero, IntPtr.Zero);
            SDL.SDL_RenderPresent(_rendererPtr);
        }

        public void Dispose()
        {
            if (_texture != IntPtr.Zero)
            {
                SDL.SDL_DestroyTexture(_texture);
                _texture = IntPtr.Zero;
            }
            if (_rendererPtr != IntPtr.Zero)
            {
                SDL.SDL_DestroyRenderer(_rendererPtr);
                _rendererPtr = IntPtr.Zero;
            }
            if (_window != IntPtr.Zero)
            {
                SDL.SDL_DestroyWindow(_window);
                _window = IntPtr.Zero;
            }
            SDL.SDL_Quit();
        }
    }
}
