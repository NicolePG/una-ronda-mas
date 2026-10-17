# ¡Una Ronda Más! — contexto del proyecto para Claude Code

Juego para la **GameJamPlus 26/27** (jam de 48 h). Es un **concurso de obstáculos de TV en 3D**: el concursante corre por una pista inflable de 3 carriles sobre el agua durante rondas de 40 s, junta estrellas y esquiva obstáculos, mientras el presentador **Tito Trampa** cambia las reglas y a veces miente. Al perder, el botón principal es **«¿Una ronda más?»**.
Temas de la jam que cumple: **One More Time** (base) y **Something's Off** (el giro).

- Diseño completo del juego: `docs/GDD.md` (léelo antes de implementar una mecánica nueva).
- Plan por fases, dueños y orden de trabajo: `docs/PLAN.md`.
- Imágenes de referencia (personajes, tramos, pantalla de juego, menús): `docs/img/*.png`.

## Fechas (hora de Bolivia, UTC−4)

- Build final subida a itch.io y a la Plataforma Oficial: **domingo 18 de octubre de 2026, 23:59** (meta interna: 21:00).
- Pitch en video: **martes 20 de octubre de 2026, 23:59**.
- Puntos de control: ver `docs/PLAN.md`. Cierre de funciones: domingo 17:00; después solo pulido y bugs.

## Equipo y dueños

| Integrante | Área | Es dueña/o de |
| --- | --- | --- |
| Otoniel | Jugabilidad, pista y animación | Escena `Game.unity`, concursante, obstáculos, tramos, efectos visuales de reglas, partículas, cámara |
| Nicole | Sistemas y lógica en código | `Scripts/Core`, `Scripts/Data`, generador de pista, premios, reglas, apuesta, guardado, build WebGL |
| Tania | Menús, interfaz, presentador y audio | Escena `MainMenu.unity`, todos los prefabs de UI, cara y frases de Tito, `AudioManager`, página de itch.io |

## Stack técnico

- **Unity 2022.3.62f2 LTS**. Los 3 usan exactamente esta versión (ver `ProjectSettings/ProjectVersion.txt`).
- Plantilla **3D (Built-in Render Pipeline)**, shader Standard. Nada de URP/HDRP.
- **C#**. Input Manager clásico (`Input.GetKeyDown`), no el paquete Input System.
- UI con **uGUI + TextMeshPro**. ProBuilder opcional (paquete gratis de Unity).
- Plataforma objetivo: **WebGL**, publicado en itch.io para jugar en el navegador.
- Solo paquetes oficiales gratuitos de Unity. No agregar paquetes de terceros ni del Asset Store sin que el equipo lo pida.

## Reglas que no se rompen

1. **Todo el código se escribe durante la jam.** No traer, pegar ni adaptar código de proyectos anteriores ni de repositorios externos: el reglamento descalifica cualquier desarrollo previo.
2. Assets externos solo con licencia clara (CC0 preferido; CC-BY con crédito en la pantalla de Créditos).
3. **Debe funcionar en WebGL:** nada de `System.Threading`, `System.IO` para guardar ni sockets. Guardar con `PlayerPrefs` + `PlayerPrefs.Save()`. `Application.Quit()` no existe en web: ocultar el botón Salir con `#if UNITY_WEBGL`.
4. **Primero el MVP** (`docs/GDD.md`, sección Alcance). No empezar nada marcado como Extra hasta que el MVP funcione completo.
5. Siempre debe existir un camino posible en la pista (ver Generador de pista).

## Cómo trabajar en este repo (instrucciones para Claude)

- Responde en **español**, corto y directo. Nombres de clases, variables y archivos en **inglés**; comentarios breves en español.
- Al empezar una sesión, pregunta **quién está trabajando** (Otoniel, Nicole o Tania) y en qué tarea de `docs/PLAN.md`. Prioriza esa área y **no modifiques archivos de otra área** sin avisar primero.
- **No edites a mano** archivos `.unity`, `.prefab`, `.asset`, `.mat` ni `.meta`. En su lugar:
  - escribe los scripts C# y luego da **pasos exactos en el Editor** (qué GameObject crear, qué componente agregar, qué campos asignar en el Inspector, qué tag o layer usar);
  - para trabajo repetitivo (materiales, prefabs base, ScriptableObjects de datos), crea **scripts de Editor** en `Assets/_Project/Editor/` con `[MenuItem("Una Ronda Más/...")]` que los generen.
