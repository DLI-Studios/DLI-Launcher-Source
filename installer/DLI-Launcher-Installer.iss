; DLI Launcher - Inno Setup Installer Script
; Build: "C:\Users\emmgg\AppData\Local\Programs\Inno Setup 7\ISCC.exe" DLI-Launcher-Installer.iss

#define MyAppName "DLI Launcher"
#define MyAppVersion "1.0.8"
#define MyAppExeName "DLI-Launcher.exe"
#define MyAppPublisher "DLI Studios"
#define MyAppURL "https://github.com/DLI-Studios/DLI-Launcher-Source"

[Setup]
AppId={{8F3C9E2A-5D4B-4C7E-9A1F-6B2C0D4E8F06}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
DefaultDirName={localappdata}\Programs\DLI-Launcher
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
OutputDir=output
OutputBaseFilename=DLI-Launcher-Setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
UninstallDisplayIcon={app}\{#MyAppExeName}
CloseApplications=yes
RestartApplications=no

[Languages]
Name: "english"; MessagesFile: "compiler:Default.isl"
Name: "turkish"; MessagesFile: "compiler:Languages\Turkish.isl"

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked
Name: "startmenuicon"; Description: "{cm:CreateStartMenu}"; GroupDescription: "{cm:AdditionalIcons}"

[Files]
Source: "..\DLI-Launcher-App\bin\Release\net9.0-windows\win-x64\publish\DLI-Launcher.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\launcher\dist\*"; DestDir: "{app}\DLI-Launcher"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "installer-files\MicrosoftEdgeWebview2Setup.exe"; DestDir: "{tmp}"; Flags: deleteafterinstall; Check: IsWebView2Missing

[Icons]
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon
Name: "{autoprograms}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: startmenuicon

[Run]
Filename: "{tmp}\MicrosoftEdgeWebview2Setup.exe"; Parameters: "/silent /install"; StatusMsg: "WebView2 Runtime kuruluyor..."; Check: IsWebView2Missing; Flags: waituntilterminated
Filename: "{app}\{#MyAppExeName}"; Description: "{cm:LaunchProgram,{#StringChange(MyAppName, '&', '&&')}}"; Flags: nowait postinstall skipifsilent

[CustomMessages]
english.CreateStartMenu=Create a &Start Menu shortcut
turkish.CreateStartMenu=&Başlat Menüsü kısayolu oluştur

[Code]
function IsWebView2Missing: Boolean;
begin
  Result := not RegKeyExists(HKEY_CURRENT_USER, 'Software\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}');
  if Result then
    Result := not RegKeyExists(HKEY_LOCAL_MACHINE, 'Software\WOW6432Node\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}');
  if Result then
    Result := not RegKeyExists(HKEY_LOCAL_MACHINE, 'Software\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}');
end;
