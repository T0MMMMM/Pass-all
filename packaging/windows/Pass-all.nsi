; Script NSIS pour Pass-all — compilable depuis Linux avec : makensis Pass-all.nsi
; Documentation : https://nsis.sourceforge.io/Docs/

!include "MUI2.nsh"
!include "x64.nsh"

; ── Métadonnées ───────────────────────────────────────────────────────────────
!define APP_NAME      "Pass-all"
!ifndef APP_VERSION
  !define APP_VERSION "1.0.0"
!endif
!define APP_PUBLISHER "Tom Fuster"
!define APP_EXE       "Pass-all.exe"
!define APP_GUID      "{8F4A2B3C-1D5E-4F6A-9B0C-2E7D8F1A3B4C}"
!define PUBLISH_DIR   "..\..\build\publish-windows"

Name          "${APP_NAME} ${APP_VERSION}"
OutFile       "..\..\build\Pass-all-Setup-Windows-x64.exe"
InstallDir    "$PROGRAMFILES64\${APP_NAME}"
InstallDirRegKey HKLM "Software\${APP_NAME}" "InstallDir"
RequestExecutionLevel admin
SetCompressor /SOLID lzma

; ── UI ────────────────────────────────────────────────────────────────────────
!define MUI_ABORTWARNING
!define MUI_ICON   "logo.ico"
!define MUI_UNICON "logo.ico"

!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_DIRECTORY
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH

!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES

!insertmacro MUI_LANGUAGE "French"
!insertmacro MUI_LANGUAGE "English"

; ── Installation ──────────────────────────────────────────────────────────────
Section "Pass-all (requis)" SecMain
    SectionIn RO
    SetOutPath "$INSTDIR"

    ; Copier tous les fichiers publiés
    File /r "${PUBLISH_DIR}\*.*"

    ; Raccourci menu démarrer
    CreateDirectory "$SMPROGRAMS\${APP_NAME}"
    CreateShortcut "$SMPROGRAMS\${APP_NAME}\${APP_NAME}.lnk" "$INSTDIR\${APP_EXE}"
    CreateShortcut "$SMPROGRAMS\${APP_NAME}\Désinstaller ${APP_NAME}.lnk" "$INSTDIR\uninstall.exe"

    ; Raccourci bureau
    CreateShortcut "$DESKTOP\${APP_NAME}.lnk" "$INSTDIR\${APP_EXE}"

    ; Désinstalleur
    WriteUninstaller "$INSTDIR\uninstall.exe"

    ; Registre (ajout/suppression de programmes)
    WriteRegStr   HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APP_GUID}" "DisplayName"     "${APP_NAME}"
    WriteRegStr   HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APP_GUID}" "DisplayVersion"  "${APP_VERSION}"
    WriteRegStr   HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APP_GUID}" "Publisher"       "${APP_PUBLISHER}"
    WriteRegStr   HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APP_GUID}" "UninstallString" "$INSTDIR\uninstall.exe"
    WriteRegStr   HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APP_GUID}" "InstallLocation" "$INSTDIR"
    WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APP_GUID}" "NoModify"        1
    WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APP_GUID}" "NoRepair"        1
SectionEnd

; ── Désinstallation ───────────────────────────────────────────────────────────
Section "Uninstall"
    ; Supprimer les fichiers installés
    RMDir /r "$INSTDIR"

    ; Supprimer les raccourcis
    Delete "$DESKTOP\${APP_NAME}.lnk"
    RMDir /r "$SMPROGRAMS\${APP_NAME}"

    ; Supprimer la clé registre
    DeleteRegKey HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APP_GUID}"
    DeleteRegKey HKLM "Software\${APP_NAME}"

    ; Note : la base de données utilisateur (%LOCALAPPDATA%\Pass-all) n'est PAS supprimée
SectionEnd
