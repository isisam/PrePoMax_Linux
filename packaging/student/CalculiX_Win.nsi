Unicode true
!include "MUI2.nsh"
!include "LogicLib.nsh"
!include "WinMessages.nsh"
Name "CalculiX_Win 2.21-win64.3"
!ifndef OUTDIR
!define OUTDIR "../dist"
!endif
OutFile "${OUTDIR}/CalculiX_Win_Setup_2.21.exe"
InstallDir "$LOCALAPPDATA\Programs\CalculiX_Win"
RequestExecutionLevel user
SetCompressor /SOLID lzma
SetCompressorDictSize 32
ManifestDPIAware true
VIProductVersion "2.21.0.3"
VIAddVersionKey /LANG=1028 "ProductName" "CalculiX_Win"
VIAddVersionKey /LANG=1028 "CompanyName" "S.Y. Su"
VIAddVersionKey /LANG=1028 "FileDescription" "Student per-user installer"
VIAddVersionKey /LANG=1028 "FileVersion" "2.21-win64.3"
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
  File /r "../payload/CalculiX_Win/*"
  WriteUninstaller "$INSTDIR\Uninstall.exe"

  nsExec::ExecToStack '"$SYSDIR\WindowsPowerShell\v1.0\powershell.exe" -NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass -File "$INSTDIR\UserPath.ps1" -Action Add -InstallDir "$INSTDIR"'
  Pop $0
  Pop $1
  ${If} $0 != 0
    SetErrorLevel 1
    Abort "Cannot update the user PATH. Run Uninstall.exe before retrying."
  ${EndIf}
  SendMessage ${HWND_BROADCAST} ${WM_SETTINGCHANGE} 0 "STR:Environment" /TIMEOUT=5000

  CreateDirectory "$SMPROGRAMS\CalculiX_Win"
  CreateShortcut "$SMPROGRAMS\CalculiX_Win\功能與安裝說明.lnk" "$INSTDIR\INSTALL_一鍵安裝.md"
  CreateShortcut "$SMPROGRAMS\CalculiX_Win\解除安裝.lnk" "$INSTDIR\Uninstall.exe"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\CalculiX_Win-Student" "DisplayName" "CalculiX_Win 2.21-win64.3"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\CalculiX_Win-Student" "DisplayVersion" "2.21-win64.3"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\CalculiX_Win-Student" "Publisher" "S.Y. Su"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\CalculiX_Win-Student" "InstallLocation" "$INSTDIR"
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\CalculiX_Win-Student" "UninstallString" '$\"$INSTDIR\Uninstall.exe$\"'
  WriteRegStr HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\CalculiX_Win-Student" "QuietUninstallString" '$\"$INSTDIR\Uninstall.exe$\" /S'
  WriteRegDWORD HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\CalculiX_Win-Student" "NoModify" 1
  WriteRegDWORD HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\CalculiX_Win-Student" "NoRepair" 1
SectionEnd
Section "Uninstall"

  nsExec::ExecToStack '"$SYSDIR\WindowsPowerShell\v1.0\powershell.exe" -NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass -File "$INSTDIR\UserPath.ps1" -Action Remove -InstallDir "$INSTDIR"'
  Pop $0
  Pop $1
  ${If} $0 != 0
    SetErrorLevel 1
    Abort "Cannot update the user PATH. PATH was not changed; please retry uninstall."
  ${EndIf}
  SendMessage ${HWND_BROADCAST} ${WM_SETTINGCHANGE} 0 "STR:Environment" /TIMEOUT=5000

  Delete "$INSTDIR\FEATURES_en.md"
  Delete "$INSTDIR\FEATURES_zh-TW.md"
  Delete "$INSTDIR\INSTALL_一鍵安裝.md"
  Delete "$INSTDIR\LICENSE"
  Delete "$INSTDIR\LICENSES\ARPACK-NG_COPYING.txt"
  Delete "$INSTDIR\LICENSES\COPYING.GPL-2.0_CalculiX.txt"
  Delete "$INSTDIR\LICENSES\CalculiX_debian_copyright.txt"
  Delete "$INSTDIR\LICENSES\GCC_Runtime_Library_Exception-3.1.txt"
  Delete "$INSTDIR\LICENSES\GCC_runtime_libgfortran_libgomp_libgcc_copyright.txt"
  Delete "$INSTDIR\LICENSES\GPL-3.0_GCC_runtime.txt"
  Delete "$INSTDIR\LICENSES\LAPACK_BLAS_LICENSE.txt"
  Delete "$INSTDIR\LICENSES\LGPL-2.1_libquadmath.txt"
  Delete "$INSTDIR\LICENSES\MinGW-w64_runtime_and_winpthreads_copyright.txt"
  Delete "$INSTDIR\LICENSES\SPOOLES_copyright.txt"
  Delete "$INSTDIR\LICENSES\THIRD_PARTY.md"
  Delete "$INSTDIR\NOTICE.md"
  Delete "$INSTDIR\SOURCE.md"
  Delete "$INSTDIR\UserPath.ps1"
  Delete "$INSTDIR\VERSION"
  Delete "$INSTDIR\ccx.exe"
  Delete "$INSTDIR\examples\README.md"
  Delete "$INSTDIR\examples\beamf.inp"
  Delete "$INSTDIR\examples\beamp.inp"
  Delete "$INSTDIR\examples\reference.json"
  Delete "$INSTDIR\examples\spring1.inp"
  Delete "$INSTDIR\examples\verification-windows.json"
  Delete "$INSTDIR\screenshots\layup-rc1g8.png"
  Delete "$INSTDIR\path-owner.txt"
  Delete "$INSTDIR\Uninstall.exe"
  RMDir "$INSTDIR\LICENSES"
  RMDir "$INSTDIR\examples"
  RMDir "$INSTDIR\screenshots"
  RMDir "$INSTDIR"
  Delete "$DESKTOP\CalculiX_Win.lnk"
  Delete "$SMPROGRAMS\CalculiX_Win\CalculiX_Win.lnk"
  Delete "$SMPROGRAMS\CalculiX_Win\功能與安裝說明.lnk"
  Delete "$SMPROGRAMS\CalculiX_Win\解除安裝.lnk"
  RMDir "$SMPROGRAMS\CalculiX_Win"
  DeleteRegKey HKCU "Software\Microsoft\Windows\CurrentVersion\Uninstall\CalculiX_Win-Student"
SectionEnd
