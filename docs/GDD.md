# ¡Una Ronda Más! — Diseño del juego y reparto de tareas


**¡Una Ronda Más!** es un concurso de obstáculos de TV en 3D: el concursante corre por una pista inflable sobre el agua, junta estrellas y esquiva trampas mientras el presentador, Tito Trampa, hace trampa. Cumple dos temas de la GameJamPlus 26/27, One More Time y Something's Off, y se hace con Unity 2022.3 LTS (C#) y herramientas gratuitas.

## La idea en 30 segundos

Es un concurso de obstáculos de televisión: corres 40 segundos por una pista inflable sobre una piscina, juntas estrellas y esquivas trampas, y el presentador hace trampa para que pierdas.

- **Para cualquier persona:** «Eres concursante en un programa de TV. Corres por una pista llena de obstáculos inflables; si chocas o te caes al agua, pierdes una vida. El presentador, Tito Trampa, cambia las reglas cada ronda y a veces miente.»
- **Para el jurado:** runner roguelite de rondas cortas ambientado en un concurso de TV. La pista se arma al azar en cada intento, cada victoria da un premio y cada derrota lleva a un solo botón: «¿Una ronda más?».
- **En inglés, para el pitch:** *A TV game show obstacle run where the host cheats — and never lets you leave.*

**Por qué engancha incluso a quien no juega**

- Todos conocen los concursos de obstáculos de TV, y caerse al agua da risa en lugar de frustrar.
- Se juega con tres acciones: cambiar de carril, saltar y deslizarse.
- Las rondas duran 40 segundos y reintentar es inmediato.
- El presentador tramposo da curiosidad por ver qué inventa después.

**Cómo cumple los temas**

- **One More Time:** pista distinta en cada intento, premios que se acumulan, récord guardado y reinicio con un clic.
- **Something's Off:** el presentador miente sobre las reglas, pone obstáculos falsos y el menú cambia según cuántas veces jugaste.

## Lo que lo hace divertido

Además de esquivar, el jugador toma una decisión importante en cada ronda; ahí están la diversión y el diferencial del juego.

| Mecánica | Cómo funciona | Por qué divierte |
| --- | --- | --- |
| Botón «¡Trampa!» | Si crees que Tito hizo trampa, presionas T. Si aciertas, la trampa se deshace, a Tito se le cae el peluquín y ganas 4 + 2 × ronda estrellas (×1,5, ×2… con la racha de detective). Si te equivocas pierdes 1 vida y el botón se bloquea 2,5 s. Se puede acusar varias veces por ronda. | El jugador observa, sospecha y arriesga; pillar a Tito ayuda a llegar a la meta |
| Trampas en vivo | Desde la ronda 2, a mitad de la carrera Tito se roba estrellas, infla la meta (ronda 3) o cruza los controles (ronda 5). Hay 6 s para acusarlo; si no, la trampa queda hecha. Desde la ronda 4 también hace faroles: guiña y distrae igual, pero no cambia nada | Hay que mirar el marcador y no solo la pista: ¿trampa o farol? |
| Premios envenenados | Desde la ronda 4, uno de los 3 premios puede ser un regalo de Tito con un efecto oculto que se revela al elegirlo | Elegir premio deja de ser obvio |
| La apuesta de Tito | Antes de cada ronda puedes apostar: la meta sube 50%, pero si ganas las 3 tarjetas de premio son raras o épicas | Riesgo y recompensa en cada ronda |
| Obstáculos falsos | Cuando Tito miente, algunos obstáculos son hologramas que se pueden atravesar | La sorpresa de «Espera, ¿qué?» |
| Caídas al agua | Al caer, el concursante salpica, Tito se ríe y el jugador reaparece en la pista | Perder da risa en lugar de frustrar |

## Cómo se juega

El concursante corre solo hacia adelante; el jugador elige el carril, salta y se desliza para esquivar obstáculos y juntar estrellas antes de llegar a la meta.

**Controles**

| Acción | Teclado | Mando (extra) |
| --- | --- | --- |
| Cambiar de carril | A / D o flechas izquierda y derecha | Stick izquierdo |
| Saltar | Espacio o W | Botón A |
| Deslizarse | S o flecha abajo | Botón B |
| Acusar a Tito («¡Trampa!») | T | Botón Y |
| Pausa | Esc | Start |
| Elegir premio o apuesta | Clic o teclas 1, 2 y 3 | Cruceta y botón A |

