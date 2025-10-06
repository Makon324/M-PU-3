from errors import (
    DuplicateLabelError,
    ProgramTooLongError,
    UndefinedLabelError,
)
from load_instructions import InstructionLoader
from utils import get_number, get_register_number
from constants import AssemblerConstants
import re
from functools import reduce


class AssemblerCodeGenerator:
    """Generates machine code from parsed assembly program.

    Attributes:
        instructions (dict): Loaded instruction specifications from json file
        symbol_table (dict): Mapping of label names to memory addresses
        program (list): Parsed assembly program
    """

    def __init__(self, program: list[dict]):
        self.instructions = InstructionLoader.load_instructions()
        self.symbol_table = {}
        self.program = program

    def _resolve_labels(self) -> None:
        """First pass: build symbol table with label addresses and validate program length.

        Raises:
            DuplicateLabelError: If a label is defined multiple times
            ProgramTooLongError: If program exceeds MAX_PROGRAM_SIZE instructions
        """
        address = 0
        for line in self.program:
            line_type = line["type"]

            if line_type == "label":
                if line["label"] in self.symbol_table:
                    raise DuplicateLabelError(
                        f"label {line['label']} already exists", line["line"]
                    )
                self.symbol_table[line["label"]] = address

            elif line_type == "instruction":
                address += 1
                if address > AssemblerConstants.MAX_PROGRAM_SIZE:
                    raise ProgramTooLongError(
                        f"Program exceeds maximum size of {AssemblerConstants.MAX_PROGRAM_SIZE} "
                        f"instructions (current: {address})",
                        line["line"],
                    )

    @staticmethod
    def _int_to_bin(number: int, n_bits: int) -> str:
        """Convert integer to n_bits-wide binary string using U2 for negatives."""
        if number < 0:
            number = (1 << n_bits) + number  # wrap around for negative values
        return format(number & ((1 << n_bits) - 1), f"0{n_bits}b")

    @staticmethod
    def _transform_operand(num: int, transformations: list[str] | None = None) -> int:
        """Apply transformations to numeric operand according to instruction specification.

        Args:
            num: Original numeric value
            transformations: List of transformations to apply in order:
                - 'neq': Logical NOT (0 becomes 1, non-zero becomes 0)
                - 'div2': Integer division by 2
                - 'dec': Decrement by 1

        Returns:
            Transformed numeric value
        """
        if transformations is None:
            return num

        return reduce(lambda n, t: AssemblerConstants.TRANSFORMATIONS[t](n), transformations, num)

    @staticmethod
    def _replace_placeholder(template: str, start_character: str, number: int) -> str:
        """Replace a placeholder pattern in the template with a binary number."""
        pattern = rf"{start_character}_*"
        match = re.search(pattern, template)  # always matches due to validation
        pos = match.start()
        length = len(match.group(0))
        replaced = (
            template[:pos]
            + AssemblerCodeGenerator._int_to_bin(number, length)
            + template[pos + length :]
        )
        return replaced

    def generate_code(self) -> list[str]:
        """Generate machine code.

        Performs two-pass assembly:
        1. Resolves labels and validates program length
        2. Generates machine code for each instruction line

        Returns:
            List of binary strings representing the machine code program
        """
        machine_code = []
        self._resolve_labels()
        for line in self.program:
            if line["type"] == "instruction":
                instruction_code = self.generate_instruction(line)
                machine_code.append(instruction_code)
        return machine_code

    def generate_instruction(self, instruction: dict) -> str:
        """Generate machine code for single instruction.

        Args:
            instruction: Dictionary returned by parser

        Returns:
            16-bit binary string representing the machine code for this instruction

        Raises:

            UndefinedLabelError: If referenced label doesn't exist in symbol table
        """
        mnemonic = instruction["mnemonic"].upper()
        instruction_spec = self.instructions.get(mnemonic)
        code_template = instruction_spec["code_template"]
        binary_code = code_template

        # Process each operand
        for i, (operand, operand_spec) in enumerate(
            zip(instruction["arguments"], instruction_spec.get("operands"))
        ):
            match operand_spec["type"]:
                case "reg":
                    reg_num = get_register_number(operand)
                    binary_code = self._replace_placeholder(binary_code, "R", reg_num)

                case "num":
                    num = get_number(operand)
                    transformed_num = self._transform_operand(
                        num, operand_spec["transformations"]
                    )
                    binary_code = self._replace_placeholder(
                        binary_code, "N", transformed_num
                    )

                case "adr":
                    if operand.type == "IDENT":
                        adr = self.symbol_table.get(operand.value)
                        if adr is None:
                            raise UndefinedLabelError(
                                f"Undefined label: {operand.value}",
                                line=operand.line,
                                column=operand.start_column,
                            )
                    else:
                        adr = get_number(operand)
                    binary_code = self._replace_placeholder(binary_code, "A", adr)

        return binary_code
