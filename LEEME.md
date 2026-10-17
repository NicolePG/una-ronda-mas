# Cómo usar este paquete con Claude Code

Este paquete le da a Claude Code todo el contexto del juego **¡Una Ronda Más!** para que programe con el equipo.

| Archivo | Para qué |
| --- | --- |
| `CLAUDE.md` | Contexto que Claude Code lee solo cada vez que se abre en el proyecto: idea, reglas de la jam, equipo, arquitectura, eventos y convenciones |
| `docs/GDD.md` | Documento de diseño completo del juego |
| `docs/PLAN.md` | Fases, tareas de cada integrante, horarios y recortes |
| `docs/img/` | Imágenes de personajes, obstáculos, tramos, pantalla de juego y menús |
| `.gitignore` | Evita subir a GitHub las carpetas pesadas que genera Unity |

## Pasos

1. Crear el proyecto en Unity 2022.3.62f2 con la plantilla **3D (Built-In Render Pipeline)**.
2. Copiar `CLAUDE.md`, `.gitignore` y la carpeta `docs/` a la **raíz del proyecto**, al lado de las carpetas `Assets/` y `ProjectSettings/`. No dentro de `Assets/`.
3. Abrir Claude Code en esa carpeta (la raíz del proyecto).
4. Primer mensaje recomendado:

> Lee CLAUDE.md, docs/GDD.md y docs/PLAN.md. Soy Nicole. Resúmeme en 5 líneas qué vamos a construir y empecemos con la Fase 0, paso 7.

5. Subir estos archivos al repositorio de GitHub junto con el proyecto: así Otoniel y Tania tienen el mismo contexto cuando usen Claude Code en sus computadoras.