**Reglas base**

- La pista tiene 3 carriles sobre el agua y cada ronda dura 40 segundos hasta la meta.
- La meta de estrellas empieza en 34 y sube 12 por ronda (34, 46, 58…): hay que juntar ~1 de cada 4 estrellas en la ronda 1 y ~6 de cada 10 en la ronda 8. Antes (20 y +8) las rondas 1 a 5 eran demasiado fáciles.
- Estrella normal: 1 punto. Estrella dorada: 3 puntos; suele estar en lugares arriesgados.
- El jugador empieza con 3 vidas. Chocar con un obstáculo o caer al agua quita 1 vida; el concursante reaparece en el carril central con 1 segundo de invulnerabilidad.
- Si llega a la meta sin las estrellas necesarias o se queda sin vidas, queda eliminado.

**Una partida, paso a paso**

1. Tito anuncia la ronda y, desde la ronda 3, una regla especial. El jugador decide si acepta la apuesta.
2. El concursante corre 40 segundos: cambia de carril, salta, se desliza y junta estrellas.
3. Si cree que Tito mintió, el jugador lo acusa con «¡Trampa!».
4. Si llega a la meta con las estrellas necesarias, elige 1 de 3 premios (raros o épicos si ganó la apuesta) y pasa a una ronda más rápida.
5. Si queda eliminado, ve la ronda alcanzada, sus estrellas totales y el récord, con el botón «¿Una ronda más?».

El puntaje final es la suma de estrellas de todas las rondas. Todos los números son el punto de partida y se ajustan en el balance del domingo.

## Ronda por ronda

Cada ronda aprieta un poco más: la pista va 7,5% más rápido, la meta sube 12 estrellas, hay más tramos medios y difíciles y Tito hace más trampas. La ronda 2 ya trae regla y la primera trampa en vivo; desde la ronda 3 Tito puede mentir con la regla.

| Ronda | Meta | Velocidad | Tramos (fácil / medio / difícil) | Obstáculos nuevos | Tito |
| --- | --- | --- | --- | --- | --- |
| 1 | 34 | 8,5 m/s | 100 / 0 / 0 | Bloques y barras bajas | Explica cómo se juega |
| 2 | 46 | 9,1 m/s | 60 / 40 / 0 | Barras altas | Primera regla (verdadera); 1 trampa: robo de estrellas |
| 3 | 58 | 9,8 m/s | 35 / 55 / 10 | Martillo y huecos al agua | Puede mentir (35%); 1 trampa; ya puede inflar la meta |
| 4 | 70 | 10,6 m/s | 15 / 60 / 25 | Pelota rodante | Mentira 40%; 2 trampas; faroles; premios envenenados |
| 5 | 82 | 11,4 m/s | 0 / 55 / 45 | — | Mentira 45%; 2 trampas; ya puede cruzar los controles |
| 6 | 94 | 12,2 m/s | 0 / 55 / 45 | Rodillo barredor | Mentira 50%; 3 trampas |
| 7 | 106 | 13,1 m/s | 0 / 30 / 70 | — | Mentira 55%; 3 trampas |
| 8 | 118 | 14,1 m/s | 0 / 30 / 70 | — | Mentira 60%; 3 trampas |
| 9 | 130 | 15,2 m/s | 0 / 10 / 90 | — | Mentira 65%; 3 trampas |
| 10 | 142 | 16,3 m/s | 0 / 10 / 90 | — | Mentira 70% (tope) |

Desde la ronda 11 la velocidad se queda en 17 m/s y solo sube la meta. Los faroles empiezan en 25% de las trampas y llegan a 45%. Todos los números están en `Balance.asset` («Crear todo» lo pone al día).

**Ejemplo: la ronda 3**

1. Tito aparece en la pantalla gigante: «¡Ronda 3! Meta: 58 estrellas. Y hoy… ¡todo vale doble!». El cartel avisa: «Tito hará hasta 1 trampa».
2. Tito ofrece su apuesta; el jugador la rechaza para ir seguro.
3. Se apagan las luces: la regla real era «Apagón». El cartel de la regla tiembla y se pone «¿¿??»; el jugador acusa con T y gana 10 estrellas.
4. A los 20 s Tito señala al público («¡Miren allá!») y el número de la meta se pone en rojo: subió a 65. El jugador acusa otra vez y la meta vuelve a 58 (+10 estrellas, racha x2: +15).
5. Si llega a la meta, elige un premio (ya pueden salir raros) y pasa a la ronda 4, donde empiezan los faroles.

