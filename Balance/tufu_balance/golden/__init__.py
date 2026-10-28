"""Golden regression baselines: ORIGINAL (frozen) and CURRENT (changed only by ``accept``).

Tests record measured values and compare them with CURRENT::

    from tufu_balance.golden import at_most, check, exact, same_set

    check("timeline", {
        "G01.itd_tail_exact": exact(84, source="G01"),
        "G03.exceptions": same_set(exceptions, source="G03", better="subset"),
        "R2.dps_max_error": at_most(0.2283, source="R2"),
    })

Command line: ``python -m tufu_balance.golden status|accept|rebaseline-original|guard``. See
``Balance/tests/golden/README.md``.
"""

from .store import (DEFAULT_STORE, GOLDEN_DIR, LATEST_DIR, AcceptRefused, AcceptResult, Expectation,
                    GoldenError, GoldenMismatch, GoldenStore, GoldenTightenWarning, GuardViolation, at_least,
                    at_most, check, check_file, exact, is_looser, same_set)

__all__ = [
    "DEFAULT_STORE", "GOLDEN_DIR", "LATEST_DIR", "AcceptRefused", "AcceptResult", "Expectation", "GoldenError",
    "GoldenMismatch", "GoldenStore", "GoldenTightenWarning", "GuardViolation", "at_least", "at_most", "check",
    "check_file", "exact", "is_looser", "same_set",
]
