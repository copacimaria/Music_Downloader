# Unelte externe

Aplicația are nevoie de două executabile în acest folder:

| Fișier       | De unde                                                      |
|--------------|--------------------------------------------------------------|
| `yt-dlp.exe` | https://github.com/yt-dlp/yt-dlp/releases (`yt-dlp.exe`)      |
| `ffmpeg.exe` | https://www.gyan.dev/ffmpeg/builds/ (arhiva `essentials`)      |

Din arhiva ffmpeg copiază din `bin\` fișierele `ffmpeg.exe` și `ffprobe.exe`.

Structura finală:

```
Musiq/
└─ Tools/
   ├─ yt-dlp.exe
   ├─ ffmpeg.exe
   └─ ffprobe.exe
```

Căile sunt configurate în `appsettings.json`, secțiunea `YtDlp`.

## Actualizare

YouTube își schimbă des API-ul intern, așa că `yt-dlp` trebuie actualizat periodic:

```
Tools\yt-dlp.exe -U
```

Dacă descărcările încep să eșueze fără motiv aparent, prima verificare e aceasta.
