# Metin2Macro

Prosta aplikacja Windows w C# / .NET 8 przeznaczona do wysyłania F3/F4 w tle.

## Domyślne ustawienia

- `R` — globalne ON/OFF
- `F3` — co 1500 ms
- `F4` — co 500 ms
- aplikacja może być zminimalizowana do traya
- interwały można zmienić w GUI

## Uruchomienie w Rider

Otwórz `Metin2Macro.csproj` w JetBrains Rider i uruchom projekt.

## Pojedynczy EXE

W terminalu:

```powershell
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

Gotowy plik:

`bin/Release/net8.0-windows/win-x64/publish/Metin2Macro.exe`

## GitHub

```bash
git init
git add .
git commit -m "Initial Metin2 macro"
git branch -M main
git remote add origin <URL_REPO>
git push -u origin main
```

### Ważne

To wykorzystuje Windows `SendInput`. Metin2 lub jego klient/anty-cheat może ograniczać albo blokować syntetyczne wejście. Zachowanie może zależeć od konkretnego klienta gry.
