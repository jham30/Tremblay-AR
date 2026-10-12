# Registro de assets de terceros

Qué contenido del juego viene de fuera, bajo qué licencia y con qué pruebas.

Este archivo existe para tres momentos concretos: cuando un abogado revise la
propiedad intelectual del juego, cuando alguien haga due diligence (la visa de
Start-up de Canadá, un inversor), y cuando llegue una reclamación de derechos a
Google Play o a la App Store. En los tres casos, lo que vale es poder demostrar
de dónde salió cada cosa.

Última revisión: 2026-10-07

---

## Resumen de las licencias en uso

### Unity Asset Store — tipo "Extension Asset"

Todos los paquetes del Asset Store usados en el proyecto son de este tipo.

| | |
|---|---|
| Propiedad | **Licenciados, no vendidos** (EULA 1.3). El copyright sigue siendo del autor |
| Ordenadores | **Máximo 2 por puesto** (EULA 2.3.2). La regla de "ordenadores ilimitados" NO aplica a Extension Assets |
| Por persona | Licencia **por puesto**. Cada colaborador necesita la suya (EULA 2.4) |
| Compartir coste | Prohibido el *forum pooling* (EULA 2.2.1.1a) |
| Uso permitido | Incorporar al juego, distribuir, modificar, monetizar incluso con compras in-app (EULA 2.2.1) |
| Condición | Debe combinarse con **contenido original sustancial** y el asset **no puede ser una porción sustancial** del producto |
| Marketing | Permitido usarlos en material publicitario del juego (EULA 2.2.1c) |
| Duración | Perpetua. Si el paquete se retira de la tienda, el derecho sigue, pero ya no se puede volver a descargar |
| Responsabilidad IP | Del Provider, no de Unity (EULA 1.4) |

Fuente: https://unity.com/legal/as-terms

**Ordenadores en uso ahora: 2 de 2.** PC de Windows (desarrollo) y MacBook (desarrollo
y compilación iOS). Sin margen para una tercera máquina ni para otra persona.

### Pixabay — Content License

| | |
|---|---|
| Propiedad | No se cede. Solo derecho de uso |
| Atribución | No obligatoria |
| Uso comercial | Permitido |
| Modificación | Permitida |
| **Como marca** | **PROHIBIDO.** No se puede usar como marca comercial, de diseño, nombre comercial ni de empresa |
| Venta suelta | Prohibido distribuir el contenido sin esfuerzo creativo aplicado |
| Marcas dentro del contenido | Si el archivo contiene logos o marcas reconocibles, no vale para uso comercial |
| Verificación | **Pixabay no comprueba que quien sube sea el autor.** El riesgo es del que lo usa |

Fuente: https://pixabay.com/service/license-summary/

---

### Claude (Anthropic) — código generado

| | |
|---|---|
| Propiedad | **Cesión.** "we assign to you all of our right, title, and interest—if any—in Outputs" |
| Uso comercial | Sin restricciones |
| Atribución | No exigida |
| Matiz | El "if any" importa: el contenido generado por máquina puede tener copyright débil o nulo según la jurisdicción. Nadie más lo reclama, y la dirección y revisión humanas sí aportan autoría |
| A revisar | Las entradas se usan para entrenar modelos salvo que se desactive en los ajustes de la cuenta |

Fuente: https://www.anthropic.com/legal/consumer-terms

### ElevenLabs — audios generados

| | |
|---|---|
| Propiedad | **Conservada.** "you retain all rights in and to your Output" (4.c.ii) |
| **Uso comercial** | **SOLO con plan de pago.** Un Free User "may only use the Services for non-commercial purposes" (1.c) |
| Licencia que les cedéis | Perpetua, irrevocable, mundial y sublicenciable, sobre entradas y salidas, para mejorar sus servicios y desarrollar productos nuevos (4.d). No exclusiva |
| Voces clonadas | Hay que estar autorizado a usar esa voz. Las de la Voice Library pueden tener condiciones del propietario |

Fuente: https://elevenlabs.io/terms-of-use

