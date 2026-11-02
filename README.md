# KlikAuto

Auto clicker Windows 10/11 berbasis C# WinForms (.NET 8).

## Fitur
- Follow cursor: klik di posisi pointer saat ini setiap interval.
- Fixed position: simpan posisi X/Y dengan tombol Capture current cursor.
- Left / Right / Middle click.
- Interval 1–600.000 ms.
- Jumlah klik terbatas atau unlimited.
- Global toggle hotkey yang bisa diganti sendiri (default F6).
- F7 selalu menjadi emergency stop melalui tombol Stop di aplikasi? Tombol Stop (F7) tersedia di UI; global emergency-stop hotkey tidak didaftarkan terpisah.

## Build menjadi EXE
1. Install **.NET 8 SDK** dari https://dotnet.microsoft.com/download/dotnet/8.0
2. Extract ZIP ini.
3. Buka Terminal / PowerShell di folder `KlikAuto`.
4. Jalankan:
   ```powershell
   dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
   ```
5. EXE ada di:
   `bin\Release\net8.0-windows\win-x64\publish\KlikAuto.exe`

## Catatan
- Hotkey toggle adalah satu tombol keyboard yang dipilih melalui Change key.
- Kalau tombol hotkey sudah dipakai aplikasi lain, pilih tombol lain.
- Gunakan interval yang wajar. Beberapa game/aplikasi dapat melarang auto-clicker.


## Build cepat
Di Windows, install .NET 8 SDK lalu double-click `build-exe.bat`. Hasil EXE akan berada di `bin\\Release\\net8.0-windows\\win-x64\\publish\\KlikAuto.exe`.


## Build EXE tanpa install SDK di PC kamu (GitHub Actions)

1. Buat repository GitHub baru, lalu upload seluruh isi folder `KlikAuto` (termasuk folder `.github`).
2. Buka tab **Actions** pada repository.
3. Pilih workflow **Build KlikAuto EXE**, lalu klik **Run workflow**.
4. Tunggu proses selesai, buka hasil run, lalu download artifact **KlikAuto-Windows-x64**.
5. Extract artifact. Jalankan `KlikAuto.exe` langsung di Windows 10/11. Tidak perlu install .NET SDK di PC.

Build dilakukan di runner Windows milik GitHub. File EXE hasil publish bersifat self-contained.
