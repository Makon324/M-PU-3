import pytest
from collections import namedtuple

from tokenizer import *

def test_simple_code():
    tokenizer = AssemblerTokenizer()
    code = \
    """.start
        ADI R1, 123 ; comment
        PST R2, -0x7A
        NOP
    """

    expected = [
        Token(type='LABEL', value='.start', line=1),

        Token(type='MNEMONIC', value='ADI', line=2),
        Token(type='REGISTER', value='R1', line=2),
        Token(type='COMMA', value=',', line=2),
        Token(type='DEC', value='123', line=2),

        Token(type='MNEMONIC', value='PST', line=3),
        Token(type='REGISTER', value='R2', line=3),
        Token(type='COMMA', value=',', line=3),
        Token(type='HEX', value='-0x7A', line=3),

        Token(type='MNEMONIC', value='NOP', line=4)
    ]

    tokens = tokenizer.tokenize(code)
    assert tokens == expected


def test_labels_and_numbers():
    tokenizer = AssemblerTokenizer()
    code = ".start 123 -0x1F 0b101"

    expected = [
        Token(type="LABEL", value=".start", line=1),
        Token(type="DEC", value="123", line=1),
        Token(type="HEX", value="-0x1F", line=1),
        Token(type="BIN", value="0b101", line=1),
    ]
    tokens = tokenizer.tokenize(code)
    assert tokens == expected

def test_unexpected_char():
    tokenizer = AssemblerTokenizer()
    code = "MOV R1, @"

    with pytest.raises(UnexpectedChar) as exc_info:
        tokenizer.tokenize(code)
    assert "Unexpected char '@'" in str(exc_info.value)
    assert exc_info.value.line == 1