## Contenido del juego

El MVP necesita 5 obstáculos, 6 premios, 4 premios envenenados y 5 reglas; lo marcado como Extra se agrega solo si sobra tiempo.

**Obstáculos** (todos inflables y rojos)

| Obstáculo | Cómo se esquiva | Desde | Nivel |
| --- | --- | --- | --- |
| Bloque inflable | Cambiar de carril | Ronda 1 | MVP |
| Barra baja | Saltar | Ronda 1 | MVP |
| Barra alta | Deslizarse | Ronda 2 | MVP |
| Martillo péndulo | Pasar cuando el martillo está en otro carril | Ronda 3 | MVP |
| Hueco al agua | Saltar o cambiar de carril | Ronda 4 | MVP |
| Pelota rodante | Cambiar de carril; viene de frente | Ronda 5 | Extra |
| Rodillo giratorio | Saltar en el momento justo; barre los 3 carriles | Ronda 6 | Extra |

**Premios** (se elige 1 de 3 al ganar una ronda; los premios se quedan toda la partida)

Cada premio se elige **una sola vez por partida**: lo que ya tienes no vuelve a salir, así cada premiación trae ventajas nuevas. El corazón es la excepción: se repite, pero nunca en dos premiaciones seguidas ni con las vidas llenas. Hay 17 premios, suficientes para tener novedades hasta las rondas 9 y 10, donde ya es muy difícil seguir; si se acaban, salen menos tarjetas.

Cada tarjeta sortea su rareza según la ronda: los comunes salen siempre, los raros desde la ronda 2 (25% y suben 6% por ronda, hasta 60%) y los épicos desde la ronda 5 (12%, +4% por ronda, hasta 35%). Ganar la apuesta hace que las 3 tarjetas sean raras o épicas.

| Premio | Rareza | Efecto |
| --- | --- | --- |
| Corazón extra | Común | +1 vida, máximo 5 (se repite, no seguido) |
| Escudo | Común | Absorbe 1 choque por ronda |
| Flotador | Común | La primera caída al agua de cada ronda no quita vida |
| Propina | Común | Empiezas cada ronda con 5 estrellas (Tito también te las puede robar) |
| Zapatillas | Común | Cambias de carril 30% más rápido |
| Imán | Raro | Atrae las estrellas de los carriles vecinos |
| Resorte | Raro | Salto más alto y más largo |
| Cámara lenta | Raro | La pista va 10% más lenta: más tiempo para esquivar (pasan menos estrellas) |
| Lupa | Raro | Te dice qué trampa hizo Tito, cuándo es un farol y si la regla es falsa |
| Silbato | Raro | La primera falsa alarma de cada ronda no quita vida |
| Caja fuerte | Raro | Tito solo puede robarte o inflarte la meta la mitad |
| Seguro de apuesta | Raro | Si fallas la meta de la apuesta pero llegas a la normal, igual pasas de ronda (con tarjetas normales) |
| Trébol | Épico | Cada estrella dorada vale 3 más |
| Súper imán | Épico | Solo si ya tienes el Imán: atrae de los 3 carriles y desde 20 m |
| Segunda oportunidad | Épico | Una vez por partida, al perder la última vida vuelves con 1 |
| Placa de detective | Épico | Pillar a Tito da el doble de estrellas |
| Contrato amañado | Épico | La meta de cada ronda baja 10% |

**Premios envenenados** (desde la ronda 4, uno de los 3 puede serlo; tiene un brillo raro como pista)

| Parece | En realidad |
| --- | --- |
| Corazón extra | Da la vida, pero la meta de la siguiente ronda sube 5 |
| Resorte | Cada salto es doble y cuesta caer donde quieres |
| Cámara lenta | Hace lento el cambio de carril, no la pista |
| Escudo | Solo dura los primeros 10 segundos de la ronda |

**Reglas de Tito** (una por ronda desde la ronda 2)

