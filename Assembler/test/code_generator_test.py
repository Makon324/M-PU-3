import pytest
from unittest.mock import patch, mock_open
import json
from code_generator import *
from errors import *
from tokenizer import make_tokens, make_token


MOCK_INSTRUCTIONS = [
    {
        "mnemonic": "MOV",
        "description": "Move value to register",
        "operands": [
            {
                "type": "reg",
                "description": "Destination register"
            },
            {
                "type": "num",
                "range": [0, 255],
                "transformations": [],
                "description": "Immediate value"
            }
        ],
        "code_template": "00001R__N_______"
    },
    {
        "mnemonic": "JMP",
        "description": "Jump to address",
        "operands": [
            {
                "type": "adr",
                "description": "Target address"
            }
        ],
        "code_template": "00010A__________"
    },
    {
        "mnemonic": "ADD",
        "description": "Add registers",
        "operands": [
            {
                "type": "reg",
                "description": "Destination register"
            },
            {
                "type": "reg",
                "description": "Source register"
            }
        ],
        "code_template": "00011R__R__00000"
    },
    {
        "mnemonic": "ADI",
        "description": "Add immediate with transformation",
        "operands": [
            {
                "type": "reg",
                "description": "Destination register"
            },
            {
                "type": "num",
                "range": [0, 30],
                "transformations": ["div2", "dec"],
                "description": "Immediate value (transformed)"
            }
        ],
        "code_template": "00100R__N_______"
    }
]


@pytest.fixture
def mock_instructions():
    """Fixture to mock the load_instructions function"""
    with patch('code_generator.load_instructions') as mock_load:
        mock_load.return_value = {item['mnemonic']: item for item in MOCK_INSTRUCTIONS}
        yield mock_load


@pytest.mark.parametrize("number, bits, expected", [
    (5, 4, "0101"),
    (255, 8, "11111111"),
    (0, 4, "0000"),
    (-1, 4, "1111"),  # negative
    (17, 4, "0001")  # wrap around
])
def test_int_to_bin(number, bits, expected):
    assert AssemblerCodeGenerator.int_to_bin(number, bits) == expected


def test_transform_operand_no_transformation():
    """Test operand transformation with no transformations"""
    result = AssemblerCodeGenerator.transform_operand(10, [])
    assert result == 10


def test_transform_operand_neq():
    """Test NOT transformation"""
    result = AssemblerCodeGenerator.transform_operand(1, ['neq'])
    assert result == 0
    result = AssemblerCodeGenerator.transform_operand(0, ['neq'])
    assert result == 1


def test_transform_operand_div2():
    """Test divide by 2 transformation"""
    result = AssemblerCodeGenerator.transform_operand(10, ['div2'])
    assert result == 5
    result = AssemblerCodeGenerator.transform_operand(9, ['div2'])
    assert result == 4


def test_transform_operand_dec():
    """Test decrement transformation"""
    result = AssemblerCodeGenerator.transform_operand(10, ['dec'])
    assert result == 9
    result = AssemblerCodeGenerator.transform_operand(-4, ['dec'])
    assert result == -5


def test_transform_operand_multiple():
    """Test multiple transformations applied in order"""
    result = AssemblerCodeGenerator.transform_operand(10, ['div2', 'dec'])
    assert result == 4, f"10 -> div2=5 -> dec=4, but got {result}"


def test_transform_operand_invalid_transformation():
    """Test no transformations"""
    assert AssemblerCodeGenerator.transform_operand(10, None) is 10


def test_replace_placeholder():
    """Test replacing placeholders in template"""
    template = "00001R__N_______"
    result = AssemblerCodeGenerator.replace_placeholder(template, "R", 5)
    assert result == "00001101N_______"
    result = AssemblerCodeGenerator.replace_placeholder(result, "N", 10)
    assert result == "0000110100001010"


def test_resolve_labels(mock_instructions):
    """Test label resolution in first pass"""
    program = [
        {"type": "label", "label": "start", "line": 1},
        {"type": "instruction", "mnemonic": "MOV", "arguments": [], "line": 1},
        {"type": "label", "label": "loop", "line": 2},
        {"type": "instruction", "mnemonic": "ADD", "arguments": [], "line": 2},
        {"type": "instruction", "mnemonic": "JMP", "arguments": [], "line": 3},
    ]

    generator = AssemblerCodeGenerator(program)
    generator.resolve_labels()

    assert generator.symbol_table == {"start": 0, "loop": 1}


