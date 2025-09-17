from collections import namedtuple
from errors import *
from tokenizer import Token


VALID_OPERAND_TYPES = {'REGISTER', 'DEC', 'HEX', 'BIN', 'IDENT'}

class AssemblerParser:
    def __init__(self, tokens: list[Token]):
        self.tokens = tokens
        self.pos = 0
        self.line = 0

    def advance(self) -> Token:
        if self.pos >= len(self.tokens):
            return None
        tok = self.tokens[self.pos]
        self.line = tok.line
        self.pos += 1
        return tok

    def collect_operands(self) -> list[Token]:
        operands = []
        line = self.line
        while line == self.line:
            tok = self.advance()
            if not tok:
                break
            elif tok.type in VALID_OPERAND_TYPES:
                operands.append(tok)
            else:
                raise InvalidSyntax(
                    f"Unexpected token type {tok.type} with value {tok.value!r}",
                    line=tok.line
                )
        return operands

    def parse_line(self) -> dict:
        tok = self.advance()
        if not tok:
            return None

        if tok.type == 'LABEL':
            return {"type": "label",
                    "label": tok.value[:-1], # drop ':'
                    "line": tok.line}

        elif tok.type == 'MNEMONIC':
            args = self.collect_operands()
            return {"type": "instruction",
                    "mnemonic": tok.value,
                    "arguments": args,
                    "line": tok.line}

        else:
            raise InvalidSyntax(
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



