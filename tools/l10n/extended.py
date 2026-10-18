"""Run the existing M2b extended Windows and L1a acceptance checks in L10N outputs."""
from pathlib import Path
import sys
from verify import BASE, ROOT, LOGS, RUN, run

def main():
    source=(BASE/'logs/M2b_extended_checks.py').read_text(encoding='utf8')
    source=source.replace("settings=logs/'M2b_acceptance/M1/M1_spooles_settings.bin'", "settings=logs/'L10N/english_settings.bin'")
    source=source.replace("r/'run_m2b", "r/'run_l10n").replace("str(r/'run_m2b')", "str(r/'run_l10n')")
    source=source.replace("r/'work_m2b", "r/'work_l10n")
    for before,after in [("'M2b_M1","'L10N_M1"),("'M2b_old_","'L10N_old_"),("'M2b_analysis_","'L10N_analysis_"),("'M2b_extended_summary.json'","'L10N_extended_summary.json'")]:source=source.replace(before,after)
    source=source.replace("logs/'M2b_acceptance/M1/M1_user_material.pmx'", "logs/'L10N_regressions/M1/M1_user_material.pmx'")
    generated=BASE/'logs/L10N_extended.py';generated.write_text(source,encoding='utf8')
    if run('extended',[sys.executable,'-B',generated],timeout=2400):return 1
    if run('L1a_cli',[sys.executable,'-B',BASE/'work_l1a/tests/L1a/verify_cli.py',
        '--cli',BASE/'logs/L1a_cli/prepomax-cli.exe','--fixtures',BASE/'linux/tests/data',
        '--work',BASE/'logs/L10N_L1a_cli','--net48-pmx',BASE/'logs/L10N_regressions/M2b_layup.pmx',
        '--solver',RUN/'Solver/ccx_dynamic.exe'],timeout=600):return 1
    if run('L1a_runtime',[BASE/'logs/L1a_runtime_checks/RuntimeChecks.exe','--native'],cwd=BASE/'logs/L1a_runtime_checks'):return 1
    return run('L1a_processes',[sys.executable,'-B',BASE/'work_l1a/tests/L1a/verify_processes.py',
        '--cli',BASE/'logs/L1a_cli/prepomax-cli.exe','--fixture',BASE/'logs/L1a_runtime_checks/RuntimeChecks.exe',
        '--work',BASE/'logs/L10N_L1a_processes'],timeout=120)
if __name__=='__main__':raise SystemExit(main())
