using System.Runtime.InteropServices;

namespace Emulator
{
    /// <summary>
    /// A simple output device that prints the stored byte as a character to the command line.
    /// </summary>
    internal sealed class ConsoleOutputDevice : IOPort
    {
        public void PortStore(byte value)
        {
            Global.GetService<IRenderer>().WriteToConsole(((char)value).ToString());
        }

        public byte PortLoad()
        {
            return 0; // Loading from this device is not supported; return 0 as a default.
        }
    }

    /// <summary>
    /// Represents a keyboard input device. It uses the Windows API to poll for currently pressed keys 
    /// from the <see cref="Key"/> enum and enqueues them in a unique queue to avoid duplicates. 
    /// The <see cref="PortLoad"/> method adds any newly pressed keys to the queue and dequeues the next key code 
    /// (as a byte) if available, or returns 0 if the queue is empty. 
    /// The <see cref="PortStore"/> method clears the queue when the value 0 is stored.
    /// </summary>
    internal sealed class KeyboardDevice : IOPort
    {
        [DllImport("user32.dll")]
        public static extern short GetAsyncKeyState(int vKey);

        private static bool IsKeyDown(Key key)
        {
            return (GetAsyncKeyState((int)key) & 0x8000) != 0;
        }

        private readonly UniqueQueue<Key> _keyQueue = new UniqueQueue<Key>();

        public void PortStore(byte value)
        {
            // Clear queue if value == 0
            if (value == 0)
            {
                _keyQueue.Clear();
            }
        }

        public byte PortLoad()
        {
            foreach (Key key in Enum.GetValues<Key>())
            {
                if (IsKeyDown(key))
                {
                    _keyQueue.Enqueue(key);
                }
            }

            if (_keyQueue.Count == 0)
            {
                return 0;
            }
            else
            {
                return (byte)_keyQueue.Dequeue();
            }
        }
    }

    /// <summary>
    /// Represent a set of keyboard keys mapped to their Windows VK codes.
    /// </summary>
    public enum Key : byte
    {
        // Letters (VK codes are the same for upper/lowercase)
        A = 0x41, B = 0x42, C = 0x43, D = 0x44, E = 0x45, F = 0x46, G = 0x47,
        H = 0x48, I = 0x49, J = 0x4A, K = 0x4B, L = 0x4C, M = 0x4D, N = 0x4E,
        O = 0x4F, P = 0x50, Q = 0x51, R = 0x52, S = 0x53, T = 0x54, U = 0x55,
        V = 0x56, W = 0x57, X = 0x58, Y = 0x59, Z = 0x5A,

        // Digits (main keyboard)
        D0 = 0x30, D1 = 0x31, D2 = 0x32, D3 = 0x33,
        D4 = 0x34, D5 = 0x35, D6 = 0x36, D7 = 0x37,
        D8 = 0x38, D9 = 0x39,

        // Common symbols (US keyboard layout VK_OEM codes)
        OemTilde = 0xC0,            // ` ~ (backtick/tilde)
        OemMinus = 0xBD,            // - _ (minus/underscore)
        OemPlus = 0xBB,             // = + (equals/plus)
        OemOpenBrackets = 0xDB,     // [ { (left bracket/brace)
        OemCloseBrackets = 0xDD,    // ] } (right bracket/brace)
        OemPipe = 0xDC,             // \ | (backslash/pipe)
        OemSemicolon = 0xBA,        // ; : (semicolon/colon)
        OemQuotes = 0xDE,           // ' " (apostrophe/double quote)
        OemComma = 0xBC,            // , < (comma/less than)
        OemPeriod = 0xBE,           // . > (period/greater than)
        OemQuestion = 0xBF,         // / ? (slash/question)
        Oem1 = 0xBA,                // Alias for semicolon (OEM_1)
        Oem2 = 0xBF,                // Alias for slash (OEM_2)
        Oem3 = 0xC0,                // Alias for tilde (OEM_3)
        Oem4 = 0xDB,                // Alias for left bracket (OEM_4)
        Oem5 = 0xDC,                // Alias for backslash (OEM_5)
        Oem6 = 0xDD,                // Alias for right bracket (OEM_6)
        Oem7 = 0xDE,                // Alias for quotes (OEM_7)
        Oem8 = 0xDF,                // Misc/OEM specific

