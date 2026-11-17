# Entrega — ¡Una Ronda Más!

| Archivo | Para qué |
| --- | --- |
| `UnaRondaMas_WebGL.zip` | Build WebGL más reciente (17/11) para itch.io (Kind: HTML, 1280 × 720, Fullscreen button) |
| `../docs/jugar/` | La misma build sin comprimir, para jugar en el navegador desde GitHub Pages |
| `UnaRondaMas_gameplay.mp4` | Video de gameplay de 95 s con las 10 rondas, sin sonido, para editar el pitch |
| `itch/portada_630x500.png` | Portada de la página de itch.io |
| `itch/captura_*.png` | Capturas para la página de itch.io |

Página del juego: https://nicolepatino.itch.io/una-ronda-mas

## Cómo jugar la build

- **Con un link (lo más fácil):** itch.io o GitHub Pages (`https://nicolepg.github.io/una-ronda-mas/jugar/`, cuando esté activado en Settings → Pages → rama `main`, carpeta `/docs`).
- **En tu PC:** con doble clic en `index.html` no carga (el navegador bloquea los archivos del juego). Descomprime el zip y, en esa carpeta, corre `python -m http.server 8765`; luego abre http://localhost:8765.

## Para volver a generarlos (Unity cerrado)

- Build: `powershell -ExecutionPolicy Bypass -File tools/build-webgl.ps1`
- Video: menú **Una Ronda Más → Pitch → Grabar video de gameplay** (sale en `Builds/Video`)
