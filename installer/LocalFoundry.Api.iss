; Inno Setup 6 script for the LocalFoundry.Api Windows installer.
;
; Built by .github/workflows/release.yml from a self-contained `dotnet publish` output:
;   ISCC /DAppVersion=1.2.3 /DAppVersionNumeric=1.2.3 /DAppArch=x64 ^
;        /DPublishDir=<publish dir> /DOutputDir=<output dir> installer\LocalFoundry.Api.iss
;
; AppVersion may carry a prerelease suffix (1.2.3-beta.1); AppVersionNumeric must be the
; purely numeric part because Windows file version resources only accept a.b.c[.d].

#ifndef AppVersion
  #error AppVersion must be defined, e.g. ISCC /DAppVersion=1.2.3
#endif
#ifndef AppVersionNumeric
  #define AppVersionNumeric AppVersion
#endif
#ifndef AppArch
  #define AppArch "x64"
#endif
#if AppArch != "x64" && AppArch != "arm64"
  #error AppArch must be x64 or arm64
#endif
#ifndef PublishDir
  #error PublishDir must point at the `dotnet publish` output folder
#endif
#ifndef OutputDir
  #define OutputDir "Output"
#endif

#define AppName "LocalFoundry.Api"
#define AppPublisher "sagarworlds"
#define AppUrl "https://github.com/sagarworlds/foundry-local-semantic-kernel"
#define AppExe "LocalFoundry.Api.exe"
#define Launcher "Start-LocalFoundry.cmd"

[Setup]
; Never change AppId: Windows uses it to recognise upgrades and the uninstaller entry.
AppId={{2BF38A6F-47A6-4857-8D4A-3890DE6208AE}
AppName={#AppName}
AppVersion={#AppVersion}
AppVerName={#AppName} {#AppVersion}
AppPublisher={#AppPublisher}
AppPublisherURL={#AppUrl}
AppSupportURL={#AppUrl}/issues
AppUpdatesURL={#AppUrl}/releases
VersionInfoVersion={#AppVersionNumeric}
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
DisableProgramGroupPage=yes
; Per-user install by default: no UAC prompt, and appsettings.json stays editable without
; admin rights. Users can still choose an all-users install from the dialog.
PrivilegesRequired=lowest
PrivilegesRequiredOverridesAllowed=dialog
#if AppArch == "arm64"
ArchitecturesAllowed=arm64
ArchitecturesInstallIn64BitMode=arm64
#else
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
#endif
; .NET 9 and Foundry Local both require Windows 10 or later.
MinVersion=10.0
OutputDir={#OutputDir}
OutputBaseFilename={#AppName}-{#AppVersion}-win-{#AppArch}-setup
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
UninstallDisplayIcon={app}\{#AppExe}
SetupLogging=yes

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Files]
; Debug symbols, native import libraries and IIS/dev-only config aren't needed at runtime.
Source: "{#PublishDir}\*"; DestDir: "{app}"; \
  Excludes: "*.pdb,*.lib,web.config,appsettings.json,appsettings.Development.json"; \
  Flags: ignoreversion recursesubdirs createallsubdirs
; Installed only if missing so upgrades keep the user's Endpoint/ModelId edits.
Source: "{#PublishDir}\appsettings.json"; DestDir: "{app}"; Flags: onlyifdoesntexist
Source: "{#Launcher}"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#Launcher}"; WorkingDir: "{app}"; IconFilename: "{app}\{#AppExe}"; \
  Comment: "Start the API and open the browser test console"
Name: "{group}\Edit configuration"; Filename: "{win}\notepad.exe"; Parameters: """{app}\appsettings.json"""; \
  Comment: "Change the Foundry Local endpoint or model"
Name: "{group}\{cm:UninstallProgram,{#AppName}}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#Launcher}"; WorkingDir: "{app}"; IconFilename: "{app}\{#AppExe}"; \
  Tasks: desktopicon

[Run]
Filename: "{app}\{#Launcher}"; WorkingDir: "{app}"; Description: "Start {#AppName} and open the test console"; \
  Flags: postinstall nowait skipifsilent shellexec

[Code]
// Foundry Local ships as an MSIX package that exposes the 'foundry' CLI through an
// app execution alias in the per-user WindowsApps folder.
function IsFoundryLocalInstalled: Boolean;
begin
  Result := FileExists(ExpandConstant('{localappdata}\Microsoft\WindowsApps\foundry.exe'));
end;

// The API installs and starts fine without Foundry Local (it returns 503 until the
// runtime is up), so a missing runtime is a warning with setup steps, not a hard failure.
procedure CurStepChanged(CurStep: TSetupStep);
begin
  if (CurStep = ssPostInstall) and not IsFoundryLocalInstalled then
  begin
    SuppressibleMsgBox(
      'Foundry Local was not found on this PC. {#AppName} needs it to run models.' + #13#10#13#10 +
      'Install it and load a model from a terminal:' + #13#10#13#10 +
      '  winget install Microsoft.FoundryLocal' + #13#10 +
      '  foundry server start --port 5273' + #13#10 +
      '  foundry model download qwen2.5-0.5b' + #13#10 +
      '  foundry model load qwen2.5-0.5b',
      mbInformation, MB_OK, IDOK);
  end;
end;
