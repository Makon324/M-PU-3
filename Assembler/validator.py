from errors import (
    InvalidOperandError,
    InvalidRegisterError,
    InvalidAddressError,
    ValueOutOfRangeError,
    InvalidInstructionError,
    InvalidSyntaxError,
)
from utils import (
    Token,
    is_register,
    is_number,
    is_address,
    is_in_range,
    get_register_number,
    get_number,
)
from load_instructions import InstructionLoader
from constants import AssemblerConstants


class AssemblerValidator:
    """Validates assembly instructions against a specification file."""

    def __init__(self):
        self.instructions = InstructionLoader.load_instructions()

    def validate(self, program: list[dict]):
        """Validates a complete parsed program."""
        for line in program:
            if line["type"] == "instruction":
                self.validate_instruction(line)

    def validate_instruction(self, instruction: dict):
        """
        Validate a single instruction against the instruction specification.

        Checks:
        - Instruction mnemonic exists in specification
        - Correct number of operands
        - Each operand matches its expected type and constraints

        Args:
            instruction: Dictionary returned by parser

        Raises:
            InvalidInstructionError: If mnemonic is not recognized
            InvalidSyntaxError: If operand count doesn't match specification
        """
        mnemonic = instruction["mnemonic"].upper()

        if mnemonic not in self.instructions:
            raise InvalidInstructionError(
                f"Invalid instruction: {mnemonic}",
                line=instruction["line"],
                column=instruction["column"],
            )

        instruction_spec = self.instructions[mnemonic]
        expected_operands = instruction_spec.get("operands", [])

        # Check operand count matches
        if len(instruction["arguments"]) != len(expected_operands):
            raise InvalidSyntaxError(
                f"Wrong number of operands for {mnemonic}. "
                f"Expected {len(expected_operands)}, got {len(instruction['arguments'])}",
                line=instruction["line"],
                column=instruction["column"],
            )

        # Validate each operand against its specification
        for operand, operand_spec in zip(instruction["arguments"], expected_operands):
            self.validate_operand(operand, operand_spec)

    def validate_operand(self, operand: Token, operand_spec: dict):
        """
        Validate a single operand against its type specification.

        Delegates to specific validation methods based on operand type.

        Args:
            operand: Token object representing the operand
            operand_spec: Dictionary containing operand specification with keys:
                - type: Operand type ('reg', 'num', 'adr')
                - range: For numeric operands, the allowed value range

        Raises:
            InvalidOperandError: If operand type doesn't match specification
        """
        match operand_spec["type"]:
            case "reg":
                self._validate_register_operand(operand)
            case "num":
                self._validate_number_operand(operand, operand_spec)
            case "adr":
                self._validate_address_operand(operand)
            case _:
                raise InvalidSyntaxError(
                    f"Unknown operand type: {operand_spec['type']!r}"
                )

    @staticmethod
    def _validate_register_operand(operand: Token) -> None:
        """Validate register operand."""
        if not is_register(operand):
            raise InvalidOperandError(
                f"Expected register operand, got {operand.type}",
                line=operand.line,
                column=operand.start_column,
            )

        reg_num = get_register_number(operand)
        if not reg_num in AssemblerConstants.REGISTER_RANGE:
            raise InvalidRegisterError(
                f"Invalid register number {reg_num}. Must be between 0 and 7",
                line=operand.line,
                column=operand.start_column,
            )

    @staticmethod
    def _validate_number_operand(operand: Token, operand_spec: dict) -> None:
        """Validate number operand against its specification."""
        if not is_number(operand):
            raise InvalidOperandError(
                f"Expected numeric operand, got {operand.type}",
                line=operand.line,
                column=operand.start_column,
            )

        number = get_number(operand)
        if not is_in_range(number, operand_spec["range"]):
            range_desc = (
                f"{operand_spec['range'][0]} to {operand_spec['range'][1]}"
                if operand_spec["range"]
                else "any value"
            )
            raise ValueOutOfRangeError(
                f"Value {number} is out of range. Expected {range_desc}",
                line=operand.line,
                column=operand.start_column,
            )

    @staticmethod
    def _validate_address_operand(operand: Token) -> None:
        """Validate address operand."""
        if not is_address(operand):
            raise InvalidOperandError(
                f"Expected address operand, got {operand.type}",
                line=operand.line,
                column=operand.start_column,
            )

        if is_number(operand):
            number = get_number(operand)
            if not number in AssemblerConstants.ADDRESS_RANGE:
                raise InvalidAddressError(
                    f"Invalid address. Must be between 0 and 1023",
                    line=operand.line,
                    column=operand.start_column,
                )
