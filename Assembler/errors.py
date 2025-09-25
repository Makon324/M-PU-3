"""Custom exception classes for the assembler.

This module defines all custom exception classes used throughout the assembler project.
"""


class AssemblerError(Exception):
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
        return self.message

class UnexpectedCharError(AssemblerError):
    """Raised when an unexpected character is encountered in input."""

class InvalidSyntaxError(AssemblerError):
    """Raised when invalid syntax is encountered in input."""

class InvalidInstructionError(AssemblerError):
    """Raised when an invalid instruction is encountered in input."""

class InvalidOperandError(AssemblerError):
    """Raised when an invalid operand is encountered in input."""

class InvalidRegisterError(AssemblerError):
    """Raised when an invalid register is encountered in input."""

class InvalidAddressError(AssemblerError):
    """Raised when an invalid address is encountered in input."""

class ValueOutOfRangeError(AssemblerError):
    """Raised when an invalid value is encountered in input."""

class DuplicateLabelError(AssemblerError):
    """Raised duplicate label is encountered in input."""

class UndefinedLabelError(AssemblerError):
    """Raised when an undefined label is encountered in input."""

class ProgramTooLongError(AssemblerError):
    """Raised when the program exceeds 1024 instructions in length."""


class InstructionsError(Exception):
    """Raised when error is encountered in instructions file."""
    def __init__(self, message):
        super().__init__(message)


