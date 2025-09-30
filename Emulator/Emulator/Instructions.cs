using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Emulator
{
    internal enum ArgumentType
    {
        REGISTER,
        NUMBER,
        ADDRESS
    }

    internal abstract class Argument
    {
        public abstract ArgumentType Type { get; }
    }

    internal sealed class RegisterArgument(byte value) : Argument
    {
        public override ArgumentType Type => ArgumentType.REGISTER;
        public byte Value { get; } = value;
    }

    internal sealed class NumberArgument(byte value) : Argument
    {
        public override ArgumentType Type => ArgumentType.NUMBER;
        public byte Value { get; } = value;
    }

    internal sealed class AddressArgument(ushort value) : Argument
    {
        public override ArgumentType Type => ArgumentType.ADDRESS;
        public ushort Value { get; } = value;
    }

    internal sealed class Instruction
    {
        public string Mnemonic { get; }
        public IReadOnlyList<Argument> Arguments { get; }

        public Instruction(string mnemonic, IEnumerable<Argument>? arguments = null)
        {
            Mnemonic = mnemonic;
            Arguments = new List<Argument>(arguments ?? Array.Empty<Argument>());
        }
    }
}
