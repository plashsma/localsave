#ifndef AppVersion
  #define AppVersion "2.7.0"
#endif

[Setup]
#ifdef TestBuild
AppId=LocalSave-Installer-Smoke-Test
AppName=LocalSave Installer Test
#else
AppId={{9AAAD126-FC12-47CE-92E8-AE85497D28EA}
AppName=LocalSave
#endif
AppVersion={#AppVersion}
AppPublisher=PLASHSMA
AppPublisherURL=https://github.com/plashsma/localsave
DefaultDirName={localappdata}\Programs\LocalSave
DefaultGroupName=LocalSave
UsePreviousAppDir=yes
PrivilegesRequired=lowest
MinVersion=10.0
WizardStyle=modern
DisableProgramGroupPage=yes
LicenseFile=..\LICENSE
SetupIconFile=..\artifacts\LocalSave.ico
UninstallDisplayIcon={app}\LocalSave.exe
OutputDir=..\dist
OutputBaseFilename=LocalSave-Setup-{#AppVersion}
Compression=lzma2/fast
SolidCompression=no
#ifdef TestBuild
AppMutex=Local\LocalSaveInstallerSmokeNative
#else
AppMutex=Local\LocalOfficeAutoSave-Native
#endif
CloseApplications=no
RestartApplications=no
SetupLogging=yes
VersionInfoVersion={#AppVersion}.0

[Tasks]
Name: desktopicon; Description: "Create a desktop shortcut"; Flags: unchecked

[Files]
Source: "..\dist\LocalSave-{#AppVersion}.exe"; DestDir: "{app}"; DestName: "LocalSave.exe"; Flags: ignoreversion
Source: "..\LICENSE"; DestDir: "{app}"; DestName: "LICENSE.txt"; Flags: ignoreversion

[Icons]
Name: "{userprograms}\LocalSave\LocalSave"; Filename: "{app}\LocalSave.exe"
Name: "{userprograms}\LocalSave\Uninstall LocalSave"; Filename: "{uninstallexe}"
Name: "{userdesktop}\LocalSave"; Filename: "{app}\LocalSave.exe"; Tasks: desktopicon

[Registry]
#ifndef TestBuild
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Run"; ValueName: "LocalOfficeAutoSave"; Flags: uninsdeletevalue
#endif

[Run]
Filename: "{app}\LocalSave.exe"; Description: "Launch LocalSave"; Flags: nowait postinstall skipifsilent; Check: CanLaunch

[Code]

function DotNetInstalled: Boolean;
var Release: Cardinal;
begin
#ifdef TestMissingRuntime
  Result := False;
  exit;
#endif
  Result := RegQueryDWordValue(HKLM32, 'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full', 'Release', Release) and (Release >= 528040);
  if not Result and IsWin64 then
    Result := RegQueryDWordValue(HKLM64, 'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full', 'Release', Release) and (Release >= 528040);
end;

function PrepareToInstall(var NeedsRestart: Boolean): String;
var Code: Integer;
begin
  Result := '';
  if DotNetInstalled then exit;
  Result := 'Microsoft .NET Framework 4.8 or later is required. Install it from Microsoft, then run LocalSave Setup again. No application files have been installed or replaced.';
  if not WizardSilent then
    if MsgBox('LocalSave requires .NET Framework 4.8 or later, which was not detected. Open the official Microsoft download page?', mbConfirmation, MB_YESNO) = IDYES then
      ShellExec('open', 'https://support.microsoft.com/en-us/servicing/dotnetframework/2019/10/microsoft-net-framework-4-8-offline-installer-for-windows', '', '', SW_SHOWNORMAL, ewNoWait, Code);
end;

function CanLaunch: Boolean;
begin
  Result := DotNetInstalled;
end;

procedure CurStepChanged(CurStep: TSetupStep);
var Previous: String;
begin
#ifndef TestBuild
  if CurStep = ssPostInstall then begin
    if RegQueryStringValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Run', 'LocalOfficeAutoSave', Previous) then begin
      if not RegWriteStringValue(HKCU, 'Software\Microsoft\Windows\CurrentVersion\Run', 'LocalOfficeAutoSave', '"' + ExpandConstant('{app}\LocalSave.exe') + '" --background') then
        Log('Startup relocation failed; launch LocalSave to retry.');
    end;
  end;
#endif
end;
