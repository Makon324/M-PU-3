import pytest
from tokenizer import make_tokens
from parser import *


def test_empty():
    tokens = []
    parser = AssemblerParser(tokens)
    result = parser.parse()
    assert result == []

def test_label():
    tokens = make_tokens([("LABEL", ".start:", 1, 1)])
    parser = AssemblerParser(tokens)
    result = parser.parse()
    assert result == [{"type": "label", "label": ".start", "line": 1, "column": 1}]

def test_parse_instruction():
    tokens = make_tokens([
        ("MNEMONIC", "MOV", 1, 1),
        ("REGISTER", "R1", 1, 5),
        ("DEC", "10", 1, 8)
    ])
    parser = AssemblerParser(tokens)
    result = parser.parse()
    assert len(result) == 1
    instr = result[0]
    assert instr["type"] == "instruction"
    assert instr["mnemonic"] == "MOV"
    assert [tok for tok in instr["arguments"]] == [Token("REGISTER", "R1", 1, 5), Token("DEC", "10", 1, 8)]

def test_multiple_lines():
    tokens = make_tokens([
        ("LABEL", ".loop:", 1, 1),
        ("MNEMONIC", "ADD", 2, 1),
        ("REGISTER", "R1", 2, 5),
        ("REGISTER", "R2", 2, 8)
    ])
    parser = AssemblerParser(tokens)
    result = parser.parse()
    assert len(result) == 2
    assert result[0]["type"] == "label"
    assert result[1]["mnemonic"] == "ADD"


