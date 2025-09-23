from tokenizer import Token


def get_register_number(token: Token) -> int:
    return int(token.value[1:])

def get_number(token: Token) -> int:
    return int(token.value, 0)

def is_register(token: Token) -> bool:
    return token.type == 'REGISTER'

def is_number(token: Token) -> bool:
    return token.type in ['HEX', 'BIN', 'DEC']

def is_address(token: Token) -> bool:
    return token.type in ['IDENT', 'HEX', 'BIN', 'DEC']

def is_in_range(number: int, range_list) -> bool:
    return range_list[0] <= number <= range_list[1]



