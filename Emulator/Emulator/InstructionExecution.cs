using System.Net;
using System.Reflection;

namespace Emulator
{
    /// <summary>
    /// Defines the contract for instruction execution implementations.
    /// Each instruction type implements this interface to provide its specific execution logic.
    /// </summary>
    internal interface IExecuteInstruction
    {
        void Execute(ref CPUContext context, bool advancePC = true);
        
        bool RequiresPipelineFlush => false;
    }

    /// <summary>
    /// Factory class to create IExecuteInstruction instances based on Instruction mnemonics.
    /// </summary>
    internal static class ExecuteFactory
    {
        public static IExecuteInstruction GetExecute(Instruction instruction)
        {
            if (_cache.TryGetValue(instruction, out var value))
            {
                return value;
            }

            string mnemonic = instruction.Mnemonic;
            List<Argument> arguments = (List<Argument>)instruction.Arguments;

            Type? type = Assembly.GetExecutingAssembly()
                .GetType($"Emulator.Execute{mnemonic}")
                ?? throw new InvalidOperationException($"Instruction '{mnemonic}' not supported");

            if (!typeof(IExecuteInstruction).IsAssignableFrom(type))
            {
                throw new InvalidOperationException($"Instruction '{mnemonic}' not supported");
            }

            // Try constructor that accepts List<Argument>
            ConstructorInfo? ctorList = type.GetConstructor(new Type[] { typeof(List<Argument>) });
            if (ctorList != null)
            {
                IExecuteInstruction toExecute = (IExecuteInstruction)ctorList.Invoke(new object[] { arguments });
                _cache.Add(instruction, toExecute);
                return toExecute;
            }

            if (instruction.Arguments.Count > 0)
                throw new InvalidOperationException($"Instruction '{mnemonic}' not supported");

            // Try parameterless constructor
            ConstructorInfo? ctorParameterless = type.GetConstructor(Type.EmptyTypes);
            if (ctorParameterless != null)
            {
                IExecuteInstruction toExecute = (IExecuteInstruction)ctorParameterless.Invoke(Array.Empty<object>());
                _cache.Add(instruction, toExecute);
                return toExecute;
            }

            throw new InvalidOperationException($"Instruction '{mnemonic}' not supported");
        }

        private static readonly Dictionary<Instruction, IExecuteInstruction> _cache = new();
    }

    #region Instruction Implementations

    internal sealed class ExecuteNOP : IExecuteInstruction
    {
        public void Execute(ref CPUContext context, bool advancePC = true)
        {
            if (advancePC)
                context.ProgramCounter.Increment();
        }
    }

    internal sealed class ExecuteHLT : IExecuteInstruction
    {
        public void Execute(ref CPUContext context, bool advancePC = true)
        {
            context.Halted = true;
        }
    }

    #region ALU Operations

    /// <summary>
    /// Base class for ALU operations operating on only registers.
    /// </summary>
    internal abstract class ExecuteALU : IExecuteInstruction
    {
        private readonly byte _destination;
        private readonly byte _sourceA;
        private readonly byte _sourceB; // 0 for 2-operand instructions     

        public ExecuteALU(List<Argument> arguments)
        {
            _destination = ((RegisterArgument)arguments[0]).Value;
            _sourceA = ((RegisterArgument)arguments[1]).Value;

            _sourceB = arguments.Count > 2
                ? ((RegisterArgument)arguments[2]).Value : (byte)0;
        }

        protected abstract (byte result, bool carry) Compute(byte a, byte b, byte carryIn /*0 or 1, byte to avoid duplicate casting*/);

        public void Execute(ref CPUContext context, bool advancePC = true)
        {
            byte registerAValue = context.Registers[_sourceA];
            byte registerBValue = context.Registers[_sourceB];
            byte carryInValue = (byte)(context.CarryFlag ? 1 : 0);

            (byte result, context.CarryFlag) = Compute(registerAValue, registerBValue, carryInValue);

            context.ZeroFlag = (result == 0);
            context.Registers[_destination] = result;

            if (advancePC)
                context.ProgramCounter.Increment();
        }

        /// <summary>
        /// Helper to get the truncated result and carry flag from an integer result.
        /// </summary>
        protected static (byte result, bool carry) GetResultAndCarry(int result)
        {
            byte truncatedResult = (byte)(result & 0xFF);
            bool carry = result > 0xFF;
            return (truncatedResult, carry);
        }

        // Helpers for calculations
        protected const byte SIGN_BIT = 0x80;
        protected const byte CARRY_BIT = 0x01;
    }

    internal sealed class ExecuteADD(List<Argument> arguments) 
        : ExecuteALU(arguments), IExecuteInstruction
    {
        protected override (byte result, bool carry) Compute(byte a, byte b, byte carryIn)
        {
            int result = a + b;
            return GetResultAndCarry(result);
        }
    }

    internal sealed class ExecuteADC(List<Argument> arguments)
        : ExecuteALU(arguments), IExecuteInstruction
    {
        protected override (byte result, bool carry) Compute(byte a, byte b, byte carryIn)
        {
            int result = a + b + carryIn;
            return GetResultAndCarry(result);
        }
    }

    internal sealed class ExecuteSUB(List<Argument> arguments)
        : ExecuteALU(arguments), IExecuteInstruction
    {
        protected override (byte result, bool carry) Compute(byte a, byte b, byte carryIn)
        {
            int result = a + ~b + 1;
            return GetResultAndCarry(result);
        }
    }

    internal sealed class ExecuteSUBC(List<Argument> arguments)
        : ExecuteALU(arguments), IExecuteInstruction
    {
        protected override (byte result, bool carry) Compute(byte a, byte b, byte carryIn)
        {
            int result = a + ~b + carryIn;
            return GetResultAndCarry(result);
        }
    }

    internal sealed class ExecuteAND(List<Argument> arguments)
        : ExecuteALU(arguments), IExecuteInstruction
    {
        protected override (byte result, bool carry) Compute(byte a, byte b, byte carryIn)
        {
            byte result = (byte)(a & b);
            return (result, false);
        }
    }

    internal sealed class ExecuteOR(List<Argument> arguments)
        : ExecuteALU(arguments), IExecuteInstruction
    {
        protected override (byte result, bool carry) Compute(byte a, byte b, byte carryIn)
        {
            byte result = (byte)(a | b);
            return (result, false);
        }
    }

    internal sealed class ExecuteXOR(List<Argument> arguments)
        : ExecuteALU(arguments), IExecuteInstruction
    {
        protected override (byte result, bool carry) Compute(byte a, byte b, byte carryIn)
        {
            byte result = (byte)(a ^ b);
            return (result, false);
        }
    }

    internal sealed class ExecuteNOT(List<Argument> arguments)
        : ExecuteALU(arguments), IExecuteInstruction
    {
        protected override (byte result, bool carry) Compute(byte a, byte b, byte carryIn)
        {
            byte result = (byte)(~a);
            return (result, false);
        }
    }

    internal sealed class ExecuteSHFT(List<Argument> arguments)
        : ExecuteALU(arguments), IExecuteInstruction
    {
        protected override (byte result, bool carry) Compute(byte a, byte b, byte carryIn)
        {
            byte result = (byte)(a >> 1);
            bool carry = (a & CARRY_BIT) != 0;
            return (result, carry);
        }
    }

    internal sealed class ExecuteSHFC(List<Argument> arguments)
        : ExecuteALU(arguments), IExecuteInstruction
    {
        protected override (byte result, bool carry) Compute(byte a, byte b, byte carryIn)
        {
            byte result = (byte)((a >> 1) + carryIn * SIGN_BIT);
            bool carry = (a & CARRY_BIT) != 0;
            return (result, carry);
        }
    }

