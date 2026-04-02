# RustUpdateNotes

Rust için güncelleme, wipe tarihi, mağaza ve commit takibi yapan bir Discord botu.

## Özellikler

- Sunucuya otomatik kategori ve kanal kurulumu
- Güncelleme tarihi paylaşımı ve saatlik yenileme
- Oyuncu/sunucu build değişimi tespiti
- Facepunch commit takibi
- Haftalık mağaza (skin) takibi
- `/destek` slash komutu

## Gereksinimler

- Windows
- .NET Framework 4.8.1
- SteamCMD (`C:\steamcmd\steamcmd.exe`)
- Discord Bot Token

## Kurulum

1. Depoyu klonlayın.
2. NuGet paketlerini geri yükleyin.
3. `Rust Update Notes.sln` çözümünü Visual Studio ile açın.
4. Projeyi derleyin.

## Yapılandırma

`/home/runner/work/RustUpdateNotes/RustUpdateNotes/Main/Helpers/Global.cs` dosyasındaki değerleri kendi ortamınıza göre düzenleyin:

- `Token`
- `DiscordLog` webhook
- `MainDiscordSohbet`
- `Mavi`
- `MainDiscordID`

## Çalıştırma

- Uygulamayı başlatın.
- Bot giriş yaptıktan sonra görev döngüleri otomatik çalışır.
- Bot eklendiği sunucularda gerekli kanal/kategori yapısını oluşturur.

## Notlar

- Proje `net481` hedefler.
- Linux üzerinde `dotnet build` sırasında `.NET Framework 4.8.1 targeting pack` eksikse derleme başarısız olur.
