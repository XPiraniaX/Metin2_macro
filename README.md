# Metin2Macro

Prosta aplikacja Windows w C# / .NET 8 przeznaczona do wysyłania F3/F4 w tle.

## Domyślne ustawienia

- `R` — globalne ON/OFF
- `F3` — co 1500 ms
- `F4` — co 500 ms
- aplikacja może być zminimalizowana do traya
- interwały można zmienić w GUI

## Pojedynczy EXE

W terminalu:

```powershell
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

Gotowy plik:

`bin/Release/net8.0-windows/win-x64/publish/Metin2Macro.exe`

