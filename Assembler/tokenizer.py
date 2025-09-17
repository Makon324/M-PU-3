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
    TOKEN_SPECIFICATION = (
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
    )

    def __init__(self):
        self.token_pattern = re.compile(
            "|".join(f"(?P<{name}>{pattern})" for name, pattern in self.TOKEN_SPECIFICATION)
        )

    def tokenize(self, code: str) -> list[Token]:
        tokens = []
        line_num = 1
        line_start = 0 # tracks start index of current line

        for match in self.token_pattern.finditer(code):
            token_type = match.lastgroup
            value = match.group()
            column = match.start() - line_start + 1

            if token_type == 'NEWLINE':
                line_num += 1
                line_start = match.end()
            elif token_type in ('SKIP', 'COMMENT'):
                continue
            elif token_type == 'MISMATCH':
                raise UnexpectedCharError(
                    f"Unexpected char {value!r}",
                    line_num,
                    column
                )
            else:
                tokens.append(
                    Token(
                        type=token_type,
                        value=value,
                        line=line_num,
                        start_column=column
                    )
                )

        return tokens


