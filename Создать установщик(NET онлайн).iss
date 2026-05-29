[Setup]
AppName=NetworkMonitor
AppVerName=NetworkMonitor 1.0
AppVersion=1.0
AppPublisher=GrudakovE.A.
DefaultDirName={pf}\NetworkMonitor
DefaultGroupName=NetworkMonitor
OutputDir=installer
OutputBaseFilename=NetworkMonitor_Setup
SetupIconFile= Resources\app.ico
Compression=lzma
SolidCompression=yes

; Русский язык
[Languages]
Name: "russian"; MessagesFile: "compiler:Languages\Russian.isl"

; Компоненты (ярлыки опциональны)
[Tasks]
Name: "desktopicon"; Description: "Создать ярлык на рабочем столе"; GroupDescription: "Дополнительные значки:";
Name: "startmenuicon"; Description: "Создать ярлык в меню Пуск"; GroupDescription: "Дополнительные значки:"

[Files]
Source: "bin\x64\Release\*"; DestDir: "{app}"; Flags: recursesubdirs ignoreversion

[Icons]
; Меню Пуск
Name: "{group}\NetworkMonitor"; Filename: "{app}\NetworkMonitor.exe"; Tasks: startmenuicon
Name: "{group}\Удалить NetworkMonitor"; Filename: "{uninstallexe}"; Tasks: startmenuicon

; Рабочий стол
Name: "{commondesktop}\NetworkMonitor"; Filename: "{app}\NetworkMonitor.exe"; Tasks: desktopicon

[Run]
Filename: "{app}\NetworkMonitor.exe"; Description: "Запустить NetworkMonitor"; Flags: nowait postinstall skipifsilent

[Code]
function IsDotNet48Installed(): Boolean;
var
  Installed: Cardinal;
begin
  Result := RegQueryDWordValue(HKLM,
    'SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full', 'Release', Installed)
    and (Installed >= 528040);
end;

function InitializeSetup(): Boolean;
var
  ErrorCode: Integer;
begin
  if not IsDotNet48Installed() then
  begin
    if MsgBox('.NET Framework 4.8 не установлен.' + #13#10 +
      'Открыть страницу загрузки в браузере?',
      mbError, MB_YESNO) = IDYES then
    begin
      ShellExec('open',
        'https://dotnet.microsoft.com/download/dotnet-framework/net48',
        '', '', SW_SHOWNORMAL, ewNoWait, ErrorCode);
    end;
    Result := False;
  end
  else
    Result := True;
end;

procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
var
  ResultCode: Integer;
  AppDir: String;
begin
  if CurUninstallStep = usUninstall then
  begin
    // Завершаем процесс
    Exec('taskkill.exe', '/F /IM NetworkMonitor.exe', '', SW_HIDE,
      ewWaitUntilTerminated, ResultCode);
  end;

  if CurUninstallStep = usPostUninstall then
  begin
    AppDir := ExpandConstant('{app}');
    if DirExists(AppDir) then
    begin
      if MsgBox('Удалить папку программы и все данные внутри?',
        mbConfirmation, MB_YESNO) = IDYES then
      begin
        DelTree(AppDir, True, True, True);
      end;
    end;
  end;
end;