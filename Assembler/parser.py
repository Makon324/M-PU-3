from errors import InvalidSyntaxError
from tokenizer import Token


class AssemblerParser:
    """Parses tokens into program structure with labels and instructions.

    Attributes:
        tokens: List of tokens to parse.
        pos: Current position in the token list.
        line: Current line number being parsed.
    """

    _VALID_OPERAND_TYPES = frozenset({"REGISTER", "DEC", "HEX", "BIN", "IDENT"})

    def __init__(self, tokens: list[Token]):
        self.tokens = tokens
        self.pos = 0
        self.line = 0

    def _advance(self) -> Token | None:
        """Advance to the next token and return it."""
        if self.pos >= len(self.tokens):
            return None
        tok = self.tokens[self.pos]
        self.line = tok.line
        self.pos += 1
        return tok

    def _collect_operands(self) -> list[Token]:
        """Collect all operands for the current instruction."""
        operands = []
        current_line = self.line

        while True:
            prev_pos = self.pos
            prev_line = self.line
            tok = self._advance()
            if tok is None:
                break
            if tok.line != current_line:
                self.pos = prev_pos
                self.line = prev_line
                break
            if tok.type in AssemblerParser._VALID_OPERAND_TYPES:
                operands.append(tok)
            else:
                raise InvalidSyntaxError(
                    f"Unexpected token type {tok.type} with value {tok.value!r}",
                    line=tok.line,
                    column=tok.start_column,
                )
        return operands

    def parse_line(self) -> dict | None:
        """Parse the current line of assembly code."""
        tok = self._advance()
        if not tok:
            return None

        match tok.type:
            case "LABEL":
                return {
                    "type": "label",
                    "label": tok.value.removesuffix(":"),
                    "line": tok.line,
                    "column": tok.start_column,
                }
            case "MNEMONIC":
                args = self._collect_operands()
                return {
                    "type": "instruction",
                    "mnemonic": tok.value,
                    "arguments": args,
                    "line": tok.line,
                    "column": tok.start_column,
                }
            case _:
                raise InvalidSyntaxError(
                    f"Unexpected token type {tok.type} with value {tok.value!r}",
                    line=tok.line,
                    column=tok.start_column,
                )

    def parse(self) -> list[dict]:
        """Parses all tokens into a complete program structure.

        Returns:
            List of dictionaries representing the parsed program with labels and instructions.
        """
        program = []
        while self.pos < len(self.tokens):
            line = self.parse_line()
            if line:
                program.append(line)

        return program
