import re
from dataclasses import dataclass
from errors import *


@dataclass
class Token:
    type: str
    value: str
    line: int = None
    start_column: int = None


class AssemblerTokenizer:
    def __init__(self):
        self.token_specification =[
            ('COMMENT', r';[^\n]*'),
            ('LABEL', r'\.[A-Za-z_][A-Za-z0-9_]*:'),
            ('IDENT', r'\.[A-Za-z_][A-Za-z0-9_]*'),
            ('REGISTER', r'R[0-9]+'),
            ('HEX', r'-?0x[0-9A-Fa-f]+'),
            ('BIN', r'-?0b[01]+'),
            ('DEC', r'-?[0-9]+'),
            ('MNEMONIC', r'[A-Za-z]+'),
            ('NEWLINE', r'\n'),
            ('SKIP', r'[\t ,]+'),
            ('MISMATCH', r'.')
        ]

        self.master_pattern = re.compile(
            "|".join(f"(?P<{name}>{pattern})" for name, pattern in self.token_specification)
        )

    def tokenize(self, code: str) -> list[Token]:
        tokens = []
        line_num = 1
        line_start = 0 # tracks start index of current line

        for mo in self.master_pattern.finditer(code):
            kind = mo.lastgroup
            value = mo.group()
            column = mo.start() - line_start + 1

            if kind == 'NEWLINE':
                line_num += 1
                line_start = mo.end()
            elif kind in ('SKIP', 'COMMENT'):
                continue
            elif kind == 'MISMATCH':
                raise UnexpectedChar(f"Unexpected char {value!r}", line_num, column)
            else:
                tokens.append(
                    Token(type=kind, value=value, line=line_num, start_column=column)
                )

        return tokens


