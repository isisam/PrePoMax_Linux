"""Generate explicit install/uninstall manifests. Author: S.Y. Su."""
from pathlib import Path
import shutil
ROOT=Path(__file__).resolve().parents[1]
BUILD=Path.home()/'AppData/Local/StudentPkgBuild'

def copy_changed(src,dst):
    src,dst=Path(src),Path(dst)
    if dst.exists() and src.stat().st_size==dst.stat().st_size and src.stat().st_mtime_ns==dst.stat().st_mtime_ns:return str(dst)
    return shutil.copy2(src,dst)

def generate(product,version,filename,exe):
    payload=ROOT/'payload'/product
    if product=='PrePoMax-RCM':
        shutil.copytree(BUILD/'runtime',payload,dirs_exist_ok=True,copy_function=copy_changed,ignore=shutil.ignore_patterns('trace.info','settings.bin','Work','*.pmh','__pycache__','*.pyc'))
        for name in ('CaeModel.dll','APIs.dll','PrePoMax.Core.dll'):
            shutil.copy2(BUILD/'managed'/name,payload/'lib'/name)
        shutil.copy2(BUILD/'managed/PrePoMax.exe',payload/'PrePoMax.exe')
    if product=='CalculiX_Win':shutil.copy2(ROOT/'packaging/UserPath.ps1',payload/'UserPath.ps1')
    for name in ['FEATURES_zh-TW.md','FEATURES_en.md','INSTALL_一鍵安裝.md']:
        shutil.copy2(ROOT/'docs'/product/name,payload/name)
    shutil.copytree(ROOT/'docs/screenshots',payload/'screenshots',dirs_exist_ok=True)
    files=sorted(p.relative_to(payload).as_posix() for p in payload.rglob('*') if p.is_file())
    dirs=sorted({p.relative_to(payload).as_posix() for p in payload.rglob('*') if p.is_dir()},key=lambda x:(-x.count('/'),x))
    files=[f for f in files if f not in ('path-owner.txt',)]
    def ns(s):return s.replace('$','$$').replace('/','\\')
    key=product+'-Student'
    path_install='''
  nsExec::ExecToStack '"$SYSDIR\\WindowsPowerShell\\v1.0\\powershell.exe" -NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass -File "$INSTDIR\\UserPath.ps1" -Action Add -InstallDir "$INSTDIR"'
  Pop $0
  Pop $1
  ${If} $0 != 0
    SetErrorLevel 1
    Abort "Cannot update the user PATH. Run Uninstall.exe before retrying."
  ${EndIf}
  SendMessage ${HWND_BROADCAST} ${WM_SETTINGCHANGE} 0 "STR:Environment" /TIMEOUT=5000
''' if product=='CalculiX_Win' else ''
    path_remove=path_install.replace('-Action Add','-Action Remove').replace('Run Uninstall.exe before retrying.','PATH was not changed; please retry uninstall.')
    launch=f'  CreateShortcut "$DESKTOP\\{product}.lnk" "$INSTDIR\\{exe}"\n  CreateShortcut "$SMPROGRAMS\\{product}\\{product}.lnk" "$INSTDIR\\{exe}"\n' if product=='PrePoMax-RCM' else ''
    text=f'''Unicode true
!include "MUI2.nsh"
!include "LogicLib.nsh"
!include "WinMessages.nsh"
Name "{product} {version}"
!ifndef OUTDIR
!define OUTDIR "../dist"
!endif
OutFile "${{OUTDIR}}/{filename}"
InstallDir "$LOCALAPPDATA\\Programs\\{product}"
RequestExecutionLevel user
SetCompressor /SOLID lzma
SetCompressorDictSize 32
ManifestDPIAware true
VIProductVersion "{('0.1.0.1' if product=='PrePoMax-RCM' else '2.21.0.2')}"
VIAddVersionKey /LANG=1028 "ProductName" "{product}"
VIAddVersionKey /LANG=1028 "CompanyName" "S.Y. Su"
VIAddVersionKey /LANG=1028 "FileDescription" "Student per-user installer"
VIAddVersionKey /LANG=1028 "FileVersion" "{version}"
VIAddVersionKey /LANG=1028 "LegalCopyright" "S.Y. Su; upstream notices retained"
!define MUI_ABORTWARNING
!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH
!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES
!insertmacro MUI_UNPAGE_FINISH
!insertmacro MUI_LANGUAGE "TradChinese"
!insertmacro MUI_LANGUAGE "English"
Function .onInit
  SetShellVarContext current
  SetRegView 64
FunctionEnd
Function un.onInit
  SetShellVarContext current
  SetRegView 64
FunctionEnd
Section "Install"
  SetOutPath "$INSTDIR"
  File /r "../payload/{product}/*"
  WriteUninstaller "$INSTDIR\\Uninstall.exe"
{path_install}
  CreateDirectory "$SMPROGRAMS\\{product}"
{launch}  CreateShortcut "$SMPROGRAMS\\{product}\\功能與安裝說明.lnk" "$INSTDIR\\INSTALL_一鍵安裝.md"
  CreateShortcut "$SMPROGRAMS\\{product}\\解除安裝.lnk" "$INSTDIR\\Uninstall.exe"
  WriteRegStr HKCU "Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\{key}" "DisplayName" "{product} {version}"
  WriteRegStr HKCU "Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\{key}" "DisplayVersion" "{version}"
  WriteRegStr HKCU "Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\{key}" "Publisher" "S.Y. Su"
  WriteRegStr HKCU "Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\{key}" "InstallLocation" "$INSTDIR"
  WriteRegStr HKCU "Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\{key}" "UninstallString" '$\\"$INSTDIR\\Uninstall.exe$\\"'
  WriteRegStr HKCU "Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\{key}" "QuietUninstallString" '$\\"$INSTDIR\\Uninstall.exe$\\" /S'
  WriteRegDWORD HKCU "Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\{key}" "NoModify" 1
  WriteRegDWORD HKCU "Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\{key}" "NoRepair" 1
SectionEnd
Section "Uninstall"
{path_remove}
'''
    if product=='PrePoMax-RCM':
        for d in dirs:
            if d=='Python' or d.startswith('Python/'):
                text+=f'  Delete "$INSTDIR\\{ns(d)}\\__pycache__\\*.pyc"\n'
                text+=f'  RMDir "$INSTDIR\\{ns(d)}\\__pycache__"\n'
    for f in files+['path-owner.txt','Uninstall.exe']+(['settings.bin','trace.info'] if product=='PrePoMax-RCM' else []):
        text+=f'  Delete "$INSTDIR\\{ns(f)}"\n'
    # Delete only shipped files, never recursive deletion of the student's folder.
    for d in dirs:text+=f'  RMDir "$INSTDIR\\{ns(d)}"\n'
    if product=='PrePoMax-RCM':text+='  RMDir "$INSTDIR\\Work"\n'
    text+=f'''  RMDir "$INSTDIR"
  Delete "$DESKTOP\\{product}.lnk"
  Delete "$SMPROGRAMS\\{product}\\{product}.lnk"
  Delete "$SMPROGRAMS\\{product}\\功能與安裝說明.lnk"
  Delete "$SMPROGRAMS\\{product}\\解除安裝.lnk"
  RMDir "$SMPROGRAMS\\{product}"
  DeleteRegKey HKCU "Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\{key}"
SectionEnd
'''
    (ROOT/'packaging'/f'{product}.nsi').write_text(text,encoding='utf-8')

if __name__=='__main__':
    generate('PrePoMax-RCM','0.1.0-rc1g8-win.1','PrePoMax-RCM_Setup_0.1.0-rc1g8-win.1.exe','PrePoMax.exe')
    generate('CalculiX_Win','2.21-win64.2','CalculiX_Win_Setup_2.21.exe','ccx.exe')
