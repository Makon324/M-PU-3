using System.Net;
using System.Reflection;
using System.Xml.Serialization;

namespace Emulator
{
    /// <summary>
    /// Defines the contract for instruction execution implementations.
    /// Each instruction type implements ExecuteInstruction to provide its specific execution logic.
    /// </summary>
    internal abstract class BaseExecute
    {
        public void Execute(ref CPUContext context, bool advancePC = true)
        {
            ExecuteInstruction(ref context);

            if (advancePC && !IsControlFlowInstruction)
                context.ProgramCounter.Increment();
        }

        public virtual bool IsControlFlowInstruction => false;

        protected abstract void ExecuteInstruction(ref CPUContext context);       
    }

    /// <summary>
    /// Factory class to create <see cref="BaseExecute"> instances based on Instruction mnemonics.
    /// </summary>
    internal static class ExecuteFactory
    {
        public static BaseExecute GetExecute(Instruction instruction)
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

            if (!typeof(BaseExecute).IsAssignableFrom(type))
            {
                throw new InvalidOperationException($"Instruction '{mnemonic}' not supported");
            }

            // Try constructor that accepts List<Argument>
            ConstructorInfo? ctorList = type.GetConstructor(new Type[] { typeof(List<Argument>) });
            if (ctorList != null)
            {
                BaseExecute toExecute = (BaseExecute)ctorList.Invoke(new object[] { arguments });
                _cache.Add(instruction, toExecute);
                return toExecute;
            }

            if (instruction.Arguments.Count > 0)
                throw new InvalidOperationException($"Instruction '{mnemonic}' not supported");

            // Try parameterless constructor
            ConstructorInfo? ctorParameterless = type.GetConstructor(Type.EmptyTypes);
            if (ctorParameterless != null)
            {
                BaseExecute toExecute = (BaseExecute)ctorParameterless.Invoke(Array.Empty<object>());
                _cache.Add(instruction, toExecute);
                return toExecute;
            }

            throw new InvalidOperationException($"Instruction '{mnemonic}' not supported");
        }