        // Control / Special Keys
        Backspace = 0x08,      // Backspace
        Tab = 0x09,            // Tab
        Enter = 0x0D,          // Enter/Return
        Escape = 0x1B,         // Esc
        Space = 0x20,          // Spacebar
        Delete = 0x2E,         // Delete (forward delete)

        // Arrow Keys
        Left = 0x25,           // Left arrow
        Up = 0x26,             // Up arrow
        Right = 0x27,          // Right arrow
        Down = 0x28,           // Down arrow

        // Navigation Keys
        Insert = 0x2D,         // Insert
        Home = 0x24,           // Home
        End = 0x23,            // End
        PageUp = 0x21,         // Page Up
        PageDown = 0x22,       // Page Down

        // Modifier Keys
        LeftShift = 0xA0,      // Left Shift
        RightShift = 0xA1,     // Right Shift
        LeftControl = 0xA2,    // Left Ctrl
        RightControl = 0xA3,   // Right Ctrl
        LeftAlt = 0xA4,        // Left Alt
        RightAlt = 0xA5,       // Right Alt
        LeftWin = 0x5B,        // Left Windows key
        RightWin = 0x5C,       // Right Windows key

        // Function Keys
        F1 = 0x70, F2 = 0x71, F3 = 0x72, F4 = 0x73,
        F5 = 0x74, F6 = 0x75, F7 = 0x76, F8 = 0x77,
        F9 = 0x78, F10 = 0x79, F11 = 0x7A, F12 = 0x7B,

        // Numpad Keys
        NumLock = 0x90,        // Num Lock
        NumPad0 = 0x60, NumPad1 = 0x61, NumPad2 = 0x62, NumPad3 = 0x63,
        NumPad4 = 0x64, NumPad5 = 0x65, NumPad6 = 0x66, NumPad7 = 0x67,
        NumPad8 = 0x68, NumPad9 = 0x69,
        Multiply = 0x6A,       // Numpad *
        Add = 0x6B,            // Numpad +
        Subtract = 0x6D,       // Numpad -
        Decimal = 0x6E,        // Numpad .
        Divide = 0x6F,         // Numpad /

        // Lock Keys
        CapsLock = 0x14,       // Caps Lock
        ScrollLock = 0x91,     // Scroll Lock

        // Other Keys
        PrintScreen = 0x2A,    // Print Screen (VK_SNAPSHOT is 0x2C, but often 0x2A for sysreq)
        Pause = 0x13,          // Pause/Break
        BrowserBack = 0xA6,    // Browser Back
        BrowserForward = 0xA7, // Browser Forward

        // Add more as needed
    }

    /// <summary>
    /// Unique Queue implementation that only allows unique items.
    /// </summary>
    internal sealed class UniqueQueue<T>
    {
        private readonly Queue<T> _queue = new Queue<T>();
        private readonly HashSet<T> _set = new HashSet<T>();

        public int Count => _queue.Count;

        public void Enqueue(T item)
        {
            if (_set.Add(item))  // Only add if not already present
            {
                _queue.Enqueue(item);
            }
        }

        public T Dequeue()
        {
            if (_queue.Count == 0)
                throw new InvalidOperationException("Queue is empty.");

            T item = _queue.Dequeue();
            _set.Remove(item);
            return item;
        }

        public T Peek()
        {
            if (_queue.Count == 0)
                throw new InvalidOperationException("Queue is empty.");

            return _queue.Peek();
        }

        public bool Contains(T item) => _set.Contains(item);

        public void Clear()
        {
            _queue.Clear();
            _set.Clear();
        }
    }

}
