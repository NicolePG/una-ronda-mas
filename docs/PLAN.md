# Plan de trabajo — ¡Una Ronda Más!

El equipo empieza a programar el **sábado 17 de octubre a las ~19:00** y la build final debe estar subida el **domingo 18 a las 21:00** (plazo oficial 23:59, hora de Bolivia). Son unas 18 horas de trabajo por persona descontando el descanso. Cada fase tiene un objetivo, tareas por integrante y una condición de «listo». Si una fase se atrasa, se aplica la lista de **Recortes** del final antes de seguir.

| Fase | Objetivo | Hasta |
| --- | --- | --- |
| 0. Preparación | Proyecto Unity compartido en GitHub y build WebGL vacía funcionando | Sábado 20:00 |
| 1. Ronda jugable | Una ronda de 40 s se puede ganar o perder | Domingo 01:00 |
| 2. Juego completo | Rondas, premios, reglas, Tito, menús y guardado | Domingo 13:00 |
| 3. Pulido | Audio, efectos, animaciones, balance y 12 tramos. **Cierre de funciones a las 17:00** | Domingo 17:00 |
| 4. Entrega | Build final probada en otra PC y subida | Domingo 21:00 |
| 5. Pitch | Video de 3 a 5 min con subtítulos en inglés | Martes 23:59 |

## Fase 0 — Preparación (hasta sábado 20:00)

**Nicole crea el proyecto; Otoniel y Tania instalan en paralelo.**

1. Los 3: en Unity Hub, instalar **Unity 2022.3.62f2** con el módulo **WebGL Build Support**. Misma versión exacta en las 3 computadoras.
2. Nicole: nuevo proyecto con la plantilla **3D (Built-In Render Pipeline)**, nombre `UnaRondaMas`.
3. Nicole: `Edit → Project Settings → Editor` → Asset Serialization: **Force Text**; Version Control: **Visible Meta Files**.
4. Nicole: copiar `CLAUDE.md`, `.gitignore` y la carpeta `docs/` a la raíz del proyecto (al lado de `Assets/`).
5. Nicole: `Window → Package Manager` → TextMeshPro (importar **TMP Essentials**) y ProBuilder (opcional).
6. Nicole: `File → Build Settings` → **WebGL** → Switch Platform. En Player Settings → Publishing Settings: Compression Format **Gzip** y **Decompression Fallback** activado; resolución 1280 × 720.
7. Nicole: con Claude Code, crear la estructura de carpetas de `CLAUDE.md`, `GameEvents.cs` y `BalanceConfig.cs`.
8. Nicole: crear el repositorio **privado** en GitHub con GitHub Desktop, primer commit e invitar a Otoniel y Tania. Ellos lo clonan y lo abren en Unity.
9. Nicole: build WebGL de prueba con una escena casi vacía y subirla a itch.io como borrador para confirmar que corre en el navegador.

**Listo cuando:** los 3 abren el mismo proyecto desde GitHub sin errores y la build de prueba corre en itch.io.

## Fase 1 — Ronda jugable (hasta domingo 01:00)

**Otoniel**
- [ ] Escena `Game.unity`: cámara, luces, agua, fondo oscuro, niebla y pantalla gigante (vacía) al fondo.
- [ ] Prefab del concursante + `ContestantController`: carriles, salto, deslizamiento, choque, caída al agua e invulnerabilidad.
- [ ] Prefabs de obstáculos: bloque, barra baja, barra alta, martillo (`HammerSwing`) y hueco (`WaterZone`).
- [ ] 4 tramos de 20 m (2 fáciles, 2 medios) con obstáculos y estrellas.

**Nicole**
- [x] `GameManager`: estados, ronda, meta, estrellas, vidas, temporizador de 40 s y velocidad según `BalanceConfig`.
- [x] `TrackGenerator` + `TrackSegment`: crear tramos adelante, moverlos, destruirlos atrás; arco de meta a tiempo.
- [x] `Star` (normal +1, dorada +3) y malla de estrella (`StarMeshBuilder`).
- [x] `SaveManager` con récord y partidas jugadas.

**Tania**
- [ ] Fuente TextMeshPro en modo Dynamic (tildes, ñ, ¡, ¿, «») y estilo de botones como prefab.
- [ ] `HUD`: estrellas/meta, tiempo, vidas, ronda, regla activa y aviso «¡Trampa!».
- [ ] Pantalla `Eliminado` con «¿Una ronda más?» (reinicia la ronda 1) y botón Menú.
- [ ] Escena `MainMenu.unity` básica: Jugar y récord.

**Listo cuando:** desde el menú se entra al juego, se corre una ronda de 40 s esquivando y juntando estrellas, y se gana (pasa a la ronda 2) o se pierde (pantalla Eliminado → «¿Una ronda más?» funciona).

## Fase 2 — Juego completo (hasta domingo 13:00)

**Otoniel**
- [ ] `RuleEffects`: apagón, espejo, gigante y sin salto.
- [ ] Prefab de holograma (material Fade, parpadeo, `isFake = true`).
- [ ] 4 tramos más (2 medios, 2 difíciles).

