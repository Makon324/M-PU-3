namespace Emulator
{
    internal abstract class Argument { }

    internal sealed class RegisterArgument : Argument
    {
        public byte Value { get; }

        public RegisterArgument(byte value)
        {
            Value = value;
        }

        public RegisterArgument(Register register)
        {
            Value = (byte)register;
        }

        public override string ToString()
        {
            return ((Register)Value).ToString();
        }
    }

    internal sealed class NumberArgument(byte value) : Argument
    {
        public byte Value { get; } = value;

        public override string ToString()
        {
            return $"{Value}";
        }
    }

    internal sealed class AddressArgument(ushort value) : Argument
    {
        public ushort Value { get; } = value;

        public override string ToString()
        {
            return $"0x{Value:X4}";
        }
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

        public override string ToString()
        {
            if (Arguments.Count == 0)
            {
                return Mnemonic;
            }

            return $"{Mnemonic} {string.Join(", ", Arguments.Select(a => a.ToString()))}";
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
