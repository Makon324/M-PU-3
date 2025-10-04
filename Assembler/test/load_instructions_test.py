import pytest
from unittest.mock import patch, mock_open
from pathlib import Path
import json
from load_instructions import InstructionLoader
from errors import InstructionFormatError

MOCK_INSTRUCTIONS = [
    {
        "mnemonic": "TEST1",
        "description": "Test instruction 1",
        "operands": [
            {"type": "num", "range": [0, 100], "description": "Test parameter"},
            {"type": "reg", "description": "Test register"},
        ],
        "code_template": "00001N_______R__",
    },
    {
        "mnemonic": "TEST2",
        "description": "Test instruction 2",
        "operands": [
            {"type": "num", "range": [0, 3], "description": "Test parameter"},
            {"type": "adr", "description": "Test address"},
        ],
        "code_template": "0011N_A_________",
    },
]

SCRIPT_DIR = Path(__file__).parent
TARGET_FILE = SCRIPT_DIR.parent.parent / "instructions.json"


@pytest.fixture(autouse=True)
def clear_caches():
    InstructionLoader.clear_cache()


@pytest.fixture
def my_fake_fs(fs):
    """Create a proper fake filesystem structure that find_root() can detect"""
    test_dir = Path(__file__).resolve().parent

    root_dir = test_dir.parent.parent
    fs.create_dir(root_dir)

    git_dir = root_dir / ".git"
    fs.create_dir(git_dir)

    json_file = root_dir / "instructions.json"
    fs.create_file(json_file, contents=json.dumps(MOCK_INSTRUCTIONS))

    return fs


def test_load_instructions(my_fake_fs):
    """Test successful loading of instructions"""
    instructions = InstructionLoader.load_instructions()
    expected = {item["mnemonic"]: item for item in MOCK_INSTRUCTIONS}
    assert instructions == expected


def test_load_instructions_file_not_found():
    """Test handling of missing instructions file"""
    with patch.object(
        InstructionLoader, "find_root", return_value=Path("/fake/root")
    ), patch("pathlib.Path.exists", return_value=False):
        with pytest.raises(FileNotFoundError):
            InstructionLoader.load_instructions()


def test_load_instructions_invalid_json():
    """Test loading instructions with invalid JSON content"""
    mock_data = "invalid json content"

    with patch.object(
        InstructionLoader, "find_root", return_value=Path("/fake/root")
    ), patch("pathlib.Path.exists", return_value=True), patch(
        "pathlib.Path.open", mock_open(read_data=mock_data)
    ):
        with pytest.raises(InstructionFormatError, match=r"Invalid JSON.*"):
            InstructionLoader.load_instructions()


def test_load_instructions_file_read_error():
    """Test handling of file read errors"""
    with patch.object(
        InstructionLoader, "find_root", return_value=Path("/fake/root")
    ), patch("pathlib.Path.exists", return_value=True), patch(
        "pathlib.Path.open"
    ) as mock_file:
        mock_file.side_effect = OSError("Permission denied")
        with pytest.raises(OSError, match="Unable to read"):
            InstructionLoader.load_instructions()
