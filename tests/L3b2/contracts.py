"""Use the GUI3 never-visible desktop for a real Windows PropertyGrid snapshot."""
from pathlib import Path
import importlib.util, json, os, sys
repo = Path(__file__).resolve().parents[2]
logs = Path(os.environ.get('PPM_FORMS_LOGS', repo.parent/'logs/L3b2/forms'))
spec = importlib.util.spec_from_file_location('hidden', repo/'tests/M2bUi/run.py')
hidden = importlib.util.module_from_spec(spec); spec.loader.exec_module(hidden)
os.environ['DOTNET_PROCESSOR_COUNT'] = '4'
apply_mode = len(sys.argv) > 1 and sys.argv[1] == '--apply'
name = ('apply-windows' if apply_mode else 'contracts-windows') + ('-final' if '--final' in sys.argv else '')
extra = ['--apply', sys.argv[2]] if apply_mode else []
with (logs/(name+'.log')).open('wb') as log:
    code = hidden.private_desktop_run([str(repo/'tests/L3b2/bin/Release/net48/FormContracts.exe'), str(logs/'windows-runtime'), str(logs/(name+'.json'))]+extra, repo, log, 180)
(logs/(name+'-run.json')).write_text(json.dumps(dict(exit=code, private_desktop=True, owner_desktop_never_switched=True),indent=2))
print((logs/(name+'.log')).read_text(encoding='utf-8',errors='replace'))
raise SystemExit(code)
