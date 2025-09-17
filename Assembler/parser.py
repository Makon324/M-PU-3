from collections import namedtuple
from errors import *
from tokenizer import Token


VALID_OPERAND_TYPES = frozenset({'REGISTER', 'DEC', 'HEX', 'BIN', 'IDENT'})

class AssemblerParser:
    def __init__(self, tokens: list[Token]):
        self.tokens = tokens
        self.pos = 0
        self.line = 0

    def advance(self) -> Token:
        """Advance to the next token and return it."""
        if self.pos >= len(self.tokens):
            return None
        tok = self.tokens[self.pos]
        self.line = tok.line
        self.pos += 1
        return tok

    def collect_operands(self) -> list[Token]:
        """Collect all operands for the current instruction."""
        operands = []
        current_line = self.line

        while current_line == self.line:
            token = self.advance()
            if not token:
                break
            elif token.type in VALID_OPERAND_TYPES:
                operands.append(token)
            else:
                raise InvalidSyntaxError(
                    f"Unexpected token type {token.type} with value {token.value!r}",
                    line=token.line
                )
        return operands

    def parse_line(self) -> dict:
        """Parse the current line of assembly code."""
        tok = self.advance()
        if not tok:
            return None

        if tok.type == 'LABEL':
            return {
                "type": "label",
                "label": tok.value[:-1], # drop ':'
                "line": tok.line
            }

        elif tok.type == 'MNEMONIC':
            args = self.collect_operands()
            return {
                "type": "instruction",
                 "mnemonic": tok.value,
                 "arguments": args,
                 "line": tok.line
            }

        else:
            raise InvalidSyntaxError(
                f"Unexpected token type {tok.type} with value {tok.value!r}",
                line=tok.line
        )

    def parse(self) -> list[dict]:
        program = []
        while self.pos < len(self.tokens):
            line = self.parse_line()
            if line:
                program.append(line)

        return program



