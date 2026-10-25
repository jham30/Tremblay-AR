# Animaciones de misión — Halloween (Etapa 1)

Control de qué misiones ya tienen su animación (Timeline) al completarse en AR.
**Revisado:** 2026-10-18 contra `Assets/misiones/Halloween/Etapa 1` y la escena `halloween`.

## Cómo se conecta una animación

1. El `PlayableDirector` va **dentro del Image Target del objeto destino** (ej. `ImageTargetFarol / ContFarol / AnimacionFarol`).
2. En la misión, `nombreTimelineAlCompletar` debe ser **igual** al nombre del GameObject del director
   **o** al nombre del asset `.playable` (lo busca `ObjectInfoUIManager.BuscarTimelineMision`).
3. Play On Awake apagado y Wrap Mode **Hold** (nunca Loop). Un timeline por misión.

## Resumen

| Estado | Misiones |
|---|---|
| ✅ Funciona | 2 (01, 37) |
| ⚠️ Asignada pero el nombre no coincide | 1 (03) |
| 🟡 Animación en escena, falta asignarla a la misión | 2 timelines (mano ← araña; árbol ← calabaza) |
| 🟠 Asset hecho, falta ponerlo en la escena | 1 timeline (ataúd ← pala) |
| ⬜ Sin animación | el resto |
| **Total** | **47 misiones** |

## Arreglos pendientes

- **Misión 03 (calabaza ← vela):** pide `AniTutoCalabazaVelaTimeline`, pero en `halloween` el director se
  llama `animacionCalabazavela` y usa `animacionCalabazaTimeline`. No va a sonar. Poner en la misión
  `animacionCalabazaTimeline` (o `animacionCalabazavela`).
  Ojo: el asset `Etapa 1/03` es de `halloween`; el tutorial usa su propia misión `t01`.
- **Tutorial `t01` (escena `halloween-tuto1`):** pide `AniTutoCalabazaTimeline`, pero el asset se renombró a
  `AniTutoCalabazaVelaTimeline` y el director se llama `AniTutoCalabaza`. Tampoco coincide. Poner
  `AniTutoCalabazaVelaTimeline` (o `AniTutoCalabaza`).
- **`animacionManoArana`** (en `ImageTargetMano`): ninguna misión la pide. Candidata: **21** (mano ← araña).
- **`AnimaCalabaArbol`** (en `ImageTargetArbol`): ninguna misión la pide. Candidatas: **24, 30 o 31**
  (las tres llevan calabaza al árbol). Hace falta decidir cuál.
- **`PalaAtaudTimeline.playable`**: existe en `Assets/models/modelos animados/pala`, pero no hay director en
  la escena. Candidatas: **17 o 32** (ataúd ← pala).
- **`AbrirLibro.playable`** (`Assets/models/book`): sin director ni misión. Candidata: **13** (libro ← farol).

## Lista completa

Destino ← objetos que hay que llevar. 📖 = la misión dispara un fragmento de historia (D = al descifrar,
C = al completar). Las misiones con fragmento al completar son las que más ganan con animación.

