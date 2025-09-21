import pytest
import json
from pathlib import Path
from unittest.mock import patch, mock_open
from validator import AssemblerValidator
from errors import *
from tokenizer import make_tokens, make_token


MOCK_INSTRUCTIONS = [
    {
        "mnemonic": "TEST1",
        "description": "Test instruction 1",
        "operands": [
            {
                "type": "num",
                "range": [0, 100],
                "description": "Test parameter"
            },
            {
                "type": "reg",
                "description": "Test register"
            }
        ],
        "code": "00001N_______R__"
    },
    {
        "mnemonic": "TEST2",
        "description": "Test instruction 2",
        "operands": [
            {
                "type": "num",
                "range": [0, 3],
                "description": "Test parameter"
            },
            {
                "type": "adr",
                "description": "Test address"
            }
        ],
        "code": "0011N_A_________"
    }
]

SCRIPT_DIR = Path(__file__).parent
TARGET_FILE = SCRIPT_DIR.parent.parent / "instructions.json"


@pytest.fixture
def validator():
    mock_data = json.dumps(MOCK_INSTRUCTIONS)

    original_exists = Path.exists
    original_open = Path.open

    def new_exists(self):
        if self.resolve() == TARGET_FILE.resolve():
            return True
        return original_exists(self)

    def new_open(self, *args, **kwargs):
        if self.resolve() == TARGET_FILE.resolve():
            return mock_open(read_data=mock_data)(*args, **kwargs)
        return original_open(self, *args, **kwargs)

    with patch.object(Path, 'exists', new_exists), \
            patch.object(Path, 'open', new_open):
        validator = AssemblerValidator()
        yield validator

def test_load_instructions(validator):
    assert validator.instructions == {item['mnemonic']: item for item in MOCK_INSTRUCTIONS}

def test_load_instructions_file_not_found():
    with patch("pathlib.Path.exists", return_value=False):
        with pytest.raises(FileNotFoundError):
            AssemblerValidator()

def test_load_instructions_invalid_json():
    """Test loading instructions with invalid JSON content"""
    mock_data = "invalid json content"

    with patch("pathlib.Path.exists", return_value=True), \
            patch("pathlib.Path.open", mock_open(read_data=mock_data)):
        with pytest.raises(ValueError, match="Invalid JSON format"):
            AssemblerValidator()

def test_load_instructions_file_read_error():
    """Test handling of file read errors"""
    with patch("pathlib.Path.exists", return_value=True), \
            patch("pathlib.Path.open") as mock_file:
        mock_file.side_effect = OSError("Permission denied")

        with pytest.raises(OSError, match="Unable to read"):
            AssemblerValidator()

def test_validate_register_operand_valid(validator):
    """Test validating valid register operands"""
    token = make_token('REGISTER', 'R3')
    validator.validate_operand(token, {'type': 'reg'})

def test_validate_register_operand_invalid_type(validator):
    """Test validating non-register token as register operand"""
    token = make_token('DEC', '5')
    with pytest.raises(InvalidOperandError, match="Expected register operand"):
        validator.validate_operand(token, {'type': 'reg'})

def test_validate_register_operand_out_of_range(validator):
    """Test validating out-of-range register number"""
    token = make_token('REGISTER', 'R8')
    with pytest.raises(InvalidRegisterError, match="Invalid register number 8"):
        validator.validate_operand(token, {'type': 'reg'})

def test_validate_number_operand_valid(validator):
    """Test validating valid number operands"""
    token = make_token('DEC', '50')
    validator.validate_operand(token, {'type': 'num', 'range': [0, 100]})

def test_validate_number_operand_invalid_type(validator):
    """Test validating non-number token as number operand"""
    token = make_token('REGISTER', 'R1')
    with pytest.raises(InvalidOperandError, match="Expected numeric operand"):
        validator.validate_operand(token, {'type': 'num', 'range': [0, 100]})

def test_validate_number_operand_out_of_range(validator):
    """Test validating out-of-range number"""
    token = make_token('DEC', '150')
    with pytest.raises(ValueOutOfRangeError, match="Value 150 is out of range"):
        validator.validate_operand(token, {'type': 'num', 'range': [0, 100]})

def test_validate_address_operand_valid_number(validator):
    """Test validating valid address as number"""
    token = make_token('DEC', '500')
    validator.validate_operand(token, {'type': 'adr'})

def test_validate_address_operand_valid_ident(validator):
    """Test validating valid address as identifier"""
    token = make_token('IDENT', '.label')
    validator.validate_operand(token, {'type': 'adr'})

def test_validate_address_operand_invalid_type(validator):
    """Test validating invalid token type as address"""
    token = make_token('MNEMONIC', 'ADD')
    with pytest.raises(InvalidOperandError, match="Expected address operand"):
        validator.validate_operand(token, {'type': 'adr'})

def test_validate_address_operand_out_of_range(validator):
    """Test validating out-of-range address"""
    token = make_token('DEC', '2000')
    with pytest.raises(InvalidAddressError, match="Invalid address"):
        validator.validate_operand(token, {'type': 'adr'})

def test_validate_instruction_valid(validator):
    """Test validating valid instruction"""
    instruction = {
        'mnemonic': 'TEST1',
        'arguments':
            make_tokens([
                ('DEC', '50'),
                ('REGISTER', 'R2')
            ]),
        'line': 1,
        'column': 1
    }
    validator.validate_instruction(instruction)

def test_validate_instruction_invalid_mnemonic(validator):
    """Test validating invalid instruction mnemonic"""
    instruction = {
        'mnemonic': 'INVALID',
        'arguments': [],
        'line': 1,
        'column': 1
    }
    with pytest.raises(InvalidInstructionError, match="Invalid instruction"):
        validator.validate_instruction(instruction)

def test_validate_instruction_wrong_operand_count(validator):
    """Test validating instruction with wrong number of operands"""
    instruction = {
        'mnemonic': 'TEST1',
        'arguments': [make_token('DEC', '50')],  # Missing second operand
        'line': 1,
        'column': 1
    }
    with pytest.raises(InvalidSyntaxError, match="Wrong number of operands"):
        validator.validate_instruction(instruction)

def test_validate_program_valid(validator):
    """Test validating valid program"""
    program = [
        {
            'type': 'instruction',
            'mnemonic': 'TEST1',
            'arguments':
                make_tokens([
                    ('DEC', '50'),
                    ('REGISTER', 'R2')
                ]),
            'line': 1,
            'column': 1
        }
    ]
    validator.validate(program)

def test_validate_program_invalid(validator):
    """Test validating invalid program"""
    program = [
        {
            'type': 'instruction',
            'mnemonic': 'TEST1',
            'arguments':
                make_tokens([
                    ('DEC', '150'),  # Out of range
                    ('REGISTER', 'R2')
                ]),
            'line': 1,
            'column': 1
        }
    ]
    with pytest.raises(ValueOutOfRangeError):
        validator.validate(program)











