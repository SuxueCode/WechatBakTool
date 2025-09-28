[Setup]
AppName=WechatBakTool
AppVersion=0.9.7.6
DefaultDirName={pf}\WechatBakTool
OutputDir=F:\下载
OutputBaseFilename=WechatBakTool0.9.7.6安装包
Compression=lzma
SolidCompression=yes

[Files]
Source: "E:\私人\WechatBakTool\WechatBakTool\bin\Release\net6.0-windows\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Tasks]
Name: "desktopicon"; Description: "创建桌面快捷方式"; GroupDescription: "附加图标"; Flags: checkablealone

[Icons]
Name: "{group}\WechatBakTool"; Filename: "{app}\WechatBakTool.exe"
Name: "{commondesktop}\WechatBakTool"; Filename: "{app}\WechatBakToolp.exe"; IconFilename: "{app}\avalonia-logo.ico"; Tasks: desktopicon

[Run]
Filename: "{app}\WechatBakTool.exe"; Description: "启动程序"; Flags: nowait postinstall skipifsilent