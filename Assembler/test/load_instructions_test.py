import pytest
from unittest.mock import patch, mock_open
from pathlib import Path
import json
from load_instructions import load_instructions


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


@pytest.fixture(autouse=True)
def clear_caches():
    load_instructions.cache_clear()

@pytest.fixture
def mock_instructions_file():
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
        yield


def test_load_instructions(mock_instructions_file):
    """Test successful loading of instructions"""
    instructions = load_instructions()
    expected = {item['mnemonic']: item for item in MOCK_INSTRUCTIONS}
    assert instructions == expected


def test_load_instructions_file_not_found():
    """Test handling of missing instructions file"""
    with patch("pathlib.Path.exists", return_value=False):
        with pytest.raises(FileNotFoundError):
            load_instructions()


def test_load_instructions_invalid_json():
    """Test loading instructions with invalid JSON content"""
    mock_data = "invalid json content"
    with patch("pathlib.Path.exists", return_value=True), \
            patch("pathlib.Path.open", mock_open(read_data=mock_data)):
        with pytest.raises(ValueError, match="Invalid JSON format"):
            load_instructions()


def test_load_instructions_file_read_error():
    """Test handling of file read errors"""
    with patch("pathlib.Path.exists", return_value=True), \
            patch("pathlib.Path.open") as mock_file:
        mock_file.side_effect = OSError("Permission denied")
        with pytest.raises(OSError, match="Unable to read"):
            load_instructions()