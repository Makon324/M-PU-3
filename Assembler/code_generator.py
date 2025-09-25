from errors import *
from load_instructions import *
from utils import get_number, get_register_number


class AssemblerCodeGenerator:
    """Generate machine code from parsed assembly program."""

    def __init__(self, program: list[dict]):
        self.instructions = load_instructions()
        self.symbol_table = {}
        self.program = program

    def resolve_labels(self) -> None:
        """First pass: build symbol table with label addresses"""
        address = 0
        for line in self.program:
            if line['type'] == 'label':
                if line['label'] in self.symbol_table:
                    raise DuplicateLabelError(f"label {line['label']} already exists", line['line'])
                self.symbol_table[line['label']] = address
            elif line['type'] == 'instruction':
                address += 1

    @staticmethod
    def int_to_bin(number: int, n_bits: int) -> str:
        """Convert integer to n_bits-wide binary string using U2 for negatives."""
        if number < 0:
            number = (1 << n_bits) + number  # wrap around for negative values
        return format(number & ((1 << n_bits) - 1), f'0{n_bits}b')

    @staticmethod
    def transform_operand(num: int, transformations: list[str]) -> int:
        """Apply series of transformations to an operand according to instruction spec."""
        if transformations is None:
            return num
        for transformation in transformations:
            if transformation == 'neq':
                num = not num
            elif transformation == 'div2':
                num = num // 2
            elif transformation == 'dec':
                num -= 1
            else:
                raise InstructionsError(f"Unknown transformation {transformation}")
        return num

    @staticmethod
    def replace_placeholder(template: str, start_character: str, number: int) -> str:
        """Replace a placeholder pattern in the template with a binary number."""
        pos = template.find(start_character)
        count = 1
        i = pos + 1
        while i < len(template) and template[i] == '_':
            count += 1
            i += 1
        return template[:pos] + AssemblerCodeGenerator.int_to_bin(number, count) + template[pos + count:]

    def generate_code(self) -> list[str]:
        """Second pass: generate machine code"""
        machine_code = []
        self.resolve_labels()
        for line in self.program:
            if line['type'] == 'instruction':
                instruction_code = self.generate_instruction(line)
                machine_code.append(instruction_code)
        return machine_code

    def generate_instruction(self, instruction: dict) -> str:
        """Generate machine code for a single instruction."""
        mnemonic = instruction['mnemonic'].upper()
        instruction_spec = self.instructions.get(mnemonic)
        if not instruction_spec:
            raise InvalidInstructionError(f"Unknown instruction: {mnemonic}")
        code_template = instruction_spec['code_template']
        binary_code = code_template

        # Process each operand
        for i, (operand, operand_spec) in enumerate(zip(
                instruction['arguments'],
                instruction_spec.get('operands')
        )):
            if operand_spec['type'] == 'reg':
                reg_num = get_register_number(operand)
                binary_code = self.replace_placeholder(binary_code, "R", reg_num)
            elif operand_spec['type'] == 'num':
                num = get_number(operand)
                transformed_num = self.transform_operand(num, operand_spec['transformations'])
                binary_code = self.replace_placeholder(binary_code, "N", transformed_num)
            elif operand_spec['type'] == 'adr':
                if operand.type == 'IDENT':
                    adr = self.symbol_table.get(operand.value)
                    if adr is None:
                        raise UndefinedLabelError(
                            f"Undefined label: {operand.value}",
                            line=operand.line,
                            column=operand.start_column
                        )
                else:
                    adr = get_number(operand)
                binary_code = self.replace_placeholder(binary_code, "A", adr)

        return binary_code





