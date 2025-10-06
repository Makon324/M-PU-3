class AssemblerConstants:
    MAX_PROGRAM_SIZE = 1024
    NUM_REGISTERS = 8
    ADDRESS_RANGE = range(0, MAX_PROGRAM_SIZE)
    REGISTER_RANGE = range(0, NUM_REGISTERS)

    TRANSFORMATIONS = {
        "neq": lambda x: not x,
        "div2": lambda x: x // 2,
        "dec": lambda x: x - 1,
    }

    TOKEN_SPECIFICATION = (
        ("COMMENT", r";[^\n]*"),
        ("LABEL", r"\.[A-Za-z_][A-Za-z0-9_]*:"),
        ("IDENT", r"\.[A-Za-z_][A-Za-z0-9_]*"),
        ("REGISTER", r"R[0-9]+"),
        ("HEX", r"-?0x[0-9A-Fa-f]+"),
        ("BIN", r"-?0b[01]+"),
        ("DEC", r"-?[0-9]+"),
        ("MNEMONIC", r"[A-Za-z]+"),
        ("NEWLINE", r"\n"),
        ("SKIP", r"[\t ,]+"),
        ("MISMATCH", r"."),
    )