    internal sealed class ExecuteSHFE(List<Argument> arguments)
    : ExecuteALU(arguments), IExecuteInstruction
    {
        protected override (byte result, bool carry) Compute(byte a, byte b, byte carryIn)
        {
            byte signBit = (byte)(a & SIGN_BIT);
            byte result = (byte)((a >> 1) + signBit);
            bool carry = (a & CARRY_BIT) != 0;
            return (result, carry);
        }
    }

    internal sealed class ExecuteSEX(List<Argument> arguments)
    : ExecuteALU(arguments), IExecuteInstruction
    {
        protected override (byte result, bool carry) Compute(byte a, byte b, byte carryIn)
        {
            byte signBit = (byte)(a & SIGN_BIT);
            byte result = signBit == 0 ? (byte)0 : (byte)0xFF;
            return (result, false);
        }
    }

    internal sealed class ExecuteMOV(List<Argument> arguments) 
        : ExecuteALU(arguments), IExecuteInstruction
    {
        protected override (byte result, bool carry) Compute(byte a, byte b, byte carryIn)
        {
            return (a, false);
        }
    }

    internal sealed class ExecuteMOVC
    {
        private readonly byte _destination;
        private readonly byte _source;
        private readonly byte _cond;

        public ExecuteMOVC(List<Argument> arguments)
        {
            _source = ((RegisterArgument)arguments[0]).Value;
            _destination = ((RegisterArgument)arguments[1]).Value;
            _cond = ((NumberArgument)arguments[2]).Value;
        }

        public void Execute(ref CPUContext context, bool advancePC = true)
        {
            bool shouldMOV = false;

            switch (_cond)
            {
                case Architecture.BRANCH_IF_ZERO_CODE:
                    shouldMOV = context.ZeroFlag;
                    break;
                case Architecture.BRANCH_IF_NOT_ZERO_CODE:
                    shouldMOV = !context.ZeroFlag;
                    break;
                case Architecture.BRANCH_IF_CARRY_CODE:
                    shouldMOV = context.CarryFlag;
                    break;
                case Architecture.BRANCH_IF_NOT_CARRY_CODE:
                    shouldMOV = !context.CarryFlag;
                    break;
            }

            if (shouldMOV)
            {
                context.Registers[_destination] = context.Registers[_source];
            }

            if (advancePC)
                context.ProgramCounter.Increment();
        }
    }

    #endregion

    #region Memory / Stack Operations

    internal abstract class ExecuteMemoryWrite : IExecuteInstruction
    {
        protected readonly byte _source;

        public ExecuteMemoryWrite(List<Argument> arguments)
        {
            _source = ((RegisterArgument)arguments[0]).Value;
        }

        protected abstract byte GetAddress(CPUContext context);

        public void Execute(ref CPUContext context, bool advancePC = true)
        {
            context.RAM[GetAddress(context)] = context.Registers[_source];

            if (advancePC)
                context.ProgramCounter.Increment();
        }
    }

    internal sealed class ExecuteMST : ExecuteMemoryWrite, IExecuteInstruction
    {
        private readonly byte _address;

        public ExecuteMST(List<Argument> arguments) : base(arguments) 
        {
            _address = ((NumberArgument)arguments[1]).Value;
        }

        protected override byte GetAddress(CPUContext context)
        {
            return _address;
        }
    }

    internal sealed class ExecuteMSP : ExecuteMemoryWrite, IExecuteInstruction
    {
        private readonly byte _pointerRegister;
        private readonly sbyte _offset;

        public ExecuteMSP(List<Argument> arguments) : base(arguments)
        {
            _pointerRegister = ((RegisterArgument)arguments[1]).Value;
            _offset = unchecked((sbyte)((NumberArgument)arguments[2]).Value);
        }

        protected override byte GetAddress(CPUContext context)
        {
            return (byte)(context.Registers[_pointerRegister] - _offset - 1);
        }
    }

    internal sealed class ExecuteMSS : ExecuteMemoryWrite, IExecuteInstruction
    {
        private readonly byte _address;