| Misión | Destino | Objetos | 📖 | Timeline asignado | Estado |
|---|---|---|---|---|---|
| 01 | farol | vela | D | `AnimacionFarolTimeline` | ✅ |
| 02 | araña | escoba | C | | ⬜ |
| 03 | calabaza | vela | D C | `AniTutoCalabazaVelaTimeline` | ⚠️ nombre |
| 04 | mesa | gorro, escoba | C | | ⬜ |
| 05 | gato | salchicha | | | ⬜ |
| 06 | gato | gorro | C | | ⬜ |
| 07 | lámpara | farol, escoba | D | | ⬜ |
| 08 | gorro | hongo | C | | ⬜ |
| 09 | cráneo | vela | D | | ⬜ |
| 10 | murciélago | escoba | | | ⬜ |
| 11 | mano | pala | C | | ⬜ |
| 12 | tumba | pala | | | ⬜ |
| 13 | libro | farol | D | | ⬜ (`AbrirLibro`?) |
| 14 | mausoleo | cráneo | | | ⬜ |
| 15 | lápida | cráneo, vela | C | | ⬜ |
| 16 | lámpara | vela | D | | ⬜ |
| 17 | ataúd | pala | C | | 🟠 (`PalaAtaud`?) |
| 18 | mausoleo | calabaza, cráneo | | | ⬜ |
| 19 | araña | escoba, gorro | C | | ⬜ |
| 20 | gato | hongo | | | ⬜ |
| 21 | mano | araña | D | | 🟡 `animacionManoArana` |
| 22 | gorro | mano | | | ⬜ |
| 23 | reja | vela | | | ⬜ |
| 24 | árbol | calabaza, vela, farol | C | | 🟡 (`AnimaCalabaArbol`?) |
| 25 | farol | araña, escoba | | | ⬜ |
| 26 | lápida | escoba | | | ⬜ |
| 27 | calabaza | vela | | | ⬜ |
| 28 | reja | escoba | C | | ⬜ |
| 29 | tumba | lámpara, vela | | | ⬜ |
| 30 | árbol | calabaza, farol | | | 🟡 (`AnimaCalabaArbol`?) |
| 31 | árbol | lámpara, calabaza | | | 🟡 (`AnimaCalabaArbol`?) |
| 32 | ataúd | pala | D | | 🟠 (`PalaAtaud`?) |
| 33 | tumba | farol | | | ⬜ |
| 34 | tumba | calabaza | D | | ⬜ |
| 35 | ataúd | mano | | | ⬜ |
| 36 | bruja | vela | | | ⬜ |
| 37 | flor | pala | C | `AnimacionFlorPalaTimeline` | ✅ |
| 38 | tumba | flor | | | ⬜ |
| 39 | bruja | escoba, gorro | | | ⬜ |
| 40 | ataúd | gorro, escoba | C | | ⬜ |
| 41 | caldero | hongo, calabaza | | | ⬜ |
| 42 | reja | farol | D | | ⬜ |
| 43 | tumba | pala, cráneo | | | ⬜ |
| 44 | lápida | flor, farol | C | | ⬜ |
| 45 | bruja | farol, gorro | D | | ⬜ |
| 46 | tumba | bruja | C | | ⬜ |
| 47 | gato | salchicha | C | | ⬜ |

## Animaciones por objeto destino

Útil para planear: cada destino necesita un Image Target con sus timelines dentro.

| Destino | Misiones | Hechas |
|---|---|---|
| tumba | 12, 29, 33, 34, 38, 43, 46 | 0 / 7 |
| ataúd | 17, 32, 35, 40 | 0 / 4 (pala hecha sin montar) |
| gato | 05, 06, 20, 47 | 0 / 4 |
| árbol | 24, 30, 31 | 0 / 3 (1 en escena sin asignar) |
| bruja | 36, 39, 45 | 0 / 3 |
| lápida | 15, 26, 44 | 0 / 3 |
| reja | 23, 28, 42 | 0 / 3 |
| araña | 02, 19 | 0 / 2 |
| calabaza | 03, 27 | 0 / 2 (03 con nombre roto) |
| farol | 01, 25 | 1 / 2 |
| gorro | 08, 22 | 0 / 2 |
| lámpara | 07, 16 | 0 / 2 |
| mano | 11, 21 | 0 / 2 (1 en escena sin asignar) |
| mausoleo | 14, 18 | 0 / 2 |
| caldero | 41 | 0 / 1 |
| cráneo | 09 | 0 / 1 |
| flor | 37 | 1 / 1 |
| libro | 13 | 0 / 1 (`AbrirLibro` sin montar) |
| mesa | 04 | 0 / 1 |
| murciélago | 10 | 0 / 1 |
