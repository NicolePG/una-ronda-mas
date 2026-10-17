# Pitch — presupuesto, competidores y cronograma (Nicole, Fase 5)

Borrador para el video del martes 20 de octubre. Los montos son **estimaciones en dólares (USD)** para seguir el juego después de la jam; ajusten las tarifas con las suyas antes de grabar.

**Supuestos**

- Equipo actual de 3 personas (programación, gameplay/arte 3D, UI/audio).
- Unity Personal es gratis mientras el estudio tenga menos de USD 200.000 de ingresos al año; el resto de herramientas son gratuitas (ver `docs/GDD.md`).
- Tarifas de referencia: medio tiempo USD 350/mes, tiempo completo USD 700–800/mes por persona.
- Cada escenario suma un 10% de imprevistos.

## Presupuesto en 3 escenarios

### 1. Mínimo viable — 3 meses, PC (itch.io + Steam)

| Concepto | Cálculo | USD |
| --- | --- | ---: |
| Equipo (3 personas, medio tiempo) | 3 × 350 × 3 meses | 3.150 |
| Música y efectos (licencias o freelance) | | 300 |
| Arte para la tienda (cápsula, íconos) | | 200 |
| Steam Direct (por juego) | | 100 |
| Pruebas con jugadores | | 100 |
| Imprevistos 10% | | 385 |
| **Total** | | **4.235** |

### 2. Ideal — 6 meses, PC (Steam) + Android

| Concepto | Cálculo | USD |
| --- | --- | ---: |
| Equipo (3 personas, tiempo completo) | 3 × 700 × 6 meses | 12.600 |
| Artista 3D freelance (concursantes y estudio) | | 2.000 |
| Música original y efectos | | 1.200 |
| Tráiler y marketing (Steam Next Fest, anuncios) | | 1.500 |
| Traducción a inglés y portugués | | 600 |
| Celulares de prueba (2 de gama baja) | | 400 |
| Google Play (pago único) + Steam Direct | 25 + 100 | 125 |
| Imprevistos 10% | | 1.843 |
| **Total** | | **20.268** |

### 3. Máximo — 12 meses, PC + Android + iOS, modo fiesta y consolas con un socio

| Concepto | Cálculo | USD |
| --- | --- | ---: |
| Equipo (5 personas, tiempo completo) | 5 × 800 × 12 meses | 48.000 |
| Voz de Tito (actor de doblaje), música y audio | | 4.000 |
| Servidores para el modo fiesta (1 año) | | 1.200 |
| Marketing, ferias e influencers | | 6.000 |
| Port a consola con un socio externo | | 10.000 |
| Legal y registro de marca | | 800 |
| Cuentas de tiendas (Apple 99/año + Google 25 + Steam 100) | | 224 |
| Equipos (Mac para iOS, celulares) | | 1.500 |
| Imprevistos 10% | | 7.172 |
| **Total** | | **78.896** |

## Competidores

| Juego | Qué es | Parecido | Nuestra diferencia |
| --- | --- | --- | --- |
| **Fall Guys** (Mediatonic / Epic Games, 2020; gratis desde 2022) | Battle royale online de obstáculos con estética de programa de TV | Concurso de obstáculos, caídas graciosas | Un jugador, rondas de 40 s, reintento inmediato y un presentador que hace trampa |
| **Stumble Guys** (Kitka Games, hoy de Scopely) | El estilo Fall Guys en celulares | Público casual y móvil | Sin depender de partidas online; la sorpresa viene de las reglas que cambian |
| **Subway Surfers** (SYBO, 2012) y **Temple Run** (Imangi, 2011) | Runners infinitos de 3 carriles | Mismos controles: carril, salto, deslizarse | Rondas con meta, premios que se acumulan (roguelite) y reglas nuevas en cada ronda |
| **Wipeout** (juegos del programa de TV, Activision) | Licencia del concurso de obstáculos | El agua y los inflables | Propiedad original, con un giro narrativo |
| **The Stanley Parable** (2013) | Narrador que no es confiable | «Algo no está bien»: la voz que guía miente | Llevamos esa idea a un arcade de 40 segundos: descubrir la mentira es una mecánica («¡Trampa!») |

**En una frase:** *A TV game show obstacle run where the host cheats — and never lets you leave.* Nadie mezcla el runner de carriles con un presentador que miente y que el jugador puede acusar.

## Cronograma

### Durante la jam

| Cuándo (hora de Bolivia) | Hito |
| --- | --- |
| Sáb 17, 20:00 | Fase 0: proyecto en GitHub y build WebGL vacía |
| Dom 18, 01:00 | Fase 1: una ronda se puede ganar o perder |
| Dom 18, 13:00 | Fase 2: juego completo (premios, reglas, Tito, menús) |
| Dom 18, 17:00 | Fase 3: pulido; cierre de funciones |
| Dom 18, 21:00 | Fase 4: build subida a itch.io y a la Plataforma Oficial |
| Mar 20, 23:59 | Fase 5: video del pitch |

### Después de la jam (escenario ideal, 6 meses)

| Mes | Hito |
| --- | --- |
| 1 | Ordenar el feedback de la jam, balance, 24 tramos, pelota rodante y rodillo |
| 2 | Arte y audio finales, 8 premios y 8 reglas, soporte de mando |
| 3 | Página de Steam «Próximamente», tráiler y demo gratis para juntar wishlists |
| 4 | Steam Next Fest con la demo y pruebas con jugadores |
| 5 | Versión Android y traducciones |
| 6 | Lanzamiento en PC y Android |