        private static readonly Dictionary<Instruction, BaseExecute> _cache = new();
    }

    #region Instruction Implementations

    internal sealed class ExecuteNOP : BaseExecute
    {
        protected override void ExecuteInstruction(ref CPUContext context)
        {
            // Do Nothing
        }
    }

    internal sealed class ExecuteHLT : BaseExecute
    {
        protected override void ExecuteInstruction(ref CPUContext context)
        {
            context.Halted = true;
        }
    }

    #region ALU And Registers Operations

    /// <summary>
    /// Base class for ALU operations operating on only registers.
    /// </summary>
    internal abstract class ExecuteALU : BaseExecute
    {
        private readonly Register _destination;
        private readonly Register _sourceA;
        private readonly Register _sourceB; // 0 for 2-operand instructions     

        public ExecuteALU(List<Argument> arguments)
        {
            _destination = (Register)((RegisterArgument)arguments[0]).Value;
            _sourceA = (Register)((RegisterArgument)arguments[1]).Value;

            _sourceB = arguments.Count > 2
                ? (Register)((RegisterArgument)arguments[2]).Value : Register.R0;
        }

        protected abstract (byte result, bool carry) Compute(byte a, byte b, byte carryIn /*0 or 1, byte to avoid duplicate casting*/);

        protected override void ExecuteInstruction(ref CPUContext context)
        {
            byte registerAValue = context.Registers[_sourceA];
            byte registerBValue = context.Registers[_sourceB];
            byte carryInValue = (byte)(context.CarryFlag ? 1 : 0);

            (byte result, context.CarryFlag) = Compute(registerAValue, registerBValue, carryInValue);

            context.ZeroFlag = (result == 0);
            context.Registers[_destination] = result;
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
        : ExecuteALU(arguments)
    {
        protected override (byte result, bool carry) Compute(byte a, byte b, byte carryIn)
        {
            int result = a + b;
            return GetResultAndCarry(result);
        }
    }

    internal sealed class ExecuteADC(List<Argument> arguments)
        : ExecuteALU(arguments)
    {
        protected override (byte result, bool carry) Compute(byte a, byte b, byte carryIn)
        {
            int result = a + b + carryIn;
            return GetResultAndCarry(result);
        }
    }

    internal sealed class ExecuteSUB(List<Argument> arguments)
        : ExecuteALU(arguments)
    {
        protected override (byte result, bool carry) Compute(byte a, byte b, byte carryIn)
        {
            int result = a + ~b + 1;
            return GetResultAndCarry(result);
        }
    }

    internal sealed class ExecuteSUBC(List<Argument> arguments)
        : ExecuteALU(arguments)
    {
        protected override (byte result, bool carry) Compute(byte a, byte b, byte carryIn)
        {
            int result = a + ~b + carryIn;
            return GetResultAndCarry(result);
        }
    }

    internal sealed class ExecuteAND(List<Argument> arguments)
        : ExecuteALU(arguments)
    {
        protected override (byte result, bool carry) Compute(byte a, byte b, byte carryIn)
        {
            byte result = (byte)(a & b);
            return (result, false);
        }
    }

    internal sealed class ExecuteOR(List<Argument> arguments)
        : ExecuteALU(arguments)
    {
        protected override (byte result, bool carry) Compute(byte a, byte b, byte carryIn)
        {
            byte result = (byte)(a | b);
            return (result, false);
        }
    }

    internal sealed class ExecuteXOR(List<Argument> arguments)
        : ExecuteALU(arguments)
    {
        protected override (byte result, bool carry) Compute(byte a, byte b, byte carryIn)
        {
            byte result = (byte)(a ^ b);
            return (result, false);
        }
    }

    internal sealed class ExecuteNOT(List<Argument> arguments)
        : ExecuteALU(arguments)
    {
        protected override (byte result, bool carry) Compute(byte a, byte b, byte carryIn)
        {
            byte result = (byte)(~a);
            return (result, false);
        }
    }

    internal sealed class ExecuteSHFT(List<Argument> arguments)
        : ExecuteALU(arguments)
    {
        protected override (byte result, bool carry) Compute(byte a, byte b, byte carryIn)
        {
            byte result = (byte)(a >> 1);
            bool carry = (a & CARRY_BIT) != 0;
            return (result, carry);
        }
    }

    internal sealed class ExecuteSHFC(List<Argument> arguments)
        : ExecuteALU(arguments)
    {
        protected override (byte result, bool carry) Compute(byte a, byte b, byte carryIn)
        {
            byte result = (byte)((a >> 1) + carryIn * SIGN_BIT);
            bool carry = (a & CARRY_BIT) != 0;
            return (result, carry);
        }
    }

    internal sealed class ExecuteSHFE(List<Argument> arguments)
        : ExecuteALU(arguments)
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
        : ExecuteALU(arguments)
    {
        protected override (byte result, bool carry) Compute(byte a, byte b, byte carryIn)
        {
            byte signBit = (byte)(a & SIGN_BIT);
            byte result = signBit == 0 ? (byte)0 : (byte)0xFF;
            return (result, false);
        }
    }

    internal sealed class ExecuteMOV(List<Argument> arguments) 
        : ExecuteALU(arguments)
    {
        protected override (byte result, bool carry) Compute(byte a, byte b, byte carryIn)
        {
            return (a, false);
        }
    }

    internal sealed class ExecuteMOVC : BaseExecute
    {
        private readonly Register _destination;
        private readonly Register _source;
        private readonly byte _cond;

        public ExecuteMOVC(List<Argument> arguments)
        {
            _source = (Register)((RegisterArgument)arguments[0]).Value;
            _destination = (Register)((RegisterArgument)arguments[1]).Value;
            _cond = ((NumberArgument)arguments[2]).Value;
        }

        protected override void ExecuteInstruction(ref CPUContext context)
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
                context.ZeroFlag = context.Registers[_destination] == 0;
            }
        }        
    }

    internal sealed class ExecuteLDI : BaseExecute
    {
        private readonly Register _destination;
        private readonly byte _immediate;

        public ExecuteLDI(List<Argument> arguments)
        {
            _destination = (Register)((RegisterArgument)arguments[0]).Value;
            _immediate = ((NumberArgument)arguments[1]).Value;
        }

        protected override void ExecuteInstruction(ref CPUContext context)
        {
            context.Registers[_destination] = _immediate;
            context.ZeroFlag = context.Registers[_destination] == 0;
        }
    }



    #endregion

    #region Memory / Stack Operations

    internal abstract class ExecuteMemoryWrite : BaseExecute
    {
        protected readonly Register _source;

        public ExecuteMemoryWrite(List<Argument> arguments)
        {
            _source = (Register)((RegisterArgument)arguments[0]).Value;
        }

        protected abstract byte GetAddress(CPUContext context);

        protected override void ExecuteInstruction(ref CPUContext context)
        {
            context.RAM[GetAddress(context)] = context.Registers[_source];
        }
    }

    internal sealed class ExecuteMST : ExecuteMemoryWrite
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

    internal sealed class ExecuteMSP : ExecuteMemoryWrite
    {
        private readonly Register _pointerRegister;
        private readonly sbyte _offset;

        public ExecuteMSP(List<Argument> arguments) : base(arguments)
        {
            _pointerRegister = (Register)((RegisterArgument)arguments[1]).Value;
            _offset = unchecked((sbyte)((NumberArgument)arguments[2]).Value);
        }

        protected override byte GetAddress(CPUContext context)
        {
            return (byte)(context.Registers[_pointerRegister] - _offset - 1);
        }
    }

    internal sealed class ExecuteMSS : ExecuteMemoryWrite
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

    internal sealed class ExecuteMSPS : ExecuteMemoryWrite
    {
        private readonly Register _pointerRegister;
        private readonly sbyte _offset;

        public ExecuteMSPS(List<Argument> arguments) : base(arguments)
        {
            _pointerRegister = (Register)((RegisterArgument)arguments[1]).Value;
            _offset = unchecked((sbyte)((NumberArgument)arguments[2]).Value);
        }

        protected override byte GetAddress(CPUContext context)
        {
            return (byte)((context.StackPointer.Value - _offset - 1) - context.Registers[_pointerRegister] - 1);
        }
    }

    internal abstract class ExecuteMemoryRead : BaseExecute
    {
        protected readonly Register _destination;

        public ExecuteMemoryRead(List<Argument> arguments)
        {
            _destination = (Register)((RegisterArgument)arguments[0]).Value;
        }

        protected abstract byte GetAddress(CPUContext context);

        protected override void ExecuteInstruction(ref CPUContext context)
        {
            context.Registers[_destination] = context.RAM[GetAddress(context)];
            context.ZeroFlag = context.Registers[_destination] == 0;
        }
    }

    internal sealed class ExecuteMLD : ExecuteMemoryRead
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

    internal sealed class ExecuteMLP : ExecuteMemoryRead
    {
        private readonly Register _pointerRegister;
        private readonly sbyte _offset;

        public ExecuteMLP(List<Argument> arguments) : base(arguments)
        {
            _pointerRegister = (Register)((RegisterArgument)arguments[1]).Value;
            _offset = unchecked((sbyte)((NumberArgument)arguments[2]).Value);
        }

        protected override byte GetAddress(CPUContext context)
        {
            return (byte)(context.Registers[_pointerRegister] - _offset - 1);
        }
    }

    internal sealed class ExecuteMLS : ExecuteMemoryRead
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

    internal sealed class ExecuteMLPS : ExecuteMemoryRead
    {
        private readonly Register _pointerRegister;
        private readonly sbyte _offset;

        public ExecuteMLPS(List<Argument> arguments) : base(arguments)
        {
            _pointerRegister = (Register)((RegisterArgument)arguments[1]).Value;
            _offset = unchecked((sbyte)((NumberArgument)arguments[2]).Value);
        }

        protected override byte GetAddress(CPUContext context)
        {
            return (byte)((context.StackPointer.Value - _offset - 1) - context.Registers[_pointerRegister] - 1);
        }
    }

    // Stack Operations

    internal sealed class ExecutePSH : BaseExecute
    {
        private readonly byte _value;

        public ExecutePSH(List<Argument> arguments)
        {
            _value = ((NumberArgument)arguments[0]).Value;
        }

        protected override void ExecuteInstruction(ref CPUContext context)
        {
            context.RAM[context.StackPointer.Value] = _value;
            context.StackPointer.Increment();
        }
    }

    internal sealed class ExecutePHR : BaseExecute
    {
        private readonly Register _source;

        public ExecutePHR(List<Argument> arguments)
        {
            _source = (Register)((RegisterArgument)arguments[0]).Value;
        }

        protected override void ExecuteInstruction(ref CPUContext context)
        {
            context.RAM[context.StackPointer.Value] = context.Registers[_source];
            context.StackPointer.Increment();
        }
    }

    internal sealed class ExecutePOP : BaseExecute
    {
        private readonly byte _frameSize;

        public ExecutePOP(List<Argument> arguments)
        {
            _frameSize = ((NumberArgument)arguments[0]).Value;
        }

        protected override void ExecuteInstruction(ref CPUContext context)
        {
            context.StackPointer.Decrement(_frameSize);
        }
    }

    internal sealed class ExecutePSHM : BaseExecute
    {
        private readonly byte _frameSize;

        public ExecutePSHM(List<Argument> arguments)
        {
            _frameSize = ((NumberArgument)arguments[0]).Value;
        }

        protected override void ExecuteInstruction(ref CPUContext context)
        {
            context.StackPointer.Increment(_frameSize);
        }
    }

    #endregion

    #region Control Flow Operations

    internal sealed class ExecuteJMP : BaseExecute
    {
        private readonly ushort _address;

        public ExecuteJMP(List<Argument> arguments)
        {
            _address = ((AddressArgument)arguments[0]).Value;
        }

        protected override void ExecuteInstruction(ref CPUContext context)
        {
            context.ProgramCounter.SetBRH(_address);
        }

        public override bool IsControlFlowInstruction => true;
    }
    
    internal sealed class ExecuteBRH : BaseExecute
    {
        private readonly byte _cond;
        private readonly ushort _address;

        public ExecuteBRH(List<Argument> arguments)
        {
            _cond = ((NumberArgument)arguments[0]).Value;
            _address = ((AddressArgument)arguments[1]).Value;
        }

        protected override void ExecuteInstruction(ref CPUContext context)
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

        public override bool IsControlFlowInstruction => true;
    }

    internal sealed class ExecuteCAL : BaseExecute
    {
        private readonly ushort _address;

        public ExecuteCAL(List<Argument> arguments)
        {
            _address = ((AddressArgument)arguments[0]).Value;
        }

        protected override void ExecuteInstruction(ref CPUContext context)
        {
            context.ProgramCounter.PushCAL(_address);
        }

        public override bool IsControlFlowInstruction => true;
    }

    internal sealed class ExecuteRET : BaseExecute
    {
        private readonly byte _frameSize;

        public ExecuteRET(List<Argument> arguments)
        {
            _frameSize = ((NumberArgument)arguments[0]).Value;
        }

        protected override void ExecuteInstruction(ref CPUContext context)
        {
            context.StackPointer.Decrement(_frameSize);
            context.ProgramCounter.PopRET();
        }

        public override bool IsControlFlowInstruction => true;
    }

    #endregion

    #region I/O Operations

    internal sealed class ExecutePST : BaseExecute
    {
        private readonly Register _source;
        private readonly byte _portNumber;

        public ExecutePST(List<Argument> arguments)
        {
            _source = (Register)((RegisterArgument)arguments[0]).Value;
            _portNumber = ((NumberArgument)arguments[1]).Value;
        }

        protected override void ExecuteInstruction(ref CPUContext context)
        {
            context.Ports[_portNumber].PortStore(context.Registers[_source]);
        }
    }

    internal sealed class ExecuteDPS : BaseExecute
    {
        private readonly Register _sourceA;
        private readonly Register _sourceB;
        private readonly byte _portNumber;

        public ExecuteDPS(List<Argument> arguments)
        {
            _sourceA = (Register)((RegisterArgument)arguments[0]).Value;
            _sourceB = (Register)((RegisterArgument)arguments[1]).Value;
            _portNumber = ((NumberArgument)arguments[2]).Value;
        }

        protected override void ExecuteInstruction(ref CPUContext context)
        {
            context.Ports[_portNumber].PortStore(context.Registers[_sourceA]);
            context.Ports[(byte)(_portNumber + 1)].PortStore(context.Registers[_sourceB]);
        }
    }
  
    internal sealed class ExecutePLD : BaseExecute
    {
        private readonly Register _destination;
        private readonly byte _portNumber;

        public ExecutePLD(List<Argument> arguments)
        {
            _destination = (Register)((RegisterArgument)arguments[0]).Value;
            _portNumber = ((NumberArgument)arguments[1]).Value;
        }

        protected override void ExecuteInstruction(ref CPUContext context)
        {
            context.Registers[_destination] = context.Ports[_portNumber].PortLoad();
            context.ZeroFlag = context.Registers[_destination] == 0;
        }
    }







    #endregion







    #endregion

}
