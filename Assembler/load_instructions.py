import json
from pathlib import Path


def load_instructions(filename: str = "instructions.json") -> dict:
    """Searches for and loads instruction specifications from a JSON file.

    Args:
        filename: Name of the JSON file to load (default: 'instructions.json').

    Returns:
        Dictionary of instruction specifications keyed by mnemonic.

    Raises:
        FileNotFoundError: If the instructions file cannot be found.
        ValueError: If the file contains invalid JSON.
        OSError: If the file cannot be read.
    """
    current_dir = Path(__file__).resolve().parent

    while True:
        candidate = current_dir / filename
        if candidate.exists():
            try:
                with candidate.open("r", encoding="utf-8") as fh:
                    data = json.load(fh)
                # Convert list to dictionary keyed by mnemonic
                return {item['mnemonic']: item for item in data}
            except json.JSONDecodeError as exc:
                raise ValueError(f"Invalid JSON format in '{candidate}': {exc}") from exc
            except OSError as exc:
                raise OSError(f"Unable to read '{candidate}': {exc}") from exc

        # Go one directory up
        parent = current_dir.parent
        if parent == current_dir:  # reached filesystem root
            break
        current_dir = parent

    raise FileNotFoundError(
        f"Instructions file '{filename}' not found when searching upward "
        f"from '{Path(__file__).resolve().parent}'."
    )