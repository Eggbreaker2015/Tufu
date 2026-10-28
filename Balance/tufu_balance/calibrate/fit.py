"""Fitting tools of the calibration loop (design 5.3, 5.5; unit P10).

* :func:`grid`: closed lists of discrete hypotheses, scored in batches (one Unity batch per call of
  ``score_batch``); the ranking keeps the runner-up and its score (design 5.5).
* :func:`minimize_scalar`: one continuous parameter (``scipy.optimize.minimize_scalar``, bounded).
* :class:`BatchedNelderMead` / :func:`batched_simplex`: two continuous parameters; every iteration asks
  for its candidate points (reflection, expansion, both contractions, or the shrink) as one batch.
* :func:`ratio_fit`, :func:`linear_least_squares`, :func:`nonlinear_least_squares`: parameters straight
  from a run's trace (kappa = sum(A F) / sum(F^2); NnTour's a, b).
* :func:`block_bootstrap`: intervals by resampling blocks (TC intervals) of existing residuals, no
  new runs.
* :func:`aic` / :func:`compare_aic`: model choice on the holdout segment (FarmedTime vs NnTour).

Everything is deterministic: explicit orders, ties broken by enumeration order, seeded generators.
"""

from __future__ import annotations

import itertools
import math
from dataclasses import dataclass, field
from typing import Any, Callable, Iterable, Mapping, Sequence

import numpy as np

Candidate = dict[str, Any]


# ---------------------------------------------------------------- discrete grid

@dataclass
class Ranked:
    candidate: Candidate
    score: float
    index: int  # position in the enumeration (tie breaker)


@dataclass
class GridResult:
    ranking: list[Ranked]
    batches: int

    @property
    def best(self) -> Ranked:
        return self.ranking[0]

    @property
    def second(self) -> Ranked | None:
        return self.ranking[1] if len(self.ranking) > 1 else None


def grid_candidates(space: Mapping[str, Sequence[Any]],
                    keep: Callable[[Candidate], bool] | None = None) -> list[Candidate]:
    """The product of the value lists in key order (first key varies slowest); ``keep`` filters."""
    names = list(space)
    out = [dict(zip(names, values)) for values in itertools.product(*(space[n] for n in names))]
    return [c for c in out if keep is None or keep(c)]


def grid(space: Mapping[str, Sequence[Any]] | Sequence[Candidate], *,
         score: Callable[[Candidate], float] | None = None,
         score_batch: Callable[[list[Candidate]], Sequence[float]] | None = None,
         batch_size: int = 12, keep: Callable[[Candidate], bool] | None = None) -> GridResult:
    """Score every candidate (lower is better) and rank them. NaN scores rank last."""
    cands = grid_candidates(space, keep) if isinstance(space, Mapping) else [dict(c) for c in space]
    if (score is None) == (score_batch is None):
        raise ValueError("pass exactly one of score / score_batch")
    if score_batch is None:
        score_batch = lambda batch: [score(c) for c in batch]  # noqa: E731
    scores: list[float] = []
    batches = 0
    for i in range(0, len(cands), max(1, batch_size)):
        chunk = cands[i:i + batch_size]
        got = list(score_batch(chunk))
        if len(got) != len(chunk):
            raise ValueError("score_batch returned the wrong number of scores")
        scores.extend(float(x) for x in got)
        batches += 1
    ranked = [Ranked(c, s, i) for i, (c, s) in enumerate(zip(cands, scores))]
    ranked.sort(key=lambda r: (math.isnan(r.score), r.score if not math.isnan(r.score) else 0.0, r.index))
    return GridResult(ranked, batches)


# ---------------------------------------------------------------- one continuous parameter

@dataclass
class ScalarResult:
    x: float
    fun: float
    evaluations: int
    success: bool


def minimize_scalar(f: Callable[[float], float], bounds: tuple[float, float], *, xatol: float = 1e-6,
                    maxiter: int = 200) -> ScalarResult:
    from scipy.optimize import minimize_scalar as _ms

    calls = [0]

    def wrapped(x):
        calls[0] += 1
        return float(f(float(x)))

    r = _ms(wrapped, bounds=bounds, method="bounded", options={"xatol": xatol, "maxiter": maxiter})
    return ScalarResult(float(r.x), float(r.fun), calls[0], bool(r.success))


# ---------------------------------------------------------------- batched simplex

