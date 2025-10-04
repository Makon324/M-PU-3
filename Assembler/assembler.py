import sys
import logging
from pathlib import Path
from tokenizer import AssemblerTokenizer
from parser import AssemblerParser
from validator import AssemblerValidator
from code_generator import AssemblerCodeGenerator
from errors import InvalidSyntaxError


# Configure logging
logging.basicConfig(
    level=logging.INFO, format="%(asctime)s - %(levelname)s - %(message)s"
)
logger = logging.getLogger(__name__)


class Assembler:
    def __init__(self):
        self.tokenizer = AssemblerTokenizer()
        self.validator = AssemblerValidator()

    def assemble(self, code: str, filename: str = "unknown") -> list[str]:
        """Assemble assembly code into machine code."""
        try:
            logger.info("Assembling code from %s (%d characters)", filename, len(code))

            # Tokenization phase
            logger.debug("Tokenizing source code")
            tokens = self.tokenizer.tokenize(code)
            logger.info("Generated %d tokens", len(tokens))

            # Parsing phase
            logger.debug("Parsing tokens")
            parser = AssemblerParser(tokens)
            parsed_program = parser.parse()
            logger.info("Parsed %d program lines", len(parsed_program))

            # Validation phase
            logger.debug("Validating parsed program")
            self.validator.validate(parsed_program)
            logger.debug("Validation completed successfully")

            # Code generation phase
            logger.debug("Generating machine code")
            code_generator = AssemblerCodeGenerator(parsed_program)
            binary = code_generator.generate_code()
            logger.info("Generated %d binary instructions", len(binary))

            return binary

        except InvalidSyntaxError as e:
            logger.error("Assembly failed: %s", e.message)
            raise
        except Exception as e:
            logger.error("Unexpected error during assembly: %s", str(e))
            raise


def load_code(path: str) -> str:
    """Load assembly code from a file."""
    try:
        file_path = Path(path)
        if not file_path.exists():
            raise FileNotFoundError(f"File '{path}' not found")

        if file_path.stat().st_size == 0:
            logger.warning("File '%s' is empty", path)

        with file_path.open("r", encoding="utf-8") as f:
            content = f.read()

        logger.debug("Loaded %d bytes from %s", len(content), path)
        return content

    except FileNotFoundError as e:
        logger.error("File not found: %s", path)
        print(f"Error: {e}", file=sys.stderr)
        sys.exit(1)
    except IOError as e:
        logger.error("I/O error reading %s: %s", path, e)
        print(f"Error reading file '{path}': {e}", file=sys.stderr)
        sys.exit(1)
    except UnicodeDecodeError as e:
        logger.error("Encoding error in %s: %s", path, e)
        print(f"Error: File '{path}' contains invalid UTF-8 encoding", file=sys.stderr)
        sys.exit(1)


def store_program(binary_code: list[str], output_path: str) -> None:
    """Store the compiled program as 16-bit binary numbers in a text file."""
    try:
        output_file = Path(output_path)
        output_file.parent.mkdir(
            parents=True, exist_ok=True
        )  # Create directories if needed

        with output_file.open("w", encoding="utf-8") as f:
            for i, instruction in enumerate(binary_code):
                # Format as two 8-bit groups separated by space
                formatted_instruction = f"{instruction[:8]} {instruction[8:]}"
                f.write(formatted_instruction + "\n")

        logger.info(
            "Successfully wrote %d instructions to %s", len(binary_code), output_path
        )

    except IOError as e:
        logger.error("Failed to write output file %s: %s", output_path, e)
        print(f"Error writing output file '{output_path}': {e}", file=sys.stderr)
        sys.exit(1)


def setup_argument_parser():
    """Set up command-line argument parsing."""
    import argparse

    parser = argparse.ArgumentParser(
        description="Assemble custom assembly language to machine code",
        epilog="Example: python assembler.py program.as -o program.txt",
    )

    parser.add_argument("input_file", help="Input assembly file (.as extension)")

    parser.add_argument(
        "-o",
        "--output",
        dest="output_file",
        help="Output file path (default: input file with .txt extension)",
    )

    parser.add_argument(
        "-v",
        "--verbose",
        action="store_true",
        help="Enable verbose logging for debugging",
    )

    return parser


def main() -> int:
    parser = setup_argument_parser()
    args = parser.parse_args()
    if args.verbose:
        logging.basicConfig(level=logging.DEBUG)
    input_path = args.input_file

    # Validate input file extension
    if Path(input_path).suffix.lower() != ".as":
        logger.error(f"Invalid input file extension: {input_path}")
        print("Error: Input file must have .as extension")
        return 1

    # Generate output path if not provided
    if args.output_file:
        output_path = args.output_file
        if not output_path.lower().endswith(".txt"):
            logger.warning(f"Output file {output_path} does not have .txt extension")
    else:
        output_path = Path(input_path).with_suffix(".txt")

    try:
        # Load, assemble, and store
        logger.info("Starting assembly process")
        code = load_code(input_path)
        assembler = Assembler()
        binary_program = assembler.assemble(code, filename=input_path)
        store_program(binary_program, output_path)

        logger.info("Assembly completed successfully")
        print(
            f"Successfully assembled {len(binary_program)} instructions to {output_path}"
        )
        return 0

    except InvalidSyntaxError as e:
        return 1
    except KeyboardInterrupt:
        logger.info("Assembly interrupted by user")
        print("\nAssembly interrupted by user", file=sys.stderr)
        return 130  # Standard exit code for Ctrl+C
    except Exception as e:
        logger.exception(f"Unexpected error during assembly: {e}")
        print(f"Internal error: {e}")
        return 2


if __name__ == "__main__":
    sys.exit(main())
