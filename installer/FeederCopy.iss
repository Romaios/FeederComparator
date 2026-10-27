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
AppName=Feeder Comparator
AppVersion={#AppVersion}
AppVerName=Feeder Comparator {#AppVersion}
AppPublisher=SORECO II
SetupIconFile=..\src\FeederCopy.App\Resources\AppIcon\app.ico
DefaultDirName={userdocs}\Feeder Comparator
DisableDirPage=no
DisableProgramGroupPage=yes
DefaultGroupName=Feeder Comparator
PrivilegesRequired=lowest
UsePreviousAppDir=yes
UsePreviousGroup=no
UninstallDisplayIcon={app}\Feeder Comparator.exe
UninstallDisplayName=Feeder Comparator
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

[InstallDelete]
Type: files; Name: "{userprograms}\Feeder kWh Copy\Feeder kWh Copy.lnk"
Type: dirifempty; Name: "{userprograms}\Feeder kWh Copy"
Type: files; Name: "{userdesktop}\Feeder kWh Copy.lnk"

[Icons]
Name: "{group}\Feeder Comparator"; Filename: "{app}\Feeder Comparator.exe"
Name: "{userdesktop}\Feeder Comparator"; Filename: "{app}\Feeder Comparator.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\Feeder Comparator.exe"; Description: "Open Feeder Comparator now"; Flags: nowait postinstall skipifsilent
