class AssemblerError(Exception):
    """Base class for all errors."""
    def __init__(self, message, line=None, column=None):
        self.message = message
        self.line = line
        self.column = column
        if line:
            if column:
                super().__init__(f"Error on line: {line}, column: {column} : {message}")
            else:
                super().__init__(f"Error on line: {line} : {message}")
        else:
            super().__init__(f"Error : {message}")

class UnexpectedChar(AssemblerError):
    """Raised when an unexpected character is encountered in input."""

class InvalidSyntax(AssemblerError):
    """Raised when an unexpected character is encountered in input."""
