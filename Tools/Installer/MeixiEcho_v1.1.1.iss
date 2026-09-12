#define MyAppName "梅溪回响"
#define MyAppVersion "1.1.1"

[Setup]
AppId={{E2CFB7E7-632C-4B7C-81B0-F74D9089CC03}
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppPublisher=Meixi Digital Culture Lab
VersionInfoVersion=1.1.1.0
DefaultDirName={localappdata}\Programs\MeixiEcho
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
OutputDir=..\..\Release
OutputBaseFilename=梅溪回响_Windows_x64_安装包_v1.1.1
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
UninstallDisplayName={#MyAppName} v{#MyAppVersion}
UninstallDisplayIcon={app}\MeixiEcho.exe
CloseApplications=yes
RestartApplications=no
SetupLogging=yes
MinVersion=10.0

[Languages]
Name: "chinesesimp"; MessagesFile: "InnoSetup\tools\Languages\ChineseSimplified.isl"

[Tasks]
Name: "desktopicon"; Description: "创建桌面快捷方式"; GroupDescription: "附加快捷方式："; Flags: unchecked

[Files]
Source: "..\..\Build\Windows\MeixiEcho.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\..\Build\Windows\*.dll"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\..\Build\Windows\UnityCrashHandler64.exe"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\..\Build\Windows\MeixiEcho_Data\*"; DestDir: "{app}\MeixiEcho_Data"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\..\Build\Windows\MonoBleedingEdge\*"; DestDir: "{app}\MonoBleedingEdge"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\..\Build\Windows\D3D12\*"; DestDir: "{app}\D3D12"; Flags: ignoreversion recursesubdirs createallsubdirs
Source: "..\..\Build\Windows\MeixiHeritageEmblem.png"; DestDir: "{app}"; Flags: ignoreversion
Source: "..\..\Build\Windows\MinOperaPrelude-ThirdPartyNotice.txt"; DestDir: "{app}"; Flags: ignoreversion

[Icons]
Name: "{group}\{#MyAppName}"; Filename: "{app}\MeixiEcho.exe"; WorkingDir: "{app}"
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\MeixiEcho.exe"; WorkingDir: "{app}"; Tasks: desktopicon

[Run]
Filename: "{app}\MeixiEcho.exe"; Description: "启动 {#MyAppName}"; Flags: nowait postinstall skipifsilent

; Only registered installation files are removed. LocalLow saves are preserved.