def test_resolve_labels_duplicate_error(mock_instructions):
    """Test duplicate labels raise an error"""
    program = [
        {"type": "label", "label": "start", "line": 1},
        {"type": "instruction", "mnemonic": "MOV", "arguments": [], "line": 1},
        {"type": "label", "label": "start", "line": 2},  # Duplicate
    ]

    generator = AssemblerCodeGenerator(program)
    with pytest.raises(DuplicateLabelError, match="label start already exists"):
        generator.resolve_labels()


def test_generate_instruction_register_number(mock_instructions):
    """Test generating instruction with register and number operands"""
    program = [
        {
            "type": "instruction",
            "mnemonic": "MOV",
            "arguments": make_tokens([("REGISTER", "R2"), ("DEC", "100")]),
            "line": 1
        }
    ]

    generator = AssemblerCodeGenerator(program)
    generator.resolve_labels()

    instruction_code = generator.generate_instruction(program[0])
    assert instruction_code == "0000101001100100"  # R2=010, 100=01100100


def test_generate_instruction_address_label(mock_instructions):
    """Test generating instruction with address label"""
    program = [
        {"type": "label", "label": "target", "line": 1},
        {
            "type": "instruction",
            "mnemonic": "JMP",
            "arguments": [make_token("IDENT", "target")],
            "line": 2
        }
    ]

    generator = AssemblerCodeGenerator(program)
    generator.resolve_labels()

    instruction_code = generator.generate_instruction(program[1])
    assert instruction_code == "0001000000000000"  # Address 0


def test_generate_instruction_address_number(mock_instructions):
    """Test generating instruction with numeric address"""
    program = [
        {
            "type": "instruction",
            "mnemonic": "JMP",
            "arguments": [make_token("DEC", "42")],
            "line": 1
        }
    ]

    generator = AssemblerCodeGenerator(program)
    generator.resolve_labels()

    instruction_code = generator.generate_instruction(program[0])
    assert instruction_code == "0001000000101010"


def test_generate_instruction_undefined_label(mock_instructions):
    """Test that undefined label raises error"""
    program = [
        {
            "type": "instruction",
            "mnemonic": "JMP",
            "arguments": [make_token("IDENT", "undefined")],
            "line": 1
        }
    ]

    generator = AssemblerCodeGenerator(program)
    generator.resolve_labels()

    with pytest.raises(UndefinedLabelError, match="Undefined label: undefined"):
        generator.generate_instruction(program[0])


def test_generate_instruction_with_transformations(mock_instructions):
    """Test generating instruction with operand transformations"""
    program = [
        {
            "type": "instruction",
            "mnemonic": "ADI",
            "arguments": make_tokens([("REGISTER", "R1"), ("DEC", "10")]),
            "line": 1
        }
    ]

    generator = AssemblerCodeGenerator(program)
    generator.resolve_labels()

    instruction_code = generator.generate_instruction(program[0])
    expected_code = "0010000100000100"
    assert instruction_code == expected_code, \
        f"expected '{expected_code}' (10->div2=5->dec=4), got '{instruction_code}'"


def test_generate_code(mock_instructions):
    """Test complete code generation"""
    program = [
        {"type": "label", "label": "start", "line": 1},
        {
            "type": "instruction",
            "mnemonic": "MOV",
            "arguments": make_tokens([("REGISTER", "R1"), ("DEC", "5")]),
            "line": 2
        },
        {"type": "label", "label": "loop", "line": 3},
        {
            "type": "instruction",
            "mnemonic": "ADD",
            "arguments": make_tokens([("REGISTER", "R1"), ("REGISTER", "R2")]),
            "line": 4
        },
        {
            "type": "instruction",
            "mnemonic": "JMP",
            "arguments": [make_token("IDENT", "loop")],
            "line": 5
        }
    ]

    generator = AssemblerCodeGenerator(program)
    machine_code = generator.generate_code()

    assert len(machine_code) == 3
    assert machine_code[0] == "0000100100000101"  # MOV R1, 5
    assert machine_code[1] == "0001100101000000"  # ADD R1, R2
    assert machine_code[2] == "0001000000000001"  # JMP loop (address 1)























