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

    internal sealed class Instruction : IEquatable<Instruction>
    {
        public string Mnemonic { get; }
        public IReadOnlyList<Argument> Arguments { get; }

        public Instruction(string mnemonic, IEnumerable<Argument>? arguments = null)
        {
            Mnemonic = mnemonic;
            Arguments = new List<Argument>(arguments ?? Array.Empty<Argument>());
        }

        // IEquatable implementation

        public bool Equals(Instruction? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;

            if (!Mnemonic.Equals(other.Mnemonic, StringComparison.Ordinal)) return false;

            if (Arguments.Count != other.Arguments.Count) return false;

            for (int i = 0; i < Arguments.Count; i++)
            {
                if (!Arguments[i].Equals(other.Arguments[i])) return false;
            }

            return true;
        }

        public override bool Equals(object? obj) => Equals(obj as Instruction);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 23 + Mnemonic.GetHashCode(StringComparison.Ordinal);

                foreach (var arg in Arguments)
                {
                    hash = hash * 23 + arg.GetHashCode();
                }

                return hash;
            }
        }

        public static bool operator ==(Instruction? left, Instruction? right) =>
            Equals(left, right);

        public static bool operator !=(Instruction? left, Instruction? right) =>
            !Equals(left, right);
    }
}
