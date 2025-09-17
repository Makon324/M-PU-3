import re
from collections import namedtuple


Token = namedtuple("Token", ["type", "value", "line"])

class LexerError(Exception):
    """Base class for all lexer errors."""
    def __init__(self, line, message):
        self.line = line
        self.message = message
        super().__init__(f"Error on {line}: {message}")

class UnexpectedChar(LexerError):
    """Raised when an unexpected character is encountered in input."""

class AssemblerTokenizer:
    def __init__(self):
        self.token_specification = [
            ('COMMENT',     r';[^\n]*'),
            ('LABEL',       r'\.[A-Za-z_][A-Za-z0-9_]+'),
            ('REGISTER',    r'R[0-9]+'),
            ('HEX',         r'-?0x[0-9A-Fa-f]+'),
            ('BIN',         r'-?0b[01]+'),
            ('DEC',         r'-?[0-9]+'),
            ('MNEMONIC',    r'[A-Za-z]+'),
            ('COMMA',       r','),
            ('NEWLINE',     r'\n'),
            ('SKIP',        r'[\t ]+'),
            ('MISMATCH',    r'.')
        ]

        self.master_pattern = re.compile(
            "|".join(f"(?P<{name}>{pattern})" for name, pattern in self.token_specification)
        )

    def tokenize(self, code: str) -> list[Token]:
        tokens = []
        line_num = 1

        for mo in self.master_pattern.finditer(code):
            kind = mo.lastgroup
            value = mo.group()
            if kind == 'NEWLINE':
                line_num += 1
            elif kind in ('SKIP', 'COMMENT'):
                continue
            elif kind == 'MISMATCH':
                raise UnexpectedChar(line_num, f"Unexpected char {value!r}")
            else:
                tokens.append(
                    Token(type=kind, value=value, line=line_num)
                )

        return tokens


