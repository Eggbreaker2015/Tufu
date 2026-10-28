"""Headless Unity harness: requests, run worktree and launcher (design 1.4, 2; unit C0b).

    from tufu_balance.unity import launcher, request
    result = launcher.run(request.preset("selftest"))      # or launcher.selftest()
    result.exit_code, result.manifest("selftest")

Command line: ``python -m tufu_balance.unity open|selftest|run|serve|status`` (see ``__main__``).
C1 adds ``simio``/``schema`` (ingest), C2 ``probes``, C8 ``bake_report``.
"""

from .launcher import (EXIT_NAMES, LauncherError, Options, RunResult, open_editor, run, selftest, serve_start,
                       serve_status, serve_stop, serve_submit)
from .request import RequestError, make_job, make_request, empty_job, preset

__all__ = ["EXIT_NAMES", "LauncherError", "Options", "RequestError", "RunResult", "empty_job", "make_job",
           "make_request", "open_editor", "preset", "run", "selftest", "serve_start", "serve_status", "serve_stop",
           "serve_submit"]