| Regla | Qué pasa | Nivel |
| --- | --- | --- |
| Todo vale doble | Las estrellas valen el doble, pero la pista va 30% más rápido | MVP |
| Apagón | La pista se oscurece; solo se ve lo que ilumina el reflector del concursante | MVP |
| Espejo | Izquierda y derecha se invierten | MVP |
| Gigante | El concursante es el doble de grande y no puede deslizarse bajo las barras altas | MVP |
| Sin salto | No se puede saltar en toda la ronda | MVP |
| Estrellas tímidas | Las estrellas cambian de carril cuando te acercas | Extra |
| Lluvia de estrellas | Aparecen el doble de estrellas y el doble de obstáculos | Extra |
| Viento | Un ventilador gigante empuja al concursante hacia un lado | Extra |

## Diseño de personajes y obstáculos (3D)

Todo se arma con formas básicas de Unity (primitivas) y colores fijos, así que no hace falta saber modelar: rojo quita vidas, dorado da puntos y azul es agua.

![Figura 1. Personajes, estrellas y obstáculos](img/lineup.png)

| Diseño | Cómo se arma en Unity | Colores | Animaciones |
| --- | --- | --- | --- |
| Concursante (jugador) | Capsule de 1,6 m como cuerpo, dos Spheres como ojos, una sonrisa, un Cylinder delgado con el número 1 (TextMeshPro) en el pecho y dos Capsules chicas como pies | Azul, número dorado, ojos negros | Corre balanceándose, se encoge al saltar, se aplasta al deslizarse, salpica al caer al agua y salta de alegría en la meta |
| Tito Trampa (villano) | Cara en la pantalla gigante al fondo: Canvas en World Space con Images de círculos y rectángulos, sobre un Cube delgado | Pelo negro (es un peluquín), moño rojo, micrófono dorado | Sonrisa, guiño, risa tramposa, pantalla que tiembla cuando miente y peluquín que sale volando cuando lo descubren |
| Estrellas | Malla de estrella de 5 puntas extruida, generada por script (`StarMeshBuilder`) o hecha con ProBuilder | Normal amarilla clara (+1), dorada grande (+3) | Giran, flotan y destellan al recogerlas |
| Bloque inflable | Cube de 2 × 2 × 2 m (bordes redondeados con ProBuilder, opcional) y un signo «!» con TextMeshPro | Rojo | Se infla y desinfla un poco, como si respirara |
| Barra baja | Cylinder horizontal a 50 cm del piso entre dos postes (Cylinders) | Roja con franjas claras | Vibra al chocarla |
| Barra alta | Cylinder horizontal a 1,2 m del piso entre dos postes (Cylinders) | Roja con franjas claras | Vibra al chocarla |
| Martillo péndulo | Cylinder grande colgado de una viga (Cube), con un pivote vacío que gira por script (`HammerSwing`) de un carril a otro | Rojo, viga gris | Balanceo constante con una onda seno |
| Hueco al agua | Un pedazo de carril sin piso, con agua debajo y un trigger `WaterZone` | Agua azul, borde rojo | El agua ondula; salpicadura al caer |
| Holograma de Tito | Copia de un obstáculo con material Standard en modo Fade (semitransparente) y `isFake = true` | Rojo transparente que parpadea | Tiembla un poco; se atraviesa sin daño |

**Personalidad**

- **Tito Trampa:** presentador encantador y tramposo. Habla como en un concurso de TV («¡Un aplauso para el concursante!»), nunca se enoja y sonríe más cuando el jugador cae al agua.
- **Concursante:** no tiene nombre ni habla; el jugador es el concursante número 1 y se expresa con saltos y gestos.

## La pista (escenario 3D)

La pista es un recorrido inflable de 3 carriles sobre una piscina dentro del estudio de TV; se arma uniendo al azar tramos de 20 m, así cada intento es distinto.

![Figura 2. Tramos de pista: 4 ejemplos vistos desde arriba](img/tramos.png)

- **Tramos:** cada tramo es una escena de 20 m con obstáculos y estrellas ya ubicados. Hay tramos fáciles, medios y difíciles; el generador elige según la ronda.
- **Cantidad:** a 8 m/s, la ronda 1 recorre 320 m en 40 segundos, es decir 16 tramos; la ronda 10 recorre 640 m, 32 tramos.
- **Para el MVP bastan 12 tramos:** 4 fáciles, 4 medios y 4 difíciles. Siempre debe haber un camino posible.
- **Meta:** un arco inflable dorado al final de los 40 segundos, con confeti al cruzarlo.
- **El estudio:** agua azul a los lados, focos de colores, público plano al fondo (extra) y la pantalla gigante de Tito al final de la pista.

