import json
import re
from pathlib import Path
from functools import lru_cache
from typing import Any
from errors import InstructionFormatError


class InstructionLoader:
    _INSTRUCTION_REQUIRED_FIELDS = {"mnemonic", "operands", "code_template"}
    _VALID_OPERAND_TYPES = {"num", "reg", "adr"}
    _VALID_TRANSFORMATIONS = {"div2", "neq", "dec"}
    _CODE_TEMPLATE_LENGTH = 16
    _CODE_TEMPLATE_ALLOWED_CHARS = set("01ANR_")
    FILE_PATH = "instructions.json"  # relative to project root
    REPO_ROOT_INDICATORS = {".git"}

    @staticmethod
    @lru_cache(maxsize=1)
    def load_instructions(path: str | Path = FILE_PATH) -> dict[str, Any]:
        """
        Load and validate instructions from a JSON file.

        Searches for the file relative to the git repository root, validates the format,
        and returns instructions as a dictionary keyed by mnemonic.

        Args:
            path: Path to the instructions JSON file. Relative paths are resolved from
                  the git repository root.

        Returns:
            Dictionary mapping instruction mnemonics to their full definition.

        Raises:
            FileNotFoundError: If the instructions file cannot be found
            InstructionFormatError: If the file content fails validation
            OSError: If there are file reading issues
        """
        content = InstructionLoader._load_file(path)
        InstructionLoader._validate_instructions_file(content)

        # Convert list to dictionary keyed by mnemonic
        return {item["mnemonic"]: item for item in content}

    @staticmethod
    @lru_cache(maxsize=1)
    def find_root(start_dir: str | Path | None = None) -> Path:
        """
        Find the root directory of the repository.

        Searches upward from the starting directory until a .git directory is found.
        Results are cached for performance.

        Args:
            start_dir: Directory to start searching from. If None, uses the directory containing this module.

        Returns:
            Path to the git repository root directory.

        Raises:
            FileNotFoundError: If no .git directory is found in the search path
            NameError: If __file__ is not available and no start_dir provided
        """
        if start_dir is not None:
            current_dir = Path(start_dir).resolve()
        else:
            try:
                current_dir = Path(__file__).resolve().parent
            except NameError as exc:
                raise NameError(
                    "__file__ is not defined in this execution context; provide start_dir explicitly"
                ) from exc

        for candidate in InstructionLoader._iter_dirs_upwards(current_dir):
            for target in InstructionLoader.REPO_ROOT_INDICATORS:
                if (candidate / target).exists():
                    return candidate

        raise FileNotFoundError(
            f"No directory containing '.git' found when searching upward from {current_dir!s}."
        )

    @staticmethod
    def clear_cache() -> None:
        """Clear cache, mainly for testing purposes."""
        InstructionLoader.find_root.cache_clear()
        InstructionLoader.load_instructions.cache_clear()

    @staticmethod
    def _iter_dirs_upwards(start: Path):
        """Yield start then each parent up to the filesystem root (inclusive)."""
        yield start
        for p in start.parents:
            yield p

    @staticmethod
    def _load_file(path: str | Path) -> list[dict[str, Any]]:
        """
        Load instruction specifications from a JSON file.

        Resolves the file path relative to the git repository root and parses JSON content.
        Results are cached for performance.

        Args:
            path: Path to the JSON file. Relative paths are resolved from git root.

        Returns:
            List of instruction dictionaries parsed from JSON.

        Raises:
            FileNotFoundError: If the file doesn't exist
            InstructionFormatError: If JSON is invalid or path is a directory
            OSError: For other file reading errors
        """
        root = InstructionLoader.find_root()
        path_file = Path(path)
        file_path = path_file if path_file.is_absolute() else (root / path_file)

        try:
            with file_path.open("r", encoding="utf-8") as fh:
                content = json.load(fh)
        except FileNotFoundError as exc:
            raise FileNotFoundError(
                f"Instructions file '{file_path}' not found under repository root '{root}'."
            ) from exc
        except json.JSONDecodeError as exc:
            raise InstructionFormatError(
                f"Invalid JSON in '{file_path}': {exc}"
            ) from exc
        except IsADirectoryError as exc:
            raise InstructionFormatError(
                f"Expected a file but found a directory at '{file_path}'"
            ) from exc
        except OSError as exc:
            raise OSError(f"Unable to read '{file_path}': {exc}") from exc

        return content

    # ==================================================
    # Section: Instructions File Validation
    # ==================================================

    @staticmethod
    def _validate_instructions_file(content: list[dict[str, Any]]) -> None:
        """
        Validate the overall structure and content of instructions file.

        Args:
            content: List of instruction dictionaries to validate

        Raises:
            InstructionFormatError: If content fails validation
        """
        if not isinstance(content, list):
            raise InstructionFormatError("Instructions content must be a list")

        seen_mnemonics = set()

        for i, instruction in enumerate(content):
            InstructionLoader._validate_instruction_structure(instruction, i)
            InstructionLoader._validate_instruction_content(
                instruction, i, seen_mnemonics
            )

    @staticmethod
    def _validate_instruction_structure(
        instruction: dict[str, Any], index: int
    ) -> None:
        """
        Validate basic structure of an instruction dictionary.

        Args:
            instruction: Instruction dictionary to validate
            index: Index of the instruction in the list (for error reporting)

        Raises:
            InstructionFormatError: If structure is invalid
        """
        if not isinstance(instruction, dict):
            raise InstructionFormatError(
                f"Instruction at index {index} must be a dictionary"
            )

        # Check required fields
        for field in InstructionLoader._INSTRUCTION_REQUIRED_FIELDS:
            if field not in instruction:
                raise InstructionFormatError(
                    f"Instruction at index {index} is missing required field: '{field}'"
                )

    @staticmethod
    def _validate_instruction_content(
        instruction: dict[str, Any], index: int, seen_mnemonics: set[str]
    ) -> None:
        """
        Validate the content of an instruction including mnemonic, operands, and template.

        Args:
            instruction: Instruction dictionary to validate
            index: Index of the instruction in the list (for error reporting)
            seen_mnemonics: Set of mnemonics already encountered (for duplicate detection)

        Raises:
            InstructionFormatError: If content is invalid
        """
        mnemonic = instruction["mnemonic"]

        # Validate mnemonic
        if not isinstance(mnemonic, str) or not mnemonic:
            raise InstructionFormatError(
                f"Instruction at index {index} has invalid mnemonic: must be non-empty string"
            )

        # Check for duplicate mnemonics
        if mnemonic in seen_mnemonics:
            raise InstructionFormatError(f"Duplicate mnemonic found: '{mnemonic}'")
        seen_mnemonics.add(mnemonic)

        # Validate operands
        InstructionLoader._validate_operands(instruction["operands"], mnemonic)

        # Validate code template
        InstructionLoader._validate_code_template(
            instruction["code_template"], instruction["operands"], mnemonic
        )

    @staticmethod
    def _validate_operands(operands: list[dict[str, Any]], mnemonic: str) -> None:
        """
        Validate the structure and content of instruction operands.

        Args:
            operands: List of operand dictionaries to validate
            mnemonic: Instruction mnemonic (for error context)

        Raises:
            InstructionFormatError: If any operand is invalid
        """
        if not isinstance(operands, list):
            raise InstructionFormatError(
                f"Instruction '{mnemonic}' operands must be a list"
            )

        for j, operand in enumerate(operands):
            if not isinstance(operand, dict):
                raise InstructionFormatError(
                    f"Instruction '{mnemonic}' operand {j} must be a dictionary"
                )

            if "type" not in operand:
                raise InstructionFormatError(
                    f"Instruction '{mnemonic}' operand {j} is missing 'type' field"
                )
            if operand["type"] not in InstructionLoader._VALID_OPERAND_TYPES:
                raise InstructionFormatError(
                    f"Instruction '{mnemonic}' operand {j} type '{operand['type']}' is not a valid operand type"
                )

            # Validate numeric operands
            if operand["type"] == "num":
                if "range" not in operand:
                    raise InstructionFormatError(
                        f"Number operand {j} is missing 'range' field"
                    )
                range_val = operand["range"]
                if (
                    not isinstance(range_val, list)
                    or len(range_val) != 2
                    or not all(isinstance(x, int) for x in range_val)
                    or range_val[0] > range_val[1]
                ):
                    raise InstructionFormatError(
                        f"Instruction '{mnemonic}' operand {j} has invalid range: must be [min, max] where min <= max and both are integers"
                    )

                if "transformations" in operand:
                    transformations = operand["transformations"]
                    if not isinstance(transformations, list) or not all(
                        isinstance(t, str) for t in transformations
                    ):
                        raise InstructionFormatError(
                            f"Instruction '{mnemonic}' operand {j} transformations must be a list of strings"
                        )
                    for transformation in transformations:
                        if (
                            transformation
                            not in InstructionLoader._VALID_TRANSFORMATIONS
                        ):
                            raise InstructionFormatError(
                                f"Instruction '{mnemonic}' operand {j} has invalid transformation: '{transformation}'. "
                                f"Valid transformations are: {InstructionLoader._VALID_TRANSFORMATIONS}"
                            )

    @staticmethod
    def _validate_code_template(
        code_template: str, operands: list[dict[str, Any]], mnemonic: str
    ) -> None:
        """
        Validate code template against operands.

        Args:
            code_template: The 16-character template string
            operands: List of operand dictionaries
            mnemonic: Instruction mnemonic (for error context)

        Raises:
            InstructionFormatError: If template placeholders don't match operands
        """
        if (
            not isinstance(code_template, str)
            or len(code_template) != InstructionLoader._CODE_TEMPLATE_LENGTH
        ):
            raise InstructionFormatError(
                f"Instruction '{mnemonic}' code_template must be a {InstructionLoader._CODE_TEMPLATE_LENGTH}-character string"
            )

        # Create a working copy of the template for validation
        remaining_template = code_template

        for i, operand in enumerate(operands):
            operand_type = operand["type"]
            expected_placeholder = {"reg": "R", "num": "N", "adr": "A"}[operand_type]

            # Find the first occurrence of the expected placeholder
            pattern = rf"{expected_placeholder}_*"
            match = re.search(pattern, remaining_template)
            pos = match.start()
            length = len(match.group(0))

            if pos == -1:
                raise InstructionFormatError(
                    f"Instruction '{mnemonic}' operand {i} (type: {operand_type}) requires "
                    f"placeholder '{expected_placeholder}' but none found in remaining template: {remaining_template}"
                )

            # Remove the matched portion from the template (everything up to and including the placeholder)
            remaining_template = remaining_template[pos + length :]

        # After processing all operands, check if there are any unexpected characters left
        if not set(remaining_template) <= {"0", "1"}:
            raise InstructionFormatError(
                f"Instruction '{mnemonic}' has unmatched placeholders in template. "
                f"Template: {code_template}, Operands: {[op['type'] for op in operands]}"
            )
