"""Custom exception classes for the assembler.

This module defines all custom exception classes used throughout the assembler project.
"""


class InvalidSyntaxError(Exception):
    """Base exception for all assembler custom errors.

    Args:
        message: Error description.
        line: Line number where error occurred (1-indexed, optional).
        column: Column number where error occurred (1-indexed, optional).

    Attributes:
        message: Error description.
        line: Line number where error occurred.
        column: Column number where error occurred.
    """

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
        if self.line is not None:
            location = f"line {self.line}"
            if self.column is not None:
                location += f", column {self.column}"
            return f"{location}: {self.message}"
        return self.message


class UnexpectedCharError(InvalidSyntaxError):
    """Raised when an unexpected character is encountered in input."""


class InvalidInstructionError(InvalidSyntaxError):
    """Raised when an invalid instruction is encountered in input."""


class InvalidOperandError(InvalidSyntaxError):
    """Raised when an invalid operand is encountered in input."""


class InvalidRegisterError(InvalidSyntaxError):
    """Raised when an invalid register is encountered in input."""


class InvalidAddressError(InvalidSyntaxError):
    """Raised when an invalid address is encountered in input."""


class ValueOutOfRangeError(InvalidSyntaxError):
    """Raised when an invalid value is encountered in input."""


class DuplicateLabelError(InvalidSyntaxError):
    """Raised duplicate label is encountered in input."""


class UndefinedLabelError(InvalidSyntaxError):
    """Raised when an undefined label is encountered in input."""


class ProgramTooLongError(InvalidSyntaxError):
    """Raised when the program exceeds 1024 instructions in length."""


# Section: Instruction Loading Errors


class InstructionsLoadError(Exception):
    """Raised when error is encountered during loading instructions file."""


class InstructionFormatError(InstructionsLoadError):
    """Raised when the JSON is malformed or doesn't match the expected shape."""