- Unity crea los `.meta` solo; nunca los generes tú.
- Todos los números de diseño (tiempos, metas, velocidades, probabilidades) viven en **`BalanceConfig`** (ScriptableObject). Nada de números mágicos en el código.
- Campos con `[SerializeField] private`, agrupados con `[Header]` y con `[Tooltip]` cuando no sean obvios.
- Eventos: usa `GameEvents` (abajo). Suscribirse en `OnEnable` y desuscribirse en `OnDisable` siempre (los eventos estáticos sobreviven al recargar escenas).
- Pausa con `Time.timeScale = 0`; la UI y las animaciones de menú usan `Time.unscaledDeltaTime`.
- Después de cada cambio, di **qué probar en Play Mode** y pide que te peguen los errores de la Consola si aparecen.
- Git: commits pequeños con mensaje en español (`feat: ...`, `fix: ...`, `chore: ...`). Nunca hagas push, force push ni cambios de rama sin que te lo pidan.

## Estructura de carpetas

```
Assets/_Project/
├── Scenes/      MainMenu.unity (Tania), Game.unity (Otoniel)
├── Scripts/
│   ├── Core/      GameEvents.cs, GameManager.cs, RuleManager.cs, SaveManager.cs, PrizeOfferBuilder.cs, AudioManager.cs
│   ├── DevTools/  DebugOverlay.cs, DebugContestant.cs (solo para probar)
│   ├── Data/      BalanceConfig.cs, PrizeData.cs, RuleData.cs, PlayerModifiers.cs, GameStats.cs
│   ├── Gameplay/  ContestantController.cs, TrackGenerator.cs, TrackSegment.cs, Obstacle.cs,
│   │              Star.cs, WaterZone.cs, HammerSwing.cs, Hologram.cs, FinishArch.cs, StarMeshBuilder.cs
│   ├── Effects/   RuleEffects.cs, CameraShake.cs
│   └── UI/        MainMenuUI.cs, HowToPlayUI.cs, OptionsUI.cs, CreditsUI.cs, HUD.cs, HostBanner.cs,
│                  HostFace.cs, HostLines.cs, PrizeScreen.cs, PauseMenu.cs, GameOverScreen.cs
├── Editor/      herramientas del menú «Una Ronda Más/...»
├── Prefabs/     Gameplay/, Obstacles/, Segments/ (Easy_01..04, Mid_01..04, Hard_01..04), UI/
├── Data/        Balance.asset, Prizes/*.asset, Rules/*.asset
├── Resources/   HostLines.csv
├── Materials/   Mat_Red, Mat_Gold, Mat_StarLight, Mat_Player, Mat_Water, Mat_Hologram, Mat_Track, Mat_Grey
└── Audio/, Fonts/, Art/
```

## Sistemas

| Script | Dueño | Qué hace |
| --- | --- | --- |
| `GameManager` | Nicole | Máquina de estados de la partida; ronda, estrellas, meta, vidas, tiempo, velocidad, apuesta, premios elegidos (`PlayerModifiers`) |
| `RuleManager` | Nicole | Elige la regla de cada ronda, decide si Tito miente, resuelve el botón «¡Trampa!» |
| `SaveManager` (estática) | Nicole | `PlayerPrefs`: récord de ronda, récord de estrellas, partidas jugadas, volúmenes |
| `TrackGenerator` | Nicole | Crea tramos adelante según la dificultad de la ronda, los mueve hacia el jugador y los borra al pasar |
| `ContestantController` | Otoniel | Carriles, salto, deslizamiento, choques, caída al agua, invulnerabilidad |
| `RuleEffects` | Otoniel | Efectos visibles de cada regla aplicada (apagón, espejo, gigante, sin salto) |
| `AudioManager` | Tania | Música y efectos; persiste entre escenas (`DontDestroyOnLoad`); se crea solo si falta |
| `HostBanner`, `HostFace`, `HostLines` | Tania | Anuncio de ronda/regla/apuesta, cara de Tito con expresiones, frases desde CSV |

