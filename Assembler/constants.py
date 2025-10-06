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