**Publicar el juego en las tiendas bajo una empresa es uso comercial**, aunque la
descarga sea gratuita. Si la narración se generó con una cuenta gratuita, no hay
derecho de uso comercial y hay que regenerarla con una suscripción de pago,
guardando prueba de la fecha y el plan.

## Paquetes del Unity Asset Store

Autor, precio y licencia comprobados en la página de cada paquete el 2026-10-07.
Todos son **gratuitos** y con licencia **Extension Asset** (Standard Unity Asset Store EULA).

| Paquete (nombre en la tienda) | Carpeta | Autor | Cuenta de Unity que lo adquirió | Fecha | Enlace |
|---|---|---|---|---|---|
| FREE Casual Game SFX Pack | `Assets/Casual Game Sounds U6/` | Dustyroom | | | https://assetstore.unity.com/packages/audio/sound-fx/free-casual-game-sfx-pack-54116 |
| Free 8 Spooky Tracks Music Pack | `Assets/Free 8 Spooky Tracks/` | alkakrab | | | https://assetstore.unity.com/packages/audio/music/free-8-spooky-tracks-music-pack-275541 |
| Halloween Pack \| Cemetery Snap | `Assets/Halloween Pack-Cemetery Snap/` | Mumifier Studio | | | https://assetstore.unity.com/packages/3d/environments/fantasy/halloween-pack-cemetery-snap-235573 |
| Pandazole - Lowpoly Asset Bundle | `Assets/Pandazole_Ultimate_Pack/Pandazole_Lowpoly_Asset_Bundle/` | Pandazole | | | https://assetstore.unity.com/packages/3d/props/pandazole-lowpoly-asset-bundle-226938 |
| Pandazole - City Town Lowpoly Pack | `…/Pandazole_Lowpoly_Asset_Bundle/Pandazole City Town Pack/` | Pandazole | | | https://assetstore.unity.com/packages/3d/props/exterior/pandazole-city-town-lowpoly-pack-205787 |
| Pandazole - Home Interior Low Poly Pack | `…/Pandazole_Lowpoly_Asset_Bundle/Pandazole Home Interior/` | Pandazole | | | https://assetstore.unity.com/packages/3d/props/interior/pandazole-home-interior-low-poly-pack-203033 |
| Pandazole - Survival Crafting Low Poly Pack | `…/Pandazole_Lowpoly_Asset_Bundle/Pandazole Survival Crafting Pack/` | Pandazole | | | https://assetstore.unity.com/packages/3d/props/pandazole-survival-crafting-low-poly-pack-208575 |
| FREE Cartoon Halloween Pack | `Assets/FREE_Cartoon_Halloween_Pack/` | **sin identificar** | | | **falta enlace** |

Carpetas en el proyecto sin enlace de tienda todavía:
- `Assets/FREE_Cartoon_Halloween_Pack/`
- Dentro de `Pandazole_Ultimate_Pack/`: `Pandazole Kitchen Food`, `Pandazole Nature Environment Pack`,
  `Pandazole Simple Game Pack`, y en el bundle `Pandazole Farm Ranch Pack`

Si alguna de esas carpetas no se usa en el juego, conviene borrarla: baja el peso del build
y deja de hacer falta su licencia.

Solo `Casual Game Sounds U6` trae su `license.pdf` dentro de la carpeta.

---

## Créditos para la pantalla About del juego

Ninguna licencia lo exige; se da por buena fe. Perfiles comprobados el 2026-10-07.

| Crédito | Por | Perfil |
|---|---|---|
| Música: Free 8 Spooky Tracks Music Pack | alkakrab | https://assetstore.unity.com/publishers/67560 |
| Efectos de sonido: FREE Casual Game SFX Pack | Dustyroom | https://assetstore.unity.com/publishers/16150 · http://dustyroom.com |
| Modelos 3D: Halloween Pack \| Cemetery Snap | Mumifier Studio | https://assetstore.unity.com/publishers/67841 |
| Modelos 3D: Lowpoly Asset Bundle, City Town, Home Interior, Survival Crafting | Pandazole | https://assetstore.unity.com/publishers/55716 |
| Canción: Pirate Bay (Guitar) | ebunny (Pixabay) | https://pixabay.com/es/users/ebunny-22480773/ |
| Efectos de sonido adicionales | Autores de Pixabay — *pendiente* | |
| Voces de la narración | ElevenLabs | https://elevenlabs.io |
| Motor | Unity | https://unity.com |
| Realidad aumentada | Vuforia Engine (PTC) | https://developer.vuforia.com |
| Desarrollo | HOLA (Hispanic Open Learning Academy) y Media Tech Tukan | |

