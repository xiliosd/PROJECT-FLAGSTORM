# Flagstorm — Defensas, arsenal por terreno y desbloqueo

- **Fecha:** 2026-11-12
- **Autor:** Oscar Dueñas (con asistencia de Claude Code)
- **Estado:** borrador para aprobación de Oscar
- **Base:** `docs/superpowers/specs/2026-11-09-flagstorm-design.md` (diseño general) y el prototipo de la
  etapa 1 (tareas 1 a 11 completas).

Convención: **[decisión de Oscar]** la definió el dueño del producto; **[propuesta de Claude]** es una
decisión del asistente que Oscar puede cambiar; **[a criterio del constructor]** queda libre.

---

## 1. Intención y porqué

Oscar jugó el prototipo en un Redmi (2026-11-12) y encontró tres cosas:

1. Sin animación el juego es demasiado aburrido. Esto lo resuelve **otro proyecto**, la vista 3D
   (sección 9), que se diseña después de este.
2. No entendió cómo perdió.
3. **No vio defensas.** Las que existen (torreta y señuelo) no se notan, y faltan contrapartes: si
   el rival tiene drones, debería existir un escudo antidrones; si tiene robots, debería haber
   robots que defiendan y combatan.

Este proyecto cambia las **reglas** para que el juego tenga ataque y defensa equilibrados, con un
arsenal que crece a medida que el jugador gana. Se hace antes que la vista 3D **[decisión de
Oscar]**, para que la vista 3D anime todas las piezas desde el principio.

## 2. Metas, no-metas y anti-metas

### Metas
- **Cada ataque tiene su defensa [decisión de Oscar].** Toda arma de ataque futura llega con su
  contraparte.
- Robots de **ataque y de defensa, uno de cada uno por terreno** (selva, hielo, volcán, playa y
  roca) **[decisión de Oscar]**.
- Combate entre robots, un choque por turno, con ventaja para el que está en su terreno **[decisión
  de Oscar]**.
- Arsenal de **6 espacios** que se elige antes de la partida, con armas que se **desbloquean según el
  avance** del jugador; al inicio hay pocas **[decisión de Oscar]**.
- Que quede claro por qué se ganó o se perdió.

### No-metas (van en otros proyectos)
- Animaciones, modelos 3D, cámara que sigue la acción y vuelo del robot entre islas: proyecto de la
  vista 3D.
- Campaña, regiones, jefes, tutorial y guardado completo: etapa 2.
- Nuevos terrenos (desierto, pantano, ciudad).

### Anti-metas
- Que una defensa sea invencible o que un arma sirva para todo (spec general §4.4 b).
- Que el jugador tenga que mover a mano a sus defensores: reaccionan solos **[decisión de Oscar]**.
- Que el azar decida un combate: las reglas siguen siendo deterministas, para poder planear y para
  las repeticiones (spec general §11) **[propuesta de Claude]**.

## 3. Reglas nuevas

### 3.1 Todo lo que está en una isla tiene vida
Robots (de ataque y defensores), torretas, escudos antidrones y antimisiles tienen **puntos de vida
(PV)** **[propuesta de Claude, a partir de lo que pidió Oscar]**. El señuelo conserva su regla
actual.

### 3.2 Misiles
- Un misil daña **todo lo rival** que esté en su área: robots defensores, torretas, escudos y
  antimisiles. El daño es su poder ajustado por el terreno de cada casilla afectada (como hoy). Lo
  que llega a 0 PV queda destruido **[decisión de Oscar]**.
- Un misil **no daña a los robots propios** que estén en su área **[propuesta de Claude]**.
- Un misil sigue sin revelar ni destruir señuelos ni banderas (regla actual).

### 3.3 Antimisiles (nuevo)
- Defensa que se construye en la isla propia. Protege una zona alrededor de su casilla.
- Un misil cuyo **objetivo** cae dentro de una zona protegida es **interceptado**: no hace daño y el
  antimisiles pierde PV o una carga **[a criterio del constructor]**, para que no sea invencible.
- El atacante sabe que su misil fue interceptado y en qué casilla apuntaba.

### 3.4 Escudo antidrones (nuevo)
- Defensa que se construye en la isla propia. Protege una zona alrededor de su casilla.
- Un dron cuyo **objetivo** cae dentro de una zona protegida es **derribado**: no revela ninguna
  casilla y la energía se pierde **[decisión de Oscar]**.
