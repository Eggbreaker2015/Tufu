"""Calibration harness (design 5; unit P10).

* :mod:`.targets`: targets T01-T20 rebuilt from ``balance.sqlite``; the same loader reads a run's
  ``sim.sqlite``.
* :mod:`.metrics` and ``tolerances.yaml``: per-cell residuals of a run and their verdicts per mode.
* :mod:`.params`: parameter layers, ``params_hash``, checks against the C# ``SimulationRebuildParams``.
* :mod:`.fit`: grid, scalar and batched-simplex optimisation, least squares, block bootstrap, AIC.
* :mod:`.driver` and :mod:`.runcache`: jobs to Unity in batches (resident or batch mode), per-job cache.
* :mod:`.report`: the byte-stable tables of ``out/calibration/<stage>/<run_id>/``.

The stages themselves (``stage_open``, ``stage_slow``, ``stage_full``) belong to K1-K3.
"""