### Equipo de desarrollo

| Rol | Nombre |
|---|---|
| Dirección pedagógica | Patricia Domínguez |
| Dirección de proyecto | Jaime Meza |
| Ilustración | Cristian Vak |
| Modelado 3D y diseño gráfico | Mateo Meza |
| Animación 2D | Jhon Ávila |
| Animación 3D | Luciana Díaz |

---

## Contenido de Pixabay u otras fuentes libres

Una fila por archivo o por lote. Sin la URL y la fecha, la prueba no sirve.

De Pixabay solo se descargaron audios: unos efectos de sonido y una canción.

| Archivo o lote | Dónde se usa | Fuente | Autor | URL original | Fecha de descarga | Prueba guardada |
|---|---|---|---|---|---|---|
| Canción "Pirate Bay (Guitar)" (ID 376548) `Assets/models/book/pirataInicio2.mp3`, escena `Inicio` | Pixabay | ebunny (https://pixabay.com/es/users/ebunny-22480773/) | https://pixabay.com/es/music/grupo-ac%C3%BAstico-pirate-bay-guitar-376548/ | 2026-10-02 | Certificado: `docs/licencias/pixabay-376548-pirate-bay-guitar.txt` |
| Efectos de sonido | *pendiente* | Pixabay | *pendiente* | *pendiente* | | *pendiente* |

Pixabay emite un certificado de licencia por cada descarga (como el de la canción). Si se
descargan de nuevo los efectos desde la misma cuenta, se pueden obtener los suyos.

---

## Pendientes

- [ ] **Confirmar con qué plan de ElevenLabs se generaron los audios de narración.** Si
      fue el gratuito, no hay derecho de uso comercial y hay que regenerarlos
- [ ] Revisar si alguna voz de ElevenLabs es clonada o viene de la Voice Library
- [ ] Decidir si desactivar el uso de entradas para entrenamiento en la cuenta de Anthropic
- [x] Autor, precio y enlace de los paquetes del Asset Store (2026-10-07)
- [ ] Enlace y autor de `FREE_Cartoon_Halloween_Pack` y de las carpetas Pandazole sin enlace
      (o borrarlas si no se usan)
- [ ] Confirmar con qué cuenta de Unity se adquirió cada paquete. Si fue una cuenta
      personal, la licenciataria es esa persona y no Hispanic Open Learning Academy Inc.
- [ ] Guardar copia de los `.unitypackage` originales fuera del repo. La licencia es
      perpetua, pero si el autor retira el paquete ya no se puede volver a descargar
- [ ] Revisar que el **icono de la app** y cualquier elemento de marca no usen contenido
      de Pixabay. Su licencia lo prohíbe expresamente, y los Términos del juego declaran
      esos elementos como marcas de la empresa
- [ ] Revisar que ningún modelo o textura incluya logos o marcas reconocibles
- [ ] Añadir una pantalla de créditos en el juego. Ninguna licencia la exige, pero deja
      constancia de buena fe
- [ ] Si entra alguien más al proyecto, que adquiera los paquetes con su propia cuenta
      de Unity. Para los gratuitos no cuesta nada

---

## Qué sí es propiedad de Hispanic Open Learning Academy Inc.

Para tenerlo claro al otro lado de la balanza: el código, la historia y sus textos,
Éliane Tremblay y el resto de personajes, el diseño de las misiones AR, las imágenes
objetivo de Vuforia, la localización en español, inglés y francés, el diseño de la
interfaz, el código generado con Claude (cedido por Anthropic), los audios de
narración de ElevenLabs si se generaron con plan de pago, y **el juego como obra
en conjunto**.

Esa combinación es original y protegible, y es también lo que sostiene la condición
del EULA de Unity sobre incorporar los assets junto a contenido original sustancial.
