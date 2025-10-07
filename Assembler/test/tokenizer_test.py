import pytest
import textwrap
from tokenizer import *
from errors import *


def test_empty():
    tokenizer = AssemblerTokenizer()
    code = ""
    expected = []
    tokens = tokenizer.tokenize(code)
    assert tokens == expected


def test_simple_code():
    tokenizer = AssemblerTokenizer()
    code = textwrap.dedent(
        """\
        .start:\r
        ADI R1, 123 ; comment
        PST R2, -0x7A
        NOP
    """
    )

    expected = [
        Token(type="LABEL", value=".start:", line=1, start_column=1),
        Token(type="MNEMONIC", value="ADI", line=2, start_column=1),
        Token(type="REGISTER", value="R1", line=2, start_column=5),
        Token(type="DEC", value="123", line=2, start_column=9),
        Token(type="MNEMONIC", value="PST", line=3, start_column=1),
        Token(type="REGISTER", value="R2", line=3, start_column=5),
        Token(type="HEX", value="-0x7A", line=3, start_column=9),
        Token(type="MNEMONIC", value="NOP", line=4, start_column=1),
    ]

    tokens = tokenizer.tokenize(code)
    assert tokens == expected


def test_labels_and_numbers():
    tokenizer = AssemblerTokenizer()
    code = ".start 123 -0x1F 0b101"

    expected = [
        Token(type="IDENT", value=".start", line=1, start_column=1),
        Token(type="DEC", value="123", line=1, start_column=8),
        Token(type="HEX", value="-0x1F", line=1, start_column=12),
        Token(type="BIN", value="0b101", line=1, start_column=18),
    ]
    tokens = tokenizer.tokenize(code)
    assert tokens == expected


def test_unexpected_char():
    tokenizer = AssemblerTokenizer()
    code = "MOV R1, @"

    with pytest.raises(UnexpectedCharError) as exc_info:
        tokenizer.tokenize(code)
    assert "Unexpected char '@'" in str(exc_info.value)
    assert exc_info.value.line == 1
    assert exc_info.value.column == 9