        public ExecuteMSS(List<Argument> arguments) : base(arguments)
        {
            _address = ((NumberArgument)arguments[1]).Value;
        }

        protected override byte GetAddress(CPUContext context)
        {
            return (byte)(context.StackPointer.Value - _address - 1);
        }
    }

    internal sealed class ExecuteMSPS : ExecuteMemoryWrite, IExecuteInstruction
    {
        private readonly byte _pointerRegister;
        private readonly sbyte _offset;

        public ExecuteMSPS(List<Argument> arguments) : base(arguments)
        {
            _pointerRegister = ((RegisterArgument)arguments[1]).Value;
            _offset = unchecked((sbyte)((NumberArgument)arguments[2]).Value);
        }

        protected override byte GetAddress(CPUContext context)
        {
            return (byte)((context.StackPointer.Value - _offset - 1) - context.Registers[_pointerRegister] - 1);
        }
    }

    internal abstract class ExecuteMemoryRead : IExecuteInstruction
    {
        protected readonly byte _destination;

        public ExecuteMemoryRead(List<Argument> arguments)
        {
            _destination = ((RegisterArgument)arguments[0]).Value;
        }

        protected abstract byte GetAddress(CPUContext context);

        public void Execute(ref CPUContext context, bool advancePC = true)
        {
            context.Registers[_destination] = context.RAM[GetAddress(context)];

            if (advancePC)
                context.ProgramCounter.Increment();
        }
    }

    internal sealed class ExecuteMLD : ExecuteMemoryRead, IExecuteInstruction
    {
        private readonly byte _address;

        public ExecuteMLD(List<Argument> arguments) : base(arguments)
        {
            _address = ((NumberArgument)arguments[1]).Value;
        }

        protected override byte GetAddress(CPUContext context)
        {
            return _address;
        }
    }

    internal sealed class ExecuteMLP : ExecuteMemoryRead, IExecuteInstruction
    {
        private readonly byte _pointerRegister;
        private readonly sbyte _offset;

        public ExecuteMLP(List<Argument> arguments) : base(arguments)
        {
            _pointerRegister = ((RegisterArgument)arguments[1]).Value;
            _offset = unchecked((sbyte)((NumberArgument)arguments[2]).Value);
        }

        protected override byte GetAddress(CPUContext context)
        {
            return (byte)(context.Registers[_pointerRegister] - _offset - 1);
        }
    }

    internal sealed class ExecuteMLS : ExecuteMemoryRead, IExecuteInstruction
    {
        private readonly byte _address;

        public ExecuteMLS(List<Argument> arguments) : base(arguments)
        {
            _address = ((NumberArgument)arguments[1]).Value;
        }

        protected override byte GetAddress(CPUContext context)
        {
            return (byte)(context.StackPointer.Value - _address - 1);
        }
    }

    internal sealed class ExecuteMLPS : ExecuteMemoryRead, IExecuteInstruction
    {
        private readonly byte _pointerRegister;
        private readonly sbyte _offset;

        public ExecuteMLPS(List<Argument> arguments) : base(arguments)
        {
            _pointerRegister = ((RegisterArgument)arguments[1]).Value;
            _offset = unchecked((sbyte)((NumberArgument)arguments[2]).Value);
        }

        protected override byte GetAddress(CPUContext context)
        {
            return (byte)((context.StackPointer.Value - _offset - 1) - context.Registers[_pointerRegister] - 1);
        }
    }

    // Stack operations

    internal sealed class ExecutePSH : IExecuteInstruction
    {
        private readonly byte _value;

        public ExecutePSH(List<Argument> arguments)
        {
            _value = ((NumberArgument)arguments[0]).Value;
        }

        public void Execute(ref CPUContext context, bool advancePC = true)
        {
            context.RAM[context.StackPointer.Value] = _value;
            context.StackPointer.Increment();

            if (advancePC)
                context.ProgramCounter.Increment();
        }
    }

    internal sealed class ExecutePHR : IExecuteInstruction
    {
        private readonly byte _source;

        public ExecutePHR(List<Argument> arguments)
        {
            _source = ((RegisterArgument)arguments[0]).Value;
        }

        public void Execute(ref CPUContext context, bool advancePC = true)
        {
            context.RAM[context.StackPointer.Value] = context.Registers[_source];
            context.StackPointer.Increment();

            if (advancePC)
                context.ProgramCounter.Increment();
        }
    }

    internal sealed class ExecutePOP : IExecuteInstruction
    {
        private readonly byte _frameSize;

        public ExecutePOP(List<Argument> arguments)
        {
            _frameSize = ((NumberArgument)arguments[0]).Value;
        }

        public void Execute(ref CPUContext context, bool advancePC = true)
        {
            context.StackPointer.Decrement(_frameSize);

            if (advancePC)
                context.ProgramCounter.Increment();
        }
    }

    internal sealed class ExecutePSHM : IExecuteInstruction
    {
        private readonly byte _frameSize;

        public ExecutePSHM(List<Argument> arguments)
        {
            _frameSize = ((NumberArgument)arguments[0]).Value;
        }

        public void Execute(ref CPUContext context, bool advancePC = true)
        {
            context.StackPointer.Increment(_frameSize);

            if (advancePC)
                context.ProgramCounter.Increment();
        }
    }

    #endregion

    #region Control Flow Operations

    internal sealed class ExecuteJMP : IExecuteInstruction
    {
        private readonly ushort _address;

        public ExecuteJMP(List<Argument> arguments)
        {
            _address = ((AddressArgument)arguments[0]).Value;
        }

        public void Execute(ref CPUContext context, bool advancePC = true)
        {
            context.ProgramCounter.SetBRH(_address);
        }

        public bool RequiresPipelineFlush => true;
    }
    
    internal sealed class ExecuteBRH : IExecuteInstruction
    {
        private readonly byte _cond;
        private readonly ushort _address;

        public ExecuteBRH(List<Argument> arguments)
        {
            _cond = ((NumberArgument)arguments[0]).Value;
            _address = ((AddressArgument)arguments[1]).Value;
        }

        public void Execute(ref CPUContext context, bool advancePC = true)
        {
            bool shouldBranch = false;

            switch (_cond)
            {
                case Architecture.BRANCH_IF_ZERO_CODE:
                    shouldBranch = context.ZeroFlag;
                    break;
                case Architecture.BRANCH_IF_NOT_ZERO_CODE:
                    shouldBranch = !context.ZeroFlag;
                    break;
                case Architecture.BRANCH_IF_CARRY_CODE:
                    shouldBranch = context.CarryFlag;
                    break;
                case Architecture.BRANCH_IF_NOT_CARRY_CODE:
                    shouldBranch = !context.CarryFlag;
                    break;
            }

            if (shouldBranch)
            {
                context.ProgramCounter.SetBRH(_address);
            }
            else
            {
                context.ProgramCounter.Increment();
            }
        }

        public bool RequiresPipelineFlush => true;
    }

    internal sealed class ExecuteCAL : IExecuteInstruction
    {
        private readonly ushort _address;

        public ExecuteCAL(List<Argument> arguments)
        {
            _address = ((AddressArgument)arguments[0]).Value;
        }

        public void Execute(ref CPUContext context, bool advancePC = true)
        {
            context.ProgramCounter.PushCAL(_address);
        }

        public bool RequiresPipelineFlush => true;
    }

    internal sealed class ExecuteRET : IExecuteInstruction
    {
        private readonly byte _frameSize;

        public ExecuteRET(List<Argument> arguments)
        {
            _frameSize = ((NumberArgument)arguments[0]).Value;
        }

        public void Execute(ref CPUContext context, bool advancePC = true)
        {
            context.StackPointer.Decrement(_frameSize);
            context.ProgramCounter.PopRET();
        }

        public bool RequiresPipelineFlush => true;
    }








    #endregion






    #endregion

}
