!include "MUI2.nsh"
!include "LogicLib.nsh"
!include "nsDialogs.nsh"
!include "FileFunc.nsh"
!insertmacro GetParameters
!insertmacro GetOptions

!define APP_NAME "Ban Words Filter"
!define APP_EXE "BanWordsFilter.exe"
!define APP_PUBLISHER "taganovv"
!define APP_VERSION "2.1.7"
!define APP_ID "BanWordsFilter"
!define PAYLOAD_DIR "..\..\dist\installer-payload"

Name "${APP_NAME}"
OutFile "..\..\dist\Ban Words Filter Windows\Ban Words Filter Setup.exe"
InstallDir "$PROGRAMFILES64\${APP_NAME}"
InstallDirRegKey HKLM "Software\${APP_ID}" "InstallDir"
RequestExecutionLevel admin
ShowInstDetails show
Unicode true

Var CreateDesktopShortcut

!define MUI_ABORTWARNING
!define MUI_ICON "${PAYLOAD_DIR}\app-icon.ico"
!define MUI_UNICON "${PAYLOAD_DIR}\app-icon.ico"

!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_DIRECTORY
Page custom DesktopShortcutPage DesktopShortcutPageLeave
!insertmacro MUI_PAGE_INSTFILES
!insertmacro MUI_PAGE_FINISH

!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES

!insertmacro MUI_LANGUAGE "Russian"

Function DesktopShortcutPage
  !insertmacro MUI_HEADER_TEXT "Дополнительные параметры" "Выберите параметры установки"
  nsDialogs::Create 1018
  Pop $0
  ${IfThen} $0 == error ${|} Abort ${|}

  ${NSD_CreateLabel} 0 0 100% 24u "Можно создать ярлык на рабочем столе для быстрого запуска приложения."
  Pop $0

  ${NSD_CreateCheckbox} 0 30u 100% 12u "Создать ярлык на рабочем столе"
  Pop $1
  ${NSD_Check} $1
  StrCpy $CreateDesktopShortcut 1

  nsDialogs::Show
FunctionEnd

Function DesktopShortcutPageLeave
  ${NSD_GetState} $1 $0
  ${If} $0 == ${BST_CHECKED}
    StrCpy $CreateDesktopShortcut 1
  ${Else}
    StrCpy $CreateDesktopShortcut 0
  ${EndIf}
FunctionEnd

Section "Ban Words Filter" SecMain
  SectionIn RO
  SetOutPath "$INSTDIR"
  File /r "${PAYLOAD_DIR}\*.*"

  WriteRegStr HKLM "Software\${APP_ID}" "InstallDir" "$INSTDIR"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APP_ID}" "DisplayName" "${APP_NAME}"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APP_ID}" "UninstallString" "$\"$INSTDIR\Uninstall.exe$\""
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APP_ID}" "DisplayIcon" "$INSTDIR\app-icon.ico"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APP_ID}" "Publisher" "${APP_PUBLISHER}"
  WriteRegStr HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APP_ID}" "DisplayVersion" "${APP_VERSION}"
  WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APP_ID}" "NoModify" 1
  WriteRegDWORD HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APP_ID}" "NoRepair" 1

  CreateDirectory "$SMPROGRAMS\${APP_NAME}"
  CreateShortcut "$SMPROGRAMS\${APP_NAME}\${APP_NAME}.lnk" "$INSTDIR\${APP_EXE}" "" "$INSTDIR\app-icon.ico" 0
  CreateShortcut "$SMPROGRAMS\${APP_NAME}\Uninstall.lnk" "$INSTDIR\Uninstall.exe"

  ${If} $CreateDesktopShortcut == 1
    CreateShortcut "$DESKTOP\${APP_NAME}.lnk" "$INSTDIR\${APP_EXE}" "" "$INSTDIR\app-icon.ico" 0
  ${EndIf}

  WriteUninstaller "$INSTDIR\Uninstall.exe"
SectionEnd

Section "Uninstall"
  Delete "$DESKTOP\${APP_NAME}.lnk"
  RMDir /r "$SMPROGRAMS\${APP_NAME}"
  RMDir /r "$INSTDIR"
  DeleteRegKey HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APP_ID}"
  DeleteRegKey HKLM "Software\${APP_ID}"
SectionEnd

Function .onInit
  StrCpy $CreateDesktopShortcut 1
  ${GetParameters} $0
  ${GetOptions} $0 "/S" $1
  ${IfNot} $1 == ""
    SetSilent silent
  ${EndIf}
FunctionEnd

Function un.onInit
  ${GetParameters} $0
  ${GetOptions} $0 "/S" $1
  ${IfNot} $1 == ""
    SetSilent silent
  ${EndIf}
FunctionEnd