- El atacante ve dónde fue derribado, lo que le indica que ahí hay algo que proteger **[propuesta de
  Claude]**.
- Un misil puede dañar o destruir el escudo (§3.2) **[decisión de Oscar]**.

### 3.5 Robots de ataque (uno por terreno)
- Los dos robots actuales pasan a ser cinco, uno especializado en cada terreno. Cada uno tiene PV,
  **nivel de arma** (poder de ataque), puntos de movimiento y costos de movimiento por terreno
  **[decisión de Oscar: nivel de arma y ventaja en su terreno]**.
- Desembarcan en la costa y se mueven como hoy. Siguen capturando banderas y exponiendo señuelos.

### 3.6 Robots defensores (uno por terreno)
- Se colocan en la isla propia (en la preparación o construyéndolos durante el turno, como la
  torreta) **[a criterio del constructor]**.
- Cada uno vigila una **zona de guardia** alrededor de su puesto **[decisión de Oscar]**.
- Si un robot rival está dentro de su zona, el defensor **avanza solo una casilla por turno** hacia
  él, sin que el jugador lo mueva **[decisión de Oscar]**. No sale de su zona; si el atacante la
  abandona, se detiene o vuelve a su puesto **[propuesta de Claude]**.
- Si no hay atacantes en su zona, no se mueve.

### 3.7 Combate entre robots
- Cuando un robot de ataque y un defensor quedan juntos, **entablan batalla**: **un choque por
  turno** **[decisión de Oscar]**.
- En cada choque, cada robot le quita al otro un daño igual a su **nivel de arma ajustado por el
  terreno** de la casilla donde pelean; el que pelea en su terreno tiene ventaja **[decisión de
  Oscar]**. Los dos golpes son simultáneos **[propuesta de Claude]**. El que llega a 0 PV queda
  destruido.
- Entre choques el jugador puede reaccionar: retirar su robot, mandar otro o disparar un misil.
- **Robots contra defensas fijas [decisión de Oscar]:** un robot de ataque junto a un escudo,
  antimisiles o torreta rival le quita cada turno su nivel de arma ajustado por el terreno, hasta
  destruirla. Así ninguna defensa es invencible aunque el jugador aún no tenga misiles. Las mismas
  reglas de "quedar juntos" y de momento del turno que el combate entre robots.
- Qué significa "quedar juntos" (casilla vecina o misma casilla), en qué momento del turno se mueven
  los defensores y se resuelven los choques, y si un robot en combate puede seguir avanzando
  **[a criterio del constructor]**, con dos condiciones: el resultado debe ser determinista y el
  registro de eventos debe dejar claro qué pasó.

### 3.8 Torreta y señuelo
Se mantienen. La torreta dispara a los robots rivales cercanos y el señuelo engaña a drones y
misiles hasta que un robot lo descubre. Ambos son armas del arsenal y se desbloquean como las demás.

### 3.9 Preparación
Las 2 torretas gratis de hoy pasan a ser **2 defensas gratis elegidas entre las defensas que el
jugador lleva en su arsenal** (escudo, antimisiles, torreta o defensor) **[propuesta de Claude]**.

### 3.10 Qué sabe cada bando
- Escudos, antimisiles, torretas y robots defensores rivales están **ocultos** hasta que se revela
  su casilla, como hoy las torretas **[propuesta de Claude]**.
- Un dron derribado **revela la casilla del escudo** que lo derribó; un misil interceptado revela la
  casilla del antimisiles **[propuesta de Claude]**. Así el escudo no es una caja negra imposible de
  encontrar y el misil tiene a qué apuntar.
- Un robot de ataque ve a los defensores que están en casillas vecinas, y todo combate deja visibles
  a los dos robots **[propuesta de Claude]**.

### 3.11 Alcance de los misiles y de "retirarse"
- Los misiles siguen apuntando solo a la isla rival. El defensor responde con sus defensores,
  torretas y nuevas construcciones, no con misiles sobre su propia isla **[propuesta de Claude]**.
- "Retirar" un robot en combate significa alejarlo caminando con sus puntos de movimiento; no hay
  comando para sacarlo de la isla **[propuesta de Claude]**.

## 4. Arsenal y desbloqueo

- Con lo anterior hay 17 armas: dron, escudo antidrones, misil estándar, misil incendiario,
  antimisiles, torreta, señuelo, 5 robots de ataque y 5 defensores.
