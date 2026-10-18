# Entrega — build WebGL, itch.io y GJ+ (Nicole)

Plazo: **domingo 18 de octubre de 2026, 23:00** (meta interna **21:00**). Cierre de funciones: domingo 17:00.

## 0. Una sola vez

- [ ] Unity Hub → Installs → 2022.3.62f2 → ⚙ → **Add modules** → **WebGL Build Support**.
- [ ] Abrir el proyecto y correr **Una Ronda Más → Build → Configurar WebGL** (cambia la plataforma a WebGL; tarda unos minutos).
  Deja: Compression **Gzip**, **Decompression Fallback** activado, 1280 × 720 y las escenas `MainMenu` y `Game` en Build Settings.

## 1. Hacer la build

Opción A (Unity abierto): **Una Ronda Más → Build → Hacer build WebGL** → sale en `Builds/WebGL`.

Opción B (terminal, Unity cerrado), también arma el zip:

```bash
powershell -ExecutionPolicy Bypass -File tools/build-webgl.ps1
```

Resultado: `Builds/UnaRondaMas_WebGL.zip` con `index.html` en la raíz (la carpeta `Builds/` no se sube a git).

## 2. Probar antes de subir

- [ ] La build abre en el navegador (desde itch en borrador, no con doble clic en `index.html`).
- [ ] El audio empieza después del primer clic.
- [ ] Se juega una partida completa: anuncio, ronda, premio, Eliminado y «¿Una ronda más?».
- [ ] El récord sigue después de recargar la página.
- [ ] No aparece el botón Salir en la versión web.
- [ ] Probado en **otra computadora** y en **otro navegador** (Chrome y Firefox).

## 3. itch.io

1. Dashboard → **Create new project**.
2. Kind of project: **HTML**. Subir `UnaRondaMas_WebGL.zip` y marcar **This file will be played in the browser**.
3. Embed options: viewport **1280 × 720**, activar **Fullscreen button**; SharedArrayBuffer apagado.
4. Portada, capturas y descripción en español e inglés (Tania).
5. Visibility: **Draft** para probar → **Public** cuando funcione.
6. Página de la jam GameJamPlus 26/27 en itch → **Submit your project**.

## 4. Plataforma Oficial de GJ+

- [ ] Registrar la build (enlace de itch.io) en la Plataforma Oficial de GJ+ antes del domingo a las 23:00.
- [ ] Los 3 juegan la versión publicada una vez cada uno.

## Pitch (martes 20 de octubre, 23:59)

- [ ] Video de 3 a 5 min en YouTube (no listado) registrado en la Plataforma Oficial. Datos de Nicole: `docs/PITCH_NICOLE.md`.