**Así se ve el juego**

![Figura 3. Pantalla de juego: cámara detrás del concursante](img/juego.png)

La cámara va detrás y por encima del concursante, mirando hacia adelante; la pantalla de Tito siempre está al fondo, así el jugador lo ve mientras corre.

## El giro: algo no está bien

Lo extraño aparece desde la ronda 2 de la primera partida, porque los jurados suelen jugar solo una o dos veces; después, el juego guarda cuántas partidas jugaste y sigue cambiando.

| Momento | Qué pasa |
| --- | --- |
| Ronda 1 | Todo normal. Tito es amable y explica las reglas. |
| Desde la ronda 2 | Tito anuncia una regla especial en cada ronda y hace su primera trampa en vivo (se roba estrellas). El anuncio avisa cuántas trampas puede hacer. |
| Desde la ronda 3 | Tito puede mentir con la regla (35%, +5% por ronda hasta 70%): anuncia una y aplica otra, y pone hologramas. El cartel muestra la regla anunciada y cada tanto falla («¿¿??»). También puede inflar la meta. Ofrece su apuesta. |
| Desde la ronda 4 | Hace faroles: guiña, señala al público y el público murmura, pero no cambia nada. Desde la ronda 5 también cruza los controles. Como extra, un cartel falso de «¡Atajo!» que lleva al agua. |
| Segunda partida | Tito te reconoce («Ah, volviste») y el botón «Jugar» pasa a decir «¿Una ronda más?». |
| Desde la tercera partida | El título del menú tiembla y Tito comenta tus derrotas anteriores («Ya van 3, ¿eh?»). |
| Ronda 10 (extra) | Se apagan las luces y aparece «FIN DEL PROGRAMA». Segundos después, Tito dice «Mentí. Una ronda más» y empieza la ronda 11. |

- **La broma nunca atrapa al jugador:** en la versión de escritorio, la primera vez que pulsa «Salir», Tito pregunta «¿De verdad te vas?», pero salir siempre funciona.
- **La mentira tiene pistas:** el cartel tiembla y los hologramas parpadean; el jugador atento la descubre y la acusa con «¡Trampa!».
- **Frases de Tito:** entre 25 y 35 frases cortas, agrupadas por momento: inicio de ronda, regla, apuesta, caída al agua, acusación acertada, acusación fallida, victoria y eliminación.

## Menús y pantallas

El juego tiene 9 pantallas; tras perder, el botón principal es «¿Una ronda más?», que reinicia en la ronda 1 sin pasar por el menú.

**Estilo de transmisión de TV:** todas las pantallas usan paneles de vidrio oscuro con borde de neón dorado a rosa, botones píldora con halo al pasar el cursor (el principal es dorado y tiene un destello que lo cruza), títulos dorados con contorno y entradas animadas en cascada. El menú tiene el cartel «● EN VIVO» y una cinta de noticias abajo con los récords; el anuncio de cada ronda también lleva «EN VIVO». Las tarjetas de premio brillan con el color de su rareza.

**Público:** las gradas tienen lugar para 72 personas. En las rondas 1 y 2 hay público normal (25 personas); desde la ronda 3, en cada carrera llegan unas 8 personas más, de a poco y saltando, hasta llenar el estudio en la ronda 8. El público grita sobre las gradas: «¡WOW!», «¡BRAVO!» u «¡OLÉ!» con las rachas, la meta o al pillar a Tito; «¡UUUH!» al esquivar por un pelo; «¡JAJAJA!» al caer al agua y «¡BUUU!» al chocar (más abucheos si choca seguido).

**Ayudas en pantalla:** al empezar la partida se ven los controles unos segundos; en la primera ronda con trampas, cómo acusar a Tito. Durante la carrera aparecen mensajes de ánimo: «¡Todo va bien!», «¡Mitad de la meta!», «¡Ya casi!», «¡Sin un rasguño!», «¡Qué reflejos!», «¡Arriba!» después de un golpe y «¡Llega más público!».

![Figura 4. Flujo de pantallas: ciclo de rondas y regreso tras perder](img/flujo.png)

**Cómo se ven las pantallas principales**