- Antes de cada partida el jugador elige **6 armas** de las que tiene desbloqueadas **[decisión de
  Oscar]**. Si tiene 6 o menos, las lleva todas sin elegir **[propuesta de Claude]**.
- **Desbloqueo por partidas ganadas**, guardado en el celular **[propuesta de Claude, aceptada por
  Oscar]**. Las armas se desbloquean **en pares, un ataque junto con su defensa** **[propuesta de
  Claude, aceptada por Oscar]**.
- **Inicio:** dron + escudo antidrones, y robot de playa + defensor de playa **[propuesta de Claude,
  aceptada por Oscar]**.
- El orden de los pares siguientes queda **[a criterio del constructor]**. Ejemplo: misil estándar +
  antimisiles, luego selva, hielo, roca y volcán; el misil incendiario, la torreta y el señuelo en
  algún punto del camino.
- La **IA usa un arsenal del mismo tamaño** que el del jugador **[propuesta de Claude, aceptada por
  Oscar]**, elegido según el terreno de la isla rival y **solo entre las armas que el jugador ya
  desbloqueó** **[propuesta de Claude]**, para que el jugador siempre tenga con qué responder.
- El desbloqueo debe poder reiniciarse para pruebas **[propuesta de Claude]**.

## 5. IA

La IA debe usar todo lo nuevo de forma razonable: construir escudos, antimisiles y defensores cerca
de sus banderas; elegir robots de ataque según el terreno; y disparar misiles contra defensas y
defensores que le estorban **[propuesta de Claude]**. Sigue sin ver información oculta (spec general
§5.2). El nivel exacto de astucia queda **[a criterio del constructor]**; la prueba de IA contra IA
debe seguir terminando todas sus partidas.

## 6. Prototipo en vista holográfica

Hasta que exista la vista 3D, el prototipo actual debe permitir probar todo esto:
- Pantalla de **selección de arsenal** antes de la partida (solo cuando hay más de 6 armas).
- Íconos de forma propia para escudo, antimisiles, robots defensores y robots de ataque, con su
  zona visible y sus PV, sin depender solo del color (WCAG 1.4.1).
- Registro de eventos con los nuevos casos: dron derribado, misil interceptado, defensor avanza,
  choque y daño, robot destruido.
- **Panel final que explique el resultado** **[propuesta de Claude]**, por ejemplo "El rival capturó
  tus 5 banderas" o "Capturaste las 5 banderas rivales", y en el registro qué robot hizo cada captura.

## 7. Equilibrio

- Los valores de energía, PV, poder, radios de las zonas, cargas del antimisiles y modificadores por
  terreno quedan **[a criterio del constructor]**, cumpliendo las condiciones del spec general
  §4.4: en cada terreno hay al menos un ataque y una defensa claramente buenos, y ningún elemento es
  el mejor en todos los terrenos.
- Se revisan con partidas IA contra IA (duración, quién gana, ventaja del primer turno) y con una
  nueva prueba de juego en el celular.
- Datos que hay que vigilar: en las partidas IA contra IA de la tarea 9, Blue (el que empieza) ganó
  14 de 20; en una partida de prueba la IA capturó 3 banderas en su primer turno porque estaban
  junto a la costa.

## 8. Restricciones

- Todo en el núcleo (`packages/com.flagstorm.core`): C# 9 compatible con Unity, determinista,
  serializable por comandos y eventos, sin Unity dentro (spec general §7 y §11).
- Pruebas escritas junto con el código (TDD); cobertura del núcleo ≥ 60 %, y ≥ 80 % en reglas
  críticas si aplica (lineamientos de Oscar).
- Las partidas repetidas a partir del historial deben dar la misma huella de estado.
- El guardado del desbloqueo no guarda datos personales.

## 9. Proyecto siguiente: vista 3D (anotado, no se construye aquí)

Lo que Oscar pidió y se diseñará después **[decisión de Oscar]**:
- Juego en 3D con **modelos gratuitos de uso comercial** para empezar (por ejemplo Kenney o
  Quaternius, CC0), registrando la licencia de cada uno.
- Cada terreno con relieve y animación propia (dunas si se agrega el desierto; palmeras, lava,
  hielo, olas).
- El robot **vuela de una isla a otra y aterriza**; se ve el recorrido de drones y misiles hasta su
  objetivo; se ven los combates; el turno del rival se ve completo, para entender por qué se pierde.
