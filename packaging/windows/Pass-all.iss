; Script Inno Setup pour Pass-all
; Compiler avec Inno Setup 6+ : https://jrsoftware.org/isinfo.php
; Avant de compiler, publier l'app : dotnet publish -c Release -r win-x64 --self-contained true -o publish\windows

#define AppName "Pass-all"
#define AppVersion "1.0.0"
#define AppPublisher "Tom Fuster"
#define AppExeName "Pass-all.exe"
#define PublishDir "..\..\build\publish-windows"

[Setup]
AppId={{8F4A2B3C-1D5E-4F6A-9B0C-2E7D8F1A3B4C}
AppName={#AppName}
AppVersion={#AppVersion}
AppPublisher={#AppPublisher}
DefaultDirName={autopf}\{#AppName}
DefaultGroupName={#AppName}
OutputDir=..\..\build
OutputBaseFilename=Pass-all-Setup-Windows-x64
SetupIconFile=pass-all.ico
Compression=lzma2/ultra64
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=admin
ArchitecturesInstallIn64BitMode=x64compatible
MinVersion=10.0.17763
UninstallDisplayIcon={app}\{#AppExeName}

[Languages]
Name: "french"; MessagesFile: "compiler:Languages\French.isl"
Name: "english"; MessagesFile: "compiler:Default.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
; Tous les fichiers publiés
Source: "{#PublishDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\{#AppName}"; Filename: "{app}\{#AppExeName}"
Name: "{group}\Désinstaller {#AppName}"; Filename: "{uninstallexe}"
Name: "{autodesktop}\{#AppName}"; Filename: "{app}\{#AppExeName}"; Tasks: desktopicon

[Run]
Filename: "{app}\{#AppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(AppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[UninstallDelete]
; Ne pas supprimer la base de données utilisateur (dans %LOCALAPPDATA%\Pass-all)