![Figura 5. Bocetos: menú, premios y eliminado](img/bocetos.png)

La pausa se abre con Esc durante cualquier ronda y vuelve al mismo punto.

| Pantalla | Contenido | Notas |
| --- | --- | --- |
| Menú principal | Jugar, Cómo jugar, Opciones, Créditos y Salir; récord visible | En la versión web se oculta Salir, porque el navegador no permite cerrar el juego |
| Cómo jugar | Controles, objetivo y una imagen | Máximo 4 líneas de texto |
| Opciones | Volumen de música, volumen de efectos y pantalla completa | Se guardan con el SaveManager |
| Créditos | Equipo y licencias de los assets | Obligatorio si algún asset es CC-BY |
| Anuncio de Tito | Número de ronda, meta, regla especial y la apuesta (aceptar o rechazar) | Avanza al elegir |
| HUD | Estrellas y meta, tiempo, vidas, ronda, regla activa y aviso de «¡Trampa!» disponible | Siempre visible durante la ronda |
| Premios | 3 tarjetas con nombre, ícono y efecto; un regalo de Tito puede estar escondido | Clic o teclas 1, 2 y 3 |
| Pausa | Continuar, Reiniciar, Opciones y Menú principal | Detiene la ronda |
| Eliminado | Ronda alcanzada, estrellas totales y récord; botones «¿Una ronda más?» y Menú | «¿Una ronda más?» es el botón grande y viene seleccionado |

## Versión 3D en Unity

El juego se ve en 3D, pero la lógica es la de un runner: el concursante solo cambia entre 3 carriles, salta y se desliza, y la pista se mueve hacia él.

| Elemento | Cómo se hace en Unity 2022.3 |
| --- | --- |
| Render | Built-in Render Pipeline con shader Standard; funciona bien en WebGL |
| Movimiento | El concursante se queda en z = 0 y los tramos avanzan hacia −Z a la velocidad de la ronda |
| Carriles | Tres posiciones fijas en X (−2, 0 y 2 m); el cambio de carril se anima por código en 0,15 s |
| Salto y deslizamiento | Salto con una curva simple en Y calculada por código; al deslizarse, la cápsula baja a ~0,7 m durante 0,6 s |
| Colisiones | Obstáculos, estrellas y agua con colliders en modo Trigger; concursante con Rigidbody cinemático |
| Tramos | Prefabs de 20 m que el `TrackGenerator` crea adelante y destruye al pasar detrás de la cámara |
| Cámara | Camera fija en (0, 4, −7) mirando hacia adelante; niebla del color del fondo para ocultar la aparición de tramos |
| Luces | Una Directional Light suave, Spot Lights de colores a los lados y luz ambiental en modo Color |
| Agua | Plane grande con material Standard en modo Fade, azul semitransparente |
| Apagón | Luz ambiental casi en negro, Directional Light apagada y una Spot Light que sale del concursante hacia adelante |
| Partículas | Particle System para salpicaduras, confeti y destellos |
| Interfaz | Canvas de uGUI con TextMeshPro encima del juego; la cara de Tito en un Canvas World Space |
| Guardado | `PlayerPrefs` (en WebGL se guarda en el navegador) |

**Reglas para que el 3D no complique el trabajo**

- El concursante nunca avanza: se mueve la pista. Así todo es más fácil de probar.
- Escala real: 1 unidad = 1 metro. El concursante mide 1,6 m, cada carril 2 m y cada tramo 20 m.
- Un material por color de la paleta, reutilizado en todos los objetos.
- La build WebGL se prueba el sábado con la escena casi vacía, no el domingo con el juego terminado.
- En WebGL el audio empieza recién después del primer clic del jugador; el menú principal lo resuelve.
- Las fuentes de TextMeshPro se crean en modo Dynamic para que salgan tildes, ñ, ¡, ¿ y «».

## Herramientas gratis para ir más rápido

Todo el juego se puede hacer sin pagar nada.

