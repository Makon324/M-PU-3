class AssemblerError(Exception):
    """Base class for all assembler errors."""

    def __init__(self, message, line=None, column=None):
        self.message = message
        self.line = line
        self.column = column

        if line is not None:
            location_info = f"line {line}"
            if column is not None:
                location_info += f", column {column}"
            super().__init__(f"{location_info}: {message}")
        else:
            super().__init__(message)

    def __str__(self):
        return self.message

class UnexpectedCharError(AssemblerError):
    """Raised when an unexpected character is encountered in input."""

class InvalidSyntaxError(AssemblerError):
    """Raised when an unexpected character is encountered in input."""
