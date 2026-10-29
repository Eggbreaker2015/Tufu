"""C1 defect reached by C6: validate export text with an independent IEEE parser.

Mono accepts 7.0236302039131 as the adjacent double 7.0236302039131004;
the former text corrupted the full-snapshot digest in the inventory replay.
"""
import math
import random
import struct

import pytest

from tufu_balance.unity import rebuildcheck


def bits(value):
    return struct.pack('>d', value).hex()


@pytest.mark.parametrize('runtime', rebuildcheck.RUNTIMES)
def test_export_double_roundtrips_in_an_independent_parser(runtime):
    if not rebuildcheck.available(runtime):
        pytest.skip(f'needs {runtime}')
    rng = random.Random(6)
    values = [7.0236302039131004, 7.0236302039131, 433.0000008940697,
              433.00000089406973, 0., -0., 5e-324, 1.7976931348623157e308,
              math.inf, -math.inf, 1e-200, 1e200]
    for _ in range(4000):
        value = struct.unpack('>d', rng.getrandbits(64).to_bytes(8, 'big'))[0]
        if not math.isnan(value):
            values.append(value)
    [out] = rebuildcheck.run([{'op': 'export-format', 'doubles': [bits(v) for v in values]}], runtime)
    assert [bits(float(s)) for s in out['texts']] == [bits(v) for v in values]