## Contrato de eventos (`GameEvents.cs`)

Acordado por el equipo. Si hace falta un evento nuevo, agrégalo aquí y en el código, y avisa.

```csharp
public enum HitCause { Obstacle, Water }
public enum RoundPhase { Announcement, Running, PrizeSelection, GameOver }

public static class GameEvents
{
    public static event Action<RoundPhase> PhaseChanged;
    public static event Action<int, int, float> RoundStarted;    // ronda, meta, velocidad (m/s)
    public static event Action<int, int> StarsChanged;           // estrellas de la ronda, meta
    public static event Action<int> LivesChanged;                // vidas actuales
    public static event Action<float> TimeChanged;               // segundos restantes
    public static event Action<HitCause> ContestantHit;
    public static event Action<int, int> RoundWon;               // ronda, cantidad de premios a elegir
    public static event Action<GameStats> GameOver;              // ronda alcanzada, estrellas totales, récords
    public static event Action<RuleData, bool> RuleAnnounced;    // regla anunciada (null en rondas sin regla), ¿es mentira?
    public static event Action<RuleData> RuleApplied;            // regla que realmente se aplica (null en rondas sin regla)
    public static event Action<bool> BetResolved;                // true = aceptó la apuesta
    public static event Action<bool> CheatCalled;               // true = acertó
    public static event Action<PrizeData> PrizeChosen;
    public static event Action<bool> PauseToggled;
    // Cada evento tiene su método Raise...(): GameEvents.RaiseStarsChanged(stars, goal);
}
```

## Flujo de una ronda

`Announcement` (Tito anuncia ronda, meta y regla; desde la ronda 3 ofrece la apuesta) → `Running` (40 s) →
- llegó a la meta con las estrellas: `PrizeSelection` (1 premio, 2 si ganó la apuesta) → siguiente `Announcement`;
- sin estrellas suficientes o sin vidas: `GameOver` → «¿Una ronda más?» reinicia en la ronda 1 sin pasar por el menú.

El **temporizador manda**: la ronda termina a los 40 s exactos; el arco de meta es solo visual y se crea para que llegue al jugador justo a tiempo.

## Cómo usar los sistemas de Nicole (ya implementados)