@dataclass
class SimplexResult:
    x: np.ndarray
    fun: float
    iterations: int
    batches: int
    evaluations: int
    converged: bool
    history: list[tuple[str, float]] = field(default_factory=list)  # (step, best value) per iteration


class BatchedNelderMead:
    """Nelder-Mead whose function evaluations come in batches.

    Each iteration proposes reflection, expansion, outside and inside contraction together (4 points, one
    batch) and then applies the textbook rules (coefficients 1, 2, 1/2, 1/2); a shrink costs one more batch
    of n points. Optional box ``bounds`` clip every proposed point.
    """

    def __init__(self, x0: Sequence[float], step: Sequence[float] | float = 0.1, *,
                 bounds: Sequence[tuple[float, float]] | None = None):
        x0 = np.asarray(x0, dtype=float)
        n = x0.size
        steps = np.full(n, float(step)) if np.isscalar(step) else np.asarray(step, dtype=float)
        self.bounds = None if bounds is None else np.asarray(bounds, dtype=float)
        self.simplex = [self._clip(x0)] + [self._clip(x0 + np.eye(n)[i] * steps[i]) for i in range(n)]
        self.values: list[float] | None = None

    def _clip(self, x: np.ndarray) -> np.ndarray:
        if self.bounds is None:
            return np.asarray(x, dtype=float)
        return np.clip(x, self.bounds[:, 0], self.bounds[:, 1])

    def _order(self) -> None:
        order = sorted(range(len(self.simplex)), key=lambda i: (self.values[i], i))
        self.simplex = [self.simplex[i] for i in order]
        self.values = [self.values[i] for i in order]

    def run(self, f_batch: Callable[[list[np.ndarray]], Sequence[float]], *, max_iter: int = 200,
            xatol: float = 1e-4, fatol: float = 1e-8) -> SimplexResult:
        batches = evaluations = 0

        def ask(points: list[np.ndarray]) -> list[float]:
            nonlocal batches, evaluations
            batches += 1
            evaluations += len(points)
            got = [float(v) for v in f_batch([np.array(p) for p in points])]
            if len(got) != len(points):
                raise ValueError("f_batch returned the wrong number of values")
            return got

        self.values = ask(self.simplex)
        self._order()
        history = []
        converged = False
        it = 0
        for it in range(1, max_iter + 1):
            best, worst = self.values[0], self.values[-1]
            spread = max(float(np.max(np.abs(p - self.simplex[0]))) for p in self.simplex[1:])
            if spread <= xatol and abs(worst - best) <= fatol:
                converged = True
                it -= 1
                break
            centroid = np.mean(self.simplex[:-1], axis=0)
            xw = self.simplex[-1]
            xr = self._clip(centroid + (centroid - xw))
            xe = self._clip(centroid + 2.0 * (centroid - xw))
            xoc = self._clip(centroid + 0.5 * (centroid - xw))
            xic = self._clip(centroid - 0.5 * (centroid - xw))
            fr, fe, foc, fic = ask([xr, xe, xoc, xic])
            second_worst = self.values[-2]
            step = None
            if fr < best:
                step, point, value = ("expand", xe, fe) if fe < fr else ("reflect", xr, fr)
            elif fr < second_worst:
                step, point, value = "reflect", xr, fr
            elif fr < worst and foc <= fr:
                step, point, value = "outside", xoc, foc
            elif fr >= worst and fic < worst:
                step, point, value = "inside", xic, fic
            if step is not None:
                self.simplex[-1], self.values[-1] = point, value
            else:
                step = "shrink"
                x0 = self.simplex[0]
                shrunk = [self._clip(x0 + 0.5 * (p - x0)) for p in self.simplex[1:]]
                self.simplex[1:] = shrunk
                self.values[1:] = ask(shrunk)
            self._order()
            history.append((step, self.values[0]))
        return SimplexResult(np.array(self.simplex[0]), self.values[0], it, batches, evaluations, converged, history)


def batched_simplex(f_batch: Callable[[list[np.ndarray]], Sequence[float]], x0: Sequence[float],
                    step: Sequence[float] | float = 0.1, **kw) -> SimplexResult:
    bounds = kw.pop("bounds", None)
    return BatchedNelderMead(x0, step, bounds=bounds).run(f_batch, **kw)


# ---------------------------------------------------------------- least squares

def ratio_fit(a: Sequence[float], f: Sequence[float]) -> float:
    """``argmin_k sum (A - k F)^2 = sum(A F) / sum(F^2)`` (design 5.3: kappa from daily.csv)."""
    a, f = np.asarray(a, dtype=float), np.asarray(f, dtype=float)
    den = float(np.dot(f, f))
    if den == 0:
        raise ValueError("all F are zero")
    return float(np.dot(a, f)) / den