| Para qué | Herramienta | Por qué ayuda |
| --- | --- | --- |
| Motor del juego | Unity 2022.3 LTS (Personal) con el módulo WebGL Build Support | Gratis; exporta al navegador para itch.io |
| Escribir código | Claude Code + Visual Studio Community o VS Code con la extensión de Unity | Autocompletado y depuración de C# |
| Código compartido | GitHub con GitHub Desktop | Repositorio privado gratis; evita pelear con comandos de Git |
| Formas 3D | Primitivas de Unity; ProBuilder (paquete gratis) para bordes redondeados | Sin modelar nada |
| Modelos 3D (opcional) | Kenney y Quaternius | Licencia CC0: se usan sin pedir permiso |
| Efectos de sonido | jsfxr y los packs de audio de Kenney | Sonidos arcade listos en segundos |
| Música | OpenGameArt, filtrando por CC0 | Música lista para usar |
| Editar audio | Audacity | Cortar, nivelar volumen y exportar a OGG |
| Tipografías | Google Fonts | Licencia OFL, se pueden usar en juegos |
| Tareas del equipo | GitHub Projects o Trello | Tablero con las tareas de `docs/PLAN.md` |
| Comunicación | Discord | Llamada y pantalla compartida mientras trabajan |
| Grabar gameplay | OBS Studio | Graba la pantalla en buena calidad para el pitch |
| Editar el video | DaVinci Resolve o CapCut | Sus versiones gratuitas alcanzan para un video de 3 a 5 minutos |
| Diapositivas | Google Slides o Canva | Plantillas gratis para el pitch |
| Publicar el juego | itch.io | Hospeda la versión WebGL gratis y es la plataforma que pide la jam |

## Arte y sonido sin saber diseñar

El juego se ve bien sin dibujar nada: packs gratuitos, una paleta fija de colores y mucho movimiento.

| Recurso | Para qué | Licencia |
| --- | --- | --- |
| Kenney (kenney.nl) | Modelos 3D opcionales, íconos de interfaz y sonidos | CC0, sin obligación de dar créditos |
| Quaternius (quaternius.com) | Personajes y objetos 3D low-poly con animaciones | CC0 |
| Google Fonts | Tipografías para títulos y textos | OFL, uso libre en juegos |
| jsfxr | Generar efectos de sonido arcade desde el navegador | Sonidos generados por el equipo |
| OpenGameArt (opengameart.org) | Música de fondo | Variable: filtrar por CC0 o CC-BY y dar crédito |

Revisar la licencia de cada pack al descargarlo y anotarla en la pantalla de Créditos.

**Reglas de estilo**

- Paleta fija de 5 colores: fondo oscuro (morado o azul noche), dorado para estrellas, rojo para peligros, azul para el jugador y blanco para textos.
- El color comunica: rojo siempre es peligro y dorado siempre es puntos.
- Una tipografía para títulos y otra para textos, nada más.
- La pista se arma con formas simples: cajas y cilindros inflables de colores, agua azul a los lados, focos y un letrero con el nombre del programa.
- La sensación de calidad viene del movimiento: partículas al recoger estrellas, sacudida de cámara al recibir un golpe y botones que crecen al pasar el mouse.

**Atajos para ganar tiempo**

- Usar las primitivas de Unity en lugar de buscar modelos: se ve coherente y no hay que importar nada.
- Hacer primero un tramo como plantilla (prefab) y duplicarlo para armar los demás.
- Crear materiales, prefabs base y datos con scripts de Editor (menú «Una Ronda Más/...») en lugar de hacerlos a mano.
- No reutilizar código de proyectos anteriores: el reglamento descalifica cualquier desarrollo previo.

## Alcance

Lo obligatorio debe estar listo el domingo a las 17:00; lo extra solo se empieza si el MVP ya funciona completo.

| Nivel | Incluye |
| --- | --- |
| MVP (obligatorio) | Concursante con carriles, salto y deslizamiento; 5 obstáculos; estrellas normales y doradas; 12 tramos; meta y tiempo por ronda; 6 premios y 4 premios envenenados; 5 reglas; mentiras de Tito con hologramas y botón «¡Trampa!»; apuesta; frases y cara de Tito; menú principal, Cómo jugar, Opciones, Créditos, anuncio de ronda, HUD, Premios, Pausa y Eliminado; récord guardado; menú que cambia desde la segunda partida; música y efectos básicos; build web |
| Extra (si sobra tiempo) | Pelota rodante y rodillo giratorio, 8 premios y 8 reglas, cartel falso de «¡Atajo!», final falso de la ronda 10, público animado, soporte de mando, interfaz en inglés |
| Fuera de alcance | Multijugador, progresión permanente entre partidas, varios escenarios, historia larga, voces grabadas |