- `GameSystems.prefab` (`Prefabs/Gameplay`) trae `GameManager` + `RuleManager` + `TrackGenerator`. Va **una vez** en `Game.unity`, en (0, 0, 0). Si se agregan tramos nuevos: menú **Una Ronda Más/Prefabs/Crear o actualizar GameSystems**.
- Cuando llega cualquier evento, las propiedades de `GameManager.Instance` ya están actualizadas. `PhaseChanged` siempre se lanza al final.
- **Anuncio:** `RuleAnnounced` → `PhaseChanged(Announcement)`. Leer `CurrentRound`, `BaseGoal`, `BetGoal`, `IsBetOffered`, `Rules.AnnouncedRule`. Sin apuesta (rondas 1–2) empieza sola a los `AnnouncementDuration` s; con apuesta espera a que la UI llame `AcceptBet()` o `DeclineBet()` (o `ConfirmAnnouncement(bool)`).
- **Carrera:** `BetResolved` (solo si hubo apuesta) → `RuleApplied` → `RoundStarted` → `PhaseChanged(Running)`.
- **Fin:** `RoundWon` → `PhaseChanged(PrizeSelection)`: la UI muestra `PrizeOffers` (3 `PrizeData`, `IsPoisoned` para el brillo raro) y llama `ChoosePrize(0..2)`; con apuesta ganada se eligen 2 (`PrizePicksRemaining`, `IsPrizeTaken(i)`). O bien `GameOver(stats)` → `PhaseChanged(GameOver)`; `LastStats` también tiene los datos.
- **Concursante (Otoniel):** al chocar lanza `GameEvents.RaiseContestantHit(HitCause.Obstacle o Water)`; GameManager aplica Escudo/Flotador y quita la vida. Lee `GameManager.Instance.Modifiers` (`JumpHeightMultiplier`, `JumpDurationMultiplier`, `LaneChangeDurationMultiplier`, `HasPoisonSpring`, `HasMagnet`), `Rules.IsApplied(RuleType.Mirror/Giant/NoJump/Blackout)` y los números de la sección «Concursante» de `Balance`. Tag **Player**.
- **Estrellas:** prefabs `Star_Normal` y `Star_Golden` (`Prefabs/Gameplay`). Se ponen dentro de los tramos; llaman solas a `GameManager.Instance.CollectStar(esDorada)`.
- **Tramos (Otoniel):** raíz con `TrackSegment` (pivote al **inicio**, 20 m hacia +Z, primeros y últimos 3 m sin obstáculos), marcar dificultad, obstáculos que tiene y `requiresJump` / `requiresSlide`; `hologramSlots` = puntos vacíos donde puede salir un holograma. Guardar en `Prefabs/Segments`; el que tenga «Safe» en el nombre es el tramo vacío. Revisar con **Una Ronda Más/Pruebas/Validar tramos**. Arco: `Prefabs/Gameplay/FinishArch.prefab`; holograma: un prefab con «Holo» en el nombre en `Prefabs/Obstacles`.
- **Pausa:** Esc o P la maneja GameManager. La UI de pausa escucha `PauseToggled` y llama `SetPaused(false)`, `RestartGame()` o `GoToMainMenu()`.
- **«¡Trampa!»:** tecla T (RuleManager) o `GameManager.Instance.Rules.CallCheat()` desde un botón; `Rules.CanCallCheat` para el aviso del HUD.
- **Guardado (Tania):** `SaveManager.BestRound`, `BestStars`, `GamesPlayed` (≥ 1 → el menú cambia), `MusicVolume`, `SfxVolume`.
- **Pruebas:** `Scenes/Test_Nicole.unity` + `DebugOverlay` (panel F1 y atajos). Quitar `DebugOverlay` o desactivar «handleKeys» cuando la UI use las teclas 1-2-3.

## Convenciones de gameplay

- 1 unidad = 1 metro. Concursante de 1,6 m; carriles en **x = −2, 0, 2**; tramos de **20 m**.
- **El concursante nunca avanza**: queda en z = 0 y la pista se mueve hacia −Z a la velocidad de la ronda. Los tramos se crean hasta ~100 m adelante y se destruyen a z < −20.
- Cámara fija en (0, 4, −7) mirando hacia adelante (~15° hacia abajo). Niebla del color del fondo para ocultar los tramos que aparecen.
- Pantalla gigante de Tito **fija en el mundo** al fondo (z ≈ 110, y ≈ 10), con un Canvas en World Space. No se mueve con la pista.
- Física: concursante con `CapsuleCollider` + `Rigidbody` cinemático; obstáculos, estrellas y agua son **triggers** con su componente (`Obstacle`, `Star`, `WaterZone`). Raíz de cada tramo con `Rigidbody` cinemático.
- Salto y deslizamiento por código (sin gravedad de física). Deslizarse baja la cápsula a ~0,7 m durante 0,6 s.
- Caer al agua solo cuenta si el concursante está en el suelo dentro de un `WaterZone`.
- Holograma = obstáculo con `isFake = true`: se ve, pero no hace daño.
- Generador de pista: con la regla **Sin salto** no pone barras bajas que ocupen los 3 carriles; con **Gigante** no pone barras altas que ocupen los 3 carriles.
- Luz ambiental en modo Color (Lighting → Environment) para poder bajarla por código en la regla Apagón (`RenderSettings.ambientLight`).

## Paleta de colores

| Uso | Color |
| --- | --- |
| Peligro (obstáculos) | `#D13B3B` |
| Puntos (estrella dorada) | `#F7B11D` |
| Estrella normal | `#FBD97A` |
| Concursante | `#2A78D4` |
| Agua | `#4FA3E0` semitransparente |
| Pista | `#E9E6DF` |
| Fondo del estudio | `#24193F` (morado noche) |
| Textos | `#FFFFFF` |

Rojo siempre es peligro, dorado siempre son puntos, azul es el jugador o el agua.
