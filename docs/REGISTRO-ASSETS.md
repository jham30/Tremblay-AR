# Registro de assets de terceros

Qué contenido del juego viene de fuera, bajo qué licencia y con qué pruebas.

Este archivo existe para tres momentos concretos: cuando un abogado revise la
propiedad intelectual del juego, cuando alguien haga due diligence (la visa de
Start-up de Canadá, un inversor), y cuando llegue una reclamación de derechos a
Google Play o a la App Store. En los tres casos, lo que vale es poder demostrar
de dónde salió cada cosa.

Última revisión: 2026-10-02

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

## Paquetes del Unity Asset Store

| Paquete | Carpeta | Autor | Gratis o pago | Cuenta de Unity que lo adquirió | Fecha | Enlace |
|---|---|---|---|---|---|---|
| Casual Game Sounds U6 | `Assets/Casual Game Sounds U6/` | | | | | |
| FREE Cartoon Halloween Pack | `Assets/FREE_Cartoon_Halloween_Pack/` | | Gratis (por el nombre) | | | |
| Free 8 Spooky Tracks | `Assets/Free 8 Spooky Tracks/` | | Gratis (por el nombre) | | | |
| Halloween Pack - Cemetery Snap | `Assets/Halloween Pack-Cemetery Snap/` | | | | | |
| Pandazole Ultimate Pack | `Assets/Pandazole_Ultimate_Pack/` | | | | | |

Solo `Casual Game Sounds U6` trae su `license.pdf` dentro de la carpeta. Los otros
cuatro no tienen licencia documentada en el repo.

---

## Contenido de Pixabay u otras fuentes libres

Una fila por archivo o por lote. Sin la URL y la fecha, la prueba no sirve.

| Archivo o lote | Dónde se usa | Fuente | Autor | URL original | Fecha de descarga | Captura guardada |
|---|---|---|---|---|---|---|
| | | | | | | |

---

## Pendientes

- [ ] Rellenar autor, origen y enlace de los cuatro paquetes sin documentar
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
interfaz, y **el juego como obra en conjunto**.

Esa combinación es original y protegible, y es también lo que sostiene la condición
del EULA de Unity sobre incorporar los assets junto a contenido original sustancial.
