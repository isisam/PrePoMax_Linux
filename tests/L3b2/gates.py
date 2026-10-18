"""Reuse the existing hidden Windows gates with task-isolated output paths."""
import importlib.util
from pathlib import Path

repo = Path(__file__).resolve().parents[2]
spec = importlib.util.spec_from_file_location('baseline', repo/'tests/L3b/baseline.py')
baseline = importlib.util.module_from_spec(spec)
spec.loader.exec_module(baseline)
baseline.logs = repo.parent/'logs/L3b2/forms'
baseline.runtime = baseline.logs/'windows-runtime'
if __name__ == '__main__':
    baseline.main()
