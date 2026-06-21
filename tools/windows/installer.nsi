!include "MUI2.nsh"
!include "LogicLib.nsh"
!include "nsDialogs.nsh"
!include "FileFunc.nsh"
!insertmacro GetParameters
!insertmacro GetOptions

!define APP_NAME "Ban Words Filter"
!define APP_EXE "BanWordsFilter.exe"
!define APP_PUBLISHER "taganovv"
!define APP_VERSION "4.0.1"
!define APP_ID "BanWordsFilter"
!define DONATE_URL "https://donatex.gg/donate/tagan"
!define PAYLOAD_DIR "..\..\dist\installer-payload"

Name "${APP_NAME}"
OutFile "..\..\dist\Ban Words Filter Windows\Ban Words Filter Setup.exe"
InstallDir "$PROGRAMFILES64\${APP_NAME}"
InstallDirRegKey HKLM "Software\${APP_ID}" "InstallDir"
RequestExecutionLevel admin
ShowInstDetails show
Unicode true

Var CreateDesktopShortcut
Var OpenAfterInstall
Var SupportDeveloper
Var FinishPageConfirmed
Var FinishOpenProgramCheckbox
Var FinishSupportDeveloperCheckbox

!define MUI_ABORTWARNING
!define MUI_ICON "${PAYLOAD_DIR}\app-icon.ico"
!define MUI_UNICON "${PAYLOAD_DIR}\app-icon.ico"

!insertmacro MUI_PAGE_WELCOME
!insertmacro MUI_PAGE_DIRECTORY
Page custom DesktopShortcutPage DesktopShortcutPageLeave
!insertmacro MUI_PAGE_INSTFILES
Page custom FinishPage FinishPageLeave

!insertmacro MUI_UNPAGE_CONFIRM
!insertmacro MUI_UNPAGE_INSTFILES

!insertmacro MUI_LANGUAGE "Russian"

!define LOCKED_FILES_MSG "Не удалось установить ${APP_NAME}.$\r$\n$\r$\nФайлы программы заняты и не могут быть перезаписаны.$\r$\n$\r$\nЧто сделать:$\r$\n1. Закройте программу через иконку в трее → «Выход»$\r$\n   или завершите ${APP_EXE} в диспетчере задач.$\r$\n2. Запустите установщик снова.$\r$\n$\r$\nЕсли программа не запущена, проверьте антивирус или перезагрузите компьютер."

!macro DefineAppProcessHelpers UNPREFIX RETRYTAG
Function ${UNPREFIX}IsAppRunning
  ClearErrors
  ExecWait 'cmd /c tasklist /FI "IMAGENAME eq ${APP_EXE}" /NH | find /I "${APP_EXE}"' $0
FunctionEnd

Function ${UNPREFIX}CloseRunningAppWithRetry
  StrCpy $R1 0
${RETRYTAG}:
  DetailPrint "Закрытие ${APP_NAME}..."
  ExecWait '"$SYSDIR\taskkill.exe" /F /IM "${APP_EXE}" /T' $0
  Sleep 1500
  Call ${UNPREFIX}IsAppRunning
  ${If} $0 != 0
    Return
  ${EndIf}
  IntOp $R1 $R1 + 1
  ${If} $R1 < 3
    Goto ${RETRYTAG}
  ${EndIf}
FunctionEnd

Function ${UNPREFIX}EnsureAppNotRunningOrAbort
  Call ${UNPREFIX}CloseRunningAppWithRetry
  Call ${UNPREFIX}IsAppRunning
  ${If} $0 != 0
    Return
  ${EndIf}

  ${If} ${Silent}
    Return
  ${EndIf}

  MessageBox MB_ICONSTOP|MB_OK "${LOCKED_FILES_MSG}"
  Abort
FunctionEnd
!macroend

!insertmacro DefineAppProcessHelpers "" "retry_install"
!insertmacro DefineAppProcessHelpers "un." "retry_uninstall"

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

Function FinishPage
  ${If} ${Silent}
    Abort
  ${EndIf}

  !insertmacro MUI_HEADER_TEXT "Установка завершена" "Ban Words Filter готов к работе"

  nsDialogs::Create 1018
  Pop $0
  ${IfThen} $0 == error ${|} Abort ${|}

  ${NSD_CreateCheckbox} 0 0 100% 12u "Открыть программу"
  Pop $FinishOpenProgramCheckbox
  ${NSD_Check} $FinishOpenProgramCheckbox

  ${NSD_CreateCheckbox} 0 20u 100% 12u "Поддержать разработчика"
  Pop $FinishSupportDeveloperCheckbox
  ${NSD_Check} $FinishSupportDeveloperCheckbox

  nsDialogs::Show
FunctionEnd

Function FinishPageLeave
  StrCpy $FinishPageConfirmed 1

  ${NSD_GetState} $FinishOpenProgramCheckbox $0
  ${If} $0 == ${BST_CHECKED}
    StrCpy $OpenAfterInstall 1
  ${Else}
    StrCpy $OpenAfterInstall 0
  ${EndIf}

  ${NSD_GetState} $FinishSupportDeveloperCheckbox $0
  ${If} $0 == ${BST_CHECKED}
    StrCpy $SupportDeveloper 1
  ${Else}
    StrCpy $SupportDeveloper 0
  ${EndIf}
FunctionEnd

Section "Ban Words Filter" SecMain
  SectionIn RO
  Call EnsureAppNotRunningOrAbort
  SetOutPath "$INSTDIR"
  AllowSkipFiles off
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
  Call un.EnsureAppNotRunningOrAbort
  Delete "$DESKTOP\${APP_NAME}.lnk"
  RMDir /r "$SMPROGRAMS\${APP_NAME}"
  RMDir /r "$INSTDIR"
  DeleteRegKey HKLM "Software\Microsoft\Windows\CurrentVersion\Uninstall\${APP_ID}"
  DeleteRegKey HKLM "Software\${APP_ID}"
SectionEnd

Function .onInit
  StrCpy $CreateDesktopShortcut 1
  StrCpy $OpenAfterInstall 0
  StrCpy $SupportDeveloper 0
  StrCpy $FinishPageConfirmed 0
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

Function .onInstFailed
  ${IfNot} ${Silent}
    MessageBox MB_ICONSTOP|MB_OK "${LOCKED_FILES_MSG}"
  ${EndIf}
FunctionEnd

Function .onGUIEnd
  ${If} ${Silent}
    Return
  ${EndIf}

  ${If} $FinishPageConfirmed != 1
    Return
  ${EndIf}

  ${If} $OpenAfterInstall == 1
    Exec '"$INSTDIR\${APP_EXE}"'
  ${EndIf}

  ${If} $SupportDeveloper == 1
    ExecShell "open" "${DONATE_URL}"
  ${EndIf}
FunctionEnd
