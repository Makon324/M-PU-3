import json
from pathlib import Path
from errors import *
from tokenizer import Token


class AssemblerValidator:
    """Validates assembly instructions against a specification file."""

    REGISTER_RANGE = range(0, 8)
    ADDRESS_RANGE = range(0, 1024)

    def __init__(self):
        self.instructions = self._load_instructions()

    def _load_instructions(self, filename: str = "instructions.json") -> dict:
        """Searches for and loads instruction specifications from a JSON file.

        Args:
            filename: Name of the JSON file to load (default: 'instructions.json').

        Returns:
            Dictionary of instruction specifications keyed by mnemonic.

        Raises:
            FileNotFoundError: If the instructions file cannot be found.
            ValueError: If the file contains invalid JSON.
            OSError: If the file cannot be read.
        """
        current_dir = Path(__file__).resolve().parent

        while True:
            candidate = current_dir / filename
            if candidate.exists():
                try:
                    with candidate.open("r", encoding="utf-8") as fh:
                        data = json.load(fh)
                    # Convert list to dictionary keyed by mnemonic
                    return {item['mnemonic']: item for item in data}
                except json.JSONDecodeError as exc:
                    raise ValueError(f"Invalid JSON format in '{candidate}': {exc}") from exc
                except OSError as exc:
                    raise OSError(f"Unable to read '{candidate}': {exc}") from exc

            # Go one directory up
            parent = current_dir.parent
            if parent == current_dir:  # reached filesystem root
                break
            current_dir = parent

        raise FileNotFoundError(
            f"Instructions file '{filename}' not found when searching upward "
            f"from '{Path(__file__).resolve().parent}'."
        )

    @staticmethod
    def is_register(token: Token) -> bool:
        return token.type == 'REGISTER'

    @staticmethod
    def is_number(token: Token) -> bool:
        return token.type in ['HEX', 'BIN', 'DEC']

    @staticmethod
    def is_address(token: Token) -> bool:
        return token.type in ['IDENT', 'HEX', 'BIN', 'DEC']

    @staticmethod
    def get_register_number(token: Token) -> int:
        return int(token.value[1:])

    @staticmethod
    def get_number(token: Token) -> int:
        return int(token.value, 0)

    @staticmethod
    def is_in_range(number: int, range_list) -> bool:
        return range_list[0] <= number <= range_list[1]

    def validate_operand(self, operand: Token, operand_spec: dict):
        """Validates a single operand against its specification."""
        if operand_spec['type'] == 'reg':
            if not self.is_register(operand):
                raise InvalidOperandError(
                    f"Expected register operand, got {operand.type}",
                    line=operand.line, column=operand.start_column
                )

            reg_num = self.get_register_number(operand)
            if not reg_num in self.REGISTER_RANGE:
                raise InvalidRegisterError(
                    f"Invalid register number {reg_num}. Must be between 0 and 7",
                    line=operand.line, column=operand.start_column
                )

        elif operand_spec['type'] == 'num':
            if not self.is_number(operand):
                raise InvalidOperandError(
                    f"Expected numeric operand, got {operand.type}",
                    line=operand.line, column=operand.start_column
                )

            number = self.get_number(operand)
            if not self.is_in_range(number, operand_spec['range']):
                range_desc = f"{operand_spec['range'][0]} to {operand_spec['range'][1]}" if operand_spec[
                    'range'] else "any value"
                raise ValueOutOfRangeError(
                    f"Value {number} is out of range. Expected {range_desc}",
                    line=operand.line, column=operand.start_column
                )

        elif operand_spec['type'] == 'adr':
            if not self.is_address(operand):
                raise InvalidOperandError(
                    f"Expected address operand, got {operand.type}",
                    line=operand.line, column=operand.start_column
                )

            if self.is_number(operand):
                number = self.get_number(operand)
                if not number in self.ADDRESS_RANGE:
                    raise InvalidAddressError(
                        f"Invalid address. Must be between 0 and 1023",
                        line=operand.line, column=operand.start_column
                    )

        else:
            raise InstructionsError(
                f"Unknown operand type in instruction specification: {operand_spec['type']}"
            )

    def validate_instruction(self, instruction: dict):
        """Validates a complete instruction against the specification."""
        mnemonic = instruction['mnemonic'].upper()

        if mnemonic not in self.instructions:
            raise InvalidInstructionError(
                f"Invalid instruction: {mnemonic}",
                line=instruction['line'], column=instruction['column']
            )

        instruction_spec = self.instructions[mnemonic]
        expected_operands = instruction_spec.get('operands', [])

        # Check operand count matches
        if len(instruction['arguments']) != len(expected_operands):
            raise InvalidSyntaxError(
                f"Wrong number of operands for {mnemonic}. "
                f"Expected {len(expected_operands)}, got {len(instruction['arguments'])}",
                line=instruction['line'], column=instruction['column']
            )

        # Validate each operand against its specification
        for operand, operand_spec in zip(instruction['arguments'], expected_operands):
            self.validate_operand(operand, operand_spec)

    def validate(self, program: list[dict]):
        """Validates a complete parsed program."""
        for line in program:
            if line["type"] == "instruction":
                self.validate_instruction(line)
