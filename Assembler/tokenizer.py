import re
from dataclasses import dataclass

from constants import AssemblerConstants
from errors import UnexpectedCharError


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
    line: int
    start_column: int


def make_token(token_type, value, line=1, column=1):
    return Token(type=token_type, value=value, line=line, start_column=column)


def make_tokens(to_build: list[tuple]):
    """Helper to build tokens list:
    make_tokens(("MNEMONIC", "MOV", 1, 1), ("REGISTER", "R1", 1, 5))
    """
    tokens: list[Token] = []
    for spec in to_build:
        tokens.append(make_token(*spec))
    return tokens


class AssemblerTokenizer:
    """Converts assembly code into tokens using regular expression patterns."""

    def __init__(self):
        self.token_pattern = re.compile(
            "|".join(
                f"(?P<{name}>{pattern})" for name, pattern in AssemblerConstants.TOKEN_SPECIFICATION
            )
        )

    def tokenize(self, code: str) -> list[Token]:
        """Converts a string of assembly code into a list of tokens.

        Args:
            code: The assembly code to tokenize.

        Returns:
            List of Token objects representing the lexical elements of code.

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

            match token_type:
                case "NEWLINE":
                    line_num += 1
                    line_start = match.end()
                case "SKIP" | "COMMENT":
                    continue
                case "MISMATCH":
                    raise UnexpectedCharError(
                        f"Unexpected char {value!r}", line_num, column
                    )
                case _:
                    tokens.append(
                        Token(
                            type=token_type,
                            value=value,
                            line=line_num,
                            start_column=column,
                        )
                    )

        return tokens