**Nicole**
- [x] Progresión de rondas 1 a 10 según la tabla de `docs/GDD.md` (meta, velocidad, obstáculos por ronda).
- [x] Premios: `PrizeData` (ScriptableObjects), `PlayerModifiers`, 6 premios y 4 premios envenenados.
- [x] `RuleManager`: 5 reglas, mentira de Tito (30% desde la ronda 4), hologramas en la pista y botón «¡Trampa!» (tecla T, una vez por ronda).
- [x] Apuesta de Tito: meta ×1,5 y 2 premios si gana.
- [x] Generador sin caminos imposibles (Sin salto / Gigante).

**Tania**
- [ ] `HostBanner`: anuncio de ronda, meta, regla y apuesta (aceptar/rechazar).
- [ ] `HostFace`: cara de Tito en la pantalla gigante con 5 expresiones (sonrisa, guiño, risa tramposa, temblor al mentir, peluquín volando).
- [ ] `HostLines.csv` con 25 a 35 frases por momento y `HostLines` para leerlas.
- [ ] Pantalla de Premios (3 tarjetas, teclas 1-2-3), Pausa (Esc), Cómo jugar, Opciones y Créditos.
- [ ] Menú que cambia según partidas jugadas («¿Una ronda más?» desde la segunda partida).

**Listo cuando:** se puede jugar una partida completa de varias rondas con premios, reglas, mentiras, acusación, apuesta, Tito hablando, menús y récord guardado.

## Fase 3 — Pulido (hasta domingo 17:00, cierre de funciones)

**Otoniel**
- [ ] Animaciones por código: balanceo al correr, estirarse al saltar, aplastarse al deslizarse, girar al chocar.
- [ ] Partículas (salpicadura, confeti en la meta, destello de estrella) y `CameraShake`.
- [ ] 4 tramos más (hasta 12) y ajuste de la sensación de juego.

**Nicole**
- [ ] Balance: ajustar `Balance.asset` jugando las rondas 1 a 6. (Herramienta lista: **Una Ronda Más → Pruebas → Informe de balance** → `docs/BALANCE.md`; falta jugar con los tramos de Otoniel.)
- [ ] Ronda de pruebas con registro de bugs (los 3 juegan 15 minutos). Plantilla: `docs/BUGS.md`.

**Tania**
- [ ] `AudioManager`: música, efectos y volúmenes desde Opciones.
- [ ] Transiciones simples entre pantallas y botones que crecen al pasar el mouse.
- [ ] Página de itch.io: portada, capturas y descripción en español e inglés.

**Listo cuando:** el MVP completo funciona y suena; desde las 17:00 solo se corrigen bugs.

## Fase 4 — Entrega (hasta domingo 21:00)

- [ ] Nicole: build WebGL final y prueba en **otra computadora** y otro navegador. (`tools/build-webgl.ps1` arma la build y el zip; pasos en `docs/ENTREGA.md`.)
- [ ] Nicole: subir a itch.io (zip con `index.html`, marcado «This file will be played in the browser»), unirse a la jam de GJ+ y enviar.
- [ ] Nicole: registrar la build en la Plataforma Oficial de GJ+.
- [ ] Los 3: jugar la versión publicada una vez cada uno.

## Fase 5 — Pitch (lunes y martes, plazo martes 23:59)

- [ ] Otoniel: grabar gameplay con OBS y editar el video.
- [x] Nicole: presupuesto en 3 escenarios (mínimo viable, ideal, máximo), competidores y cronograma. Borrador en `docs/PITCH_NICOLE.md` (revisar montos).
- [ ] Tania: guion, diapositivas, subtítulos en inglés y diapositiva final de contactos.
- [ ] Los 3: video de 3 a 5 minutos con gameplay real, subido a YouTube (no listado) y registrado en la Plataforma Oficial.

## Recortes si vamos atrasados

Aplicar en este orden, solo lo necesario:

1. 12 tramos → 8 (3 fáciles, 3 medios, 2 difíciles).
2. Animaciones → solo aplastar y estirar por código.
3. Premios envenenados 4 → 2.
4. Reglas 5 → 4 (quitar Gigante).
5. Opciones → solo volumen.
6. Menú que cambia → solo el texto del botón.

**Nunca se recorta:** el ciclo de rondas, la mentira de Tito con «¡Trampa!», los premios, el botón «¿Una ronda más?» y la build WebGL.

## Cómo pedirle cosas a Claude Code

Pedidos cortos, de una tarea a la vez, nombrando la tarea de este plan. Ejemplos:

- Nicole: «Soy Nicole. Haz la Fase 0, paso 7: crea las carpetas, `GameEvents.cs` y `BalanceConfig.cs`, y dime cómo crear `Balance.asset`.»
- Otoniel: «Soy Otoniel. Haz el `ContestantController` de la Fase 1 y dime paso a paso cómo armar el prefab del concursante en el Editor.»
- Tania: «Soy Tania. Haz el `HUD` de la Fase 1 escuchando `GameEvents` y dime cómo armar el Canvas.»
- Para errores: «Me sale este error en la Consola de Unity: [pegar el error]. Arréglalo.»