@dataclass
class LsqResult:
    x: np.ndarray
    residuals: np.ndarray
    rss: float
    n: int
    k: int


def linear_least_squares(x: Sequence[Sequence[float]], y: Sequence[float]) -> LsqResult:
    xm, yv = np.asarray(x, dtype=float), np.asarray(y, dtype=float)
    if xm.ndim == 1:
        xm = xm[:, None]
    coef, *_ = np.linalg.lstsq(xm, yv, rcond=None)
    res = yv - xm @ coef
    return LsqResult(coef, res, float(res @ res), len(yv), xm.shape[1])


def nonlinear_least_squares(residual: Callable[[np.ndarray], Sequence[float]], x0: Sequence[float], *,
                            bounds: tuple[Sequence[float], Sequence[float]] | None = None) -> LsqResult:
    from scipy.optimize import least_squares

    r = least_squares(lambda p: np.asarray(residual(p), dtype=float), np.asarray(x0, dtype=float),
                      bounds=bounds if bounds is not None else (-np.inf, np.inf), method="trf")
    res = np.asarray(r.fun, dtype=float)
    return LsqResult(np.asarray(r.x), res, float(res @ res), len(res), len(r.x))


# ---------------------------------------------------------------- block bootstrap

@dataclass
class BootstrapResult:
    estimate: float
    lo: float
    hi: float
    samples: np.ndarray
    blocks: int
    level: float

    @property
    def width(self) -> float:
        return self.hi - self.lo


def block_bootstrap(blocks: Mapping[Any, Sequence[Any]] | Sequence[Sequence[Any]],
                    statistic: Callable[[list[Any]], float], *, n_boot: int = 1000, level: float = 0.9,
                    seed: int = 0) -> BootstrapResult:
    """Percentile interval of ``statistic`` over resampled blocks.

    ``blocks`` are the observations grouped by TC interval (a mapping is taken in sorted key order); each
    replicate draws as many blocks as there are, with replacement, and concatenates their observations.
    """
    groups = [list(blocks[k]) for k in sorted(blocks)] if isinstance(blocks, Mapping) else [list(b) for b in blocks]
    groups = [g for g in groups if g]
    if not groups:
        raise ValueError("no non-empty blocks")
    rng = np.random.default_rng(seed)
    flat = [x for g in groups for x in g]
    estimate = float(statistic(flat))
    picks = rng.integers(0, len(groups), size=(n_boot, len(groups)))
    samples = np.array([float(statistic([x for j in row for x in groups[j]])) for row in picks])
    alpha = (1.0 - level) / 2.0
    lo, hi = np.quantile(samples, [alpha, 1.0 - alpha])
    return BootstrapResult(estimate, float(lo), float(hi), samples, len(groups), level)


# ---------------------------------------------------------------- information criteria

def aic(rss: float, n: int, k: int) -> float:
    """Gaussian AIC from a residual sum of squares: ``n ln(RSS/n) + 2k``."""
    if n <= 0:
        raise ValueError("n must be positive")
    if rss <= 0:
        return -math.inf
    return n * math.log(rss / n) + 2 * k


def aic_from_residuals(residuals: Iterable[float], k: int) -> float:
    r = np.asarray(list(residuals), dtype=float)
    return aic(float(r @ r), len(r), k)


@dataclass
class AicRow:
    name: str
    aic: float
    delta: float
    weight: float
    n: int
    k: int
    rss: float


def compare_aic(models: Mapping[str, tuple[float, int, int]]) -> list[AicRow]:
    """``{name: (rss, n, k)}`` -> rows sorted by AIC (ties by name) with delta and Akaike weights."""
    rows = [(name, aic(rss, n, k), n, k, rss) for name, (rss, n, k) in models.items()]
    ns = {n for _, _, n, _, _ in rows}
    if len(ns) > 1:
        raise ValueError(f"AIC needs the same observations for every model, got n = {sorted(ns)}")
    rows.sort(key=lambda r: (r[1], r[0]))
    best = rows[0][1]
    rel = [math.exp(-0.5 * (a - best)) for _, a, *_ in rows]
    total = sum(rel)
    return [AicRow(name, a, a - best, w / total, n, k, rss) for (name, a, n, k, rss), w in zip(rows, rel)]
