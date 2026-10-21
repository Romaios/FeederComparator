#ifndef AppVersion
  #error AppVersion must be passed with /DAppVersion=x.y.z
#endif
#ifndef SourceDir
  #error SourceDir must be passed with /DSourceDir=path
#endif
#ifndef OutputDir
  #define OutputDir "."
#endif

[Setup]
AppId={{3245578D-6549-4798-9580-A52D3E9615DB}
AppName=Feeder kWh Copy
AppVersion={#AppVersion}
AppVerName=Feeder kWh Copy {#AppVersion}
AppPublisher=SORECO II
SetupIconFile=..\src\FeederCopy.App\Resources\AppIcon\app.ico
DefaultDirName={userdocs}\Feeder kWh Copy
DisableDirPage=no
DisableProgramGroupPage=yes
DefaultGroupName=Feeder kWh Copy
PrivilegesRequired=lowest
UsePreviousAppDir=yes
UninstallDisplayIcon={app}\Feeder kWh Copy.exe
UninstallDisplayName=Feeder kWh Copy
OutputDir={#OutputDir}
OutputBaseFilename=FeederCopy-Setup
Compression=lzma2
SolidCompression=yes
WizardStyle=modern
ArchitecturesAllowed=x64compatible
ArchitecturesInstallIn64BitMode=x64compatible
CloseApplications=yes

[Tasks]
Name: "desktopicon"; Description: "Create a desktop shortcut"; GroupDescription: "Shortcuts:"

[Files]
Source: "{#SourceDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\Feeder kWh Copy"; Filename: "{app}\Feeder kWh Copy.exe"
Name: "{userdesktop}\Feeder kWh Copy"; Filename: "{app}\Feeder kWh Copy.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\Feeder kWh Copy.exe"; Description: "Open Feeder kWh Copy now"; Flags: nowait postinstall skipifsilent
