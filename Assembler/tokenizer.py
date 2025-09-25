import re
from dataclasses import dataclass
from errors import *


@dataclass
class Token:
    """Represents a lexical token with type, value, and position information.

    Attributes:
        type: Token type (e.g., 'MNEMONIC', 'REGISTER').
        value: The actual text value of the token.
        line: Line number where the token appears (1-indexed).
        start_column: Starting column position of the token (1-indexed).
    """
    type: str
    value: str
    line: int = None
    start_column: int = None

def make_token(token_type, value, line=1, column=1):
    return Token(type=token_type, value=value, line=line, start_column=column)

def make_tokens(to_build: list[tuple]):
    """Helper to build tokens list:
    make_tokens(("MNEMONIC", "MOV", 1, 1), ("REGISTER", "R1", 1, 5))
    """
    tokens = []
    for t in to_build:
        if len(t) == 2:
            tokens.append(make_token(t[0], t[1]))
        elif len(t) == 3:
            tokens.append(make_token(t[0], t[1], t[2]))
        elif len(t) == 4:
            tokens.append(make_token(t[0], t[1], t[2], t[3]))
        else:
            raise ValueError(f"Tuple must have 2, 3, or 4 elements, got {len(t)}")
    return tokens


class AssemblerTokenizer:
    """Converts assembly code into tokens using regular expression patterns."""

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
        """Converts a string of assembly code into a list of tokens.

        Args:
            code: The assembly code to tokenize.

        Returns:
            List of Token objects representing the lexical elements of the code.

        Raises:
            UnexpectedCharError: If an unrecognized character is encountered.
        """
        tokens = []
        line_num = 1
        line_start = 0  # tracks start index of current line

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


