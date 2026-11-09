# Flagstorm — Documento de diseño

- **Fecha:** 2026-11-09
- **Autor:** Oscar Dueñas (con asistencia de Claude Code)
- **Estado:** aprobado por Oscar (2026-11-09)
- **Nombre:** "Flagstorm" es nombre de trabajo. Una búsqueda web (2026-11-09) no encontró juegos,
  apps ni marcas con ese nombre; falta la verificación oficial (SIC, USPTO, tiendas, dominio) antes
  de invertir en logo y marca.

Convención de este documento: lo marcado **[decisión de Oscar]** lo definió el dueño del producto;
lo marcado **[propuesta de Claude]** es una decisión tomada por el asistente que Oscar puede
cambiar; lo marcado **[a criterio del constructor]** queda libre.

---

## 1. Intención y porqué

Flagstorm es un juego móvil de estrategia por turnos sobre islas, inspirado en el **género** de
juegos clásicos de "base en una isla contra base en otra isla" (como Metal Marines), pero con
mecánicas, nombre, arte, personajes y música propios. No se copia nada de ningún juego existente.

Lo que engancha, según Oscar, es:

1. **Elegir el arsenal:** escoger qué armas y qué robots llevar.
2. **El viaje entre islas:** ver el ataque cruzando el mar hasta el impacto, y avanzar de una isla
   a la siguiente en una campaña.

El objetivo de negocio **[decisión de Oscar]** es publicarlo en Google Play (y más adelante en App Store) para que
lo descargue mucha gente y genere ingresos.

## 2. El concepto central

**Captura de banderas escondidas [decisión de Oscar].** Cada bando tiene una isla y esconde en
ella **5 banderas**. Gana quien capture primero las 5 banderas del rival. El objetivo no es
destruir la base enemiga, sino esconder bien y encontrar mejor.

**El terreno importa [decisión de Oscar].** Las armas ganan o pierden potencia según el terreno,
y hay armas y robots especializados para cada terreno. Elegir el arsenal es elegir lo adecuado
para la isla rival, no lo "más fuerte".

**Por turnos [decisión de Oscar].** Juega un bando, el otro ve el resultado y responde. (Al inicio
se habló de tiempo real; Oscar lo cambió explícitamente a turnos).

## 3. Metas, no-metas y anti-metas

### Metas de la versión 1.0
- Campaña para un jugador contra la IA: **20 islas, con un jefe cada 5** (islas 5, 10, 15 y 20)
  **[decisión de Oscar]**.
- Disponible en **Android** **[decisión de Oscar, 2026-11-09]**. iPhone queda **pospuesto**, no descartado:
  el código se mantiene multiplataforma, pero no se compila, prueba ni publica para iOS por ahora.
- Gráficos modernos y llamativos, con dos vistas (real y holográfica) **[decisión de Oscar]**.
- Monetización justa desde el lanzamiento (sección 10).
- Arquitectura preparada para agregar multijugador después sin reescribir el juego (sección 11).

### No-metas de la 1.0 (vienen después) [propuesta de Claude]
- Multijugador en línea (asíncrono), multijugador local cercano (Bluetooth o Wi-Fi) y modo "pasa
  el celular". Oscar quiere los tres en el juego final **[decisión de Oscar]**; la 1.0 no los trae.
- Pase de temporada, clasificatorias y temporadas.
- Tiempo real.

### Anti-metas (si pasa esto, fracasamos aunque funcione)
- **Pagar para ganar:** nunca se venden armas, robots ni ventajas de juego **[decisión de Oscar,
  al aceptar el modelo]**.
- Parecerse a Metal Marines en nombre, personajes, arte, música, mapas o estética.
- Anuncios forzados en medio de una partida.
- Jefes imposibles: deben ser más difíciles que una isla normal de su región, pero superables
  **[decisión de Oscar]**.
- Que la vista real quede como decoración inútil, o que la holográfica sea solo un filtro de
  color: cada una tiene un propósito (sección 6).

## 4. Reglas del juego

### 4.1 La isla
- Cada isla es una cuadrícula de casillas, y cada casilla tiene un tipo de terreno. La forma de la
  cuadrícula (hexagonal o cuadrada) y su tamaño quedan **[a criterio del constructor]**; se
  recomienda hexagonal porque las distancias se leen mejor en vistas isométricas.
- Terrenos para la 1.0 **[propuesta de Claude]**: **playa**, **selva**, **hielo**, **volcán** y
  **roca/montaña**. Más adelante se pueden agregar pantano, desierto y ciudad en ruinas.
- La isla rival está cubierta por **niebla de guerra**: solo ves lo que has revelado.

### 4.2 Fases de una partida [propuesta de Claude]
1. **Preparación:** eliges tu arsenal (sección 4.4) y escondes tus 5 banderas en tu isla. También
   colocas tus defensas iniciales. La IA hace lo mismo.
2. **Turnos alternos** hasta que un bando capture las 5 banderas del otro.

En la campaña ambos bandos atacan y defienden: la IA también intenta capturar tus banderas
**[propuesta de Claude]**. Si la IA te captura las 5, pierdes la isla y puedes reintentarla.

### 4.3 Un turno [propuesta de Claude]
1. Recibes **energía** para ese turno (una cantidad base más bonificaciones).
2. La gastas en cualquier combinación de acciones:
   - **Revelar:** drones o radares que levantan la niebla en una zona y pueden detectar "señales"
     de banderas (que pueden ser señuelos).
   - **Destruir:** misiles y bombardeos que dañan defensas en una zona.
   - **Capturar:** robots que desembarcan en la costa rival y avanzan por las casillas. **Solo un
     robot puede capturar una bandera**: debe llegar vivo a la casilla de la bandera.
   - **Defender:** construir o reparar torretas, escudos y señuelos (banderas falsas) en tu isla.
3. Ves la ejecución en la vista real (sección 6) y sus resultados.
4. Turno del rival. Al empezar tu siguiente turno ves la repetición de lo que te hizo.

Un robot que no alcanzó la bandera se queda en la isla rival hasta el siguiente turno, expuesto a
las defensas.

Los valores concretos (energía por turno, costos, movimiento de robots, vida de las defensas)
quedan **[a criterio del constructor]**, y deben ajustarse con pruebas de juego. Se recomienda
que una partida normal dure de **5 a 10 minutos** **[propuesta de Claude]**.

### 4.4 Arsenal y terreno
- Antes de cada isla eliges un **arsenal limitado**, con un número fijo de espacios (por ejemplo,
  6) entre las armas y robots que tengas desbloqueados **[propuesta de Claude]**. El juego sugiere
  un arsenal adecuado al terreno, y el jugador puede ignorar la sugerencia.
- Cada arma o robot tiene un **modificador por terreno** **[decisión de Oscar]**. Ejemplos ilustrativos:
  - Misil incendiario: +40 % en selva, −50 % en hielo.
  - Robot de orugas: rápido en playa o roca, lento en volcán.
  - Robot anfibio o de nieve: especializado en su terreno, débil fuera de él.
- Debe haber **armas especializadas y robots especializados para cada terreno** **[decisión de
  Oscar]**.
- La lista exacta de armas y robots, sus valores y su equilibrio quedan **[a criterio del
  constructor]**, con dos condiciones: (a) en cada terreno debe existir al menos un arma y un
  robot claramente buenos, y (b) ningún elemento puede ser el mejor en todos los terrenos.
- Las armas y robots **se desbloquean jugando**, nunca pagando.

## 5. Campaña

- **20 islas en 4 regiones de 5** **[propuesta de Claude, sobre la decisión de Oscar de 20 islas
  con jefe cada 5]**:
  1. Archipiélago Tropical (playa y selva). La isla 1 es el tutorial jugable de 2 a 3 minutos.
  2. Costa Helada (hielo y roca).
  3. Cinturón Volcánico (volcán y roca).
  4. Fortaleza Final (todos los terrenos mezclados).
- Cada región presenta 1 o 2 armas o robots nuevos especializados en su terreno.
- **Curva de dificultad:** cada isla normal sube un poco la dificultad, el jefe da un salto y la
  primera isla de la región siguiente baja un poco.
- En el mapa de campaña se ve el **viaje de isla en isla** (la flota o el avión que se desplaza
  sobre el mar) **[decisión de Oscar: el viaje entre islas es parte de lo que engancha]**.
- Calificación de 1 a 3 estrellas por isla (por ejemplo, según turnos usados o banderas propias
  conservadas), para dar motivo de rejugar **[propuesta de Claude]**.

- **Contenido después de la isla 20 [propuesta de Claude]:** en la 1.0 la rejugabilidad viene de
  las estrellas por isla y de un modo **"Difícil"** que se desbloquea al terminar la campaña (las
  mismas islas con una IA más fuerte y menos energía). No hay generación procedural de islas en la
  1.0. Las islas se definen como **datos** (no en código), para poder agregar islas nuevas en
  actualizaciones sin reprogramar y dejar abierta la puerta a generarlas automáticamente después.

### 5.1 Jefes (islas 5, 10, 15 y 20)
Los jefes son **más difíciles que una isla normal de su región, pero superables** **[decisión de
Oscar]**. Mecanismos **[propuesta de Claude]**:
- IA con mejor estrategia de escondite: usa **señuelos** y puede mover una bandera una vez durante
  la partida.
- Más defensas alrededor de sus banderas.
- **Una habilidad especial por jefe**, ligada al terreno de su región. Ejemplos: congelar un
  espacio de arma del jugador durante un turno, o generar lava que daña a los robots que
  desembarcan.
- Cada jefe es un **comandante con personaje propio** (nombre, retrato y unas pocas frases de
  diálogo), totalmente original.

**Para que el jefe sea justo:** su habilidad se anuncia antes de la partida, la isla anterior
entrena para ese reto, el juego sugiere un arsenal adecuado y, después de 2 o 3 derrotas, ofrece
una ayuda opcional (por ejemplo, un dron extra al inicio). Al vencerlo se recibe una recompensa
especial: un arma o robot nuevo y una apariencia exclusiva.

### 5.2 IA
La implementación queda **[a criterio del constructor]**. Requisitos: debe esconder sus banderas
de forma creíble (no siempre en los mismos lugares), reaccionar a lo que revela el jugador y tener
niveles de dificultad configurables por isla. No debe hacer trampa leyendo información oculta del
jugador, salvo que se diseñe a propósito como habilidad de un jefe y se le anuncie al jugador.

## 6. Presentación visual

### 6.1 Dos vistas [decisión de Oscar]
Inspirado en las maquetas A (tropical) y B (neón) del 2026-11-09, el jugador puede alternar entre
dos vistas con un botón **REAL / HOLO**. Oscar eligió que **cada vista tenga un propósito
distinto**:

- **Vista Holográfica = planear.** Es "lo que ve tu robot o tu satélite": un océano oscuro con
  cuadrícula y elementos en neón (**cian** para lo propio, **rosa** para lo del rival). Muestra
  información táctica que la vista real no muestra: zonas de terreno por color, señales de
  posibles banderas, zonas sin explorar, la trayectoria y alcance del ataque, y el modificador de
  terreno de cada arma sobre el punto al que se apunta (por ejemplo, "−50 %").
- **Vista Real = ver el espectáculo.** Islas en 3D low-poly estilizado tropical brillante, con
  colores vivos, agua animada, luz de día y explosiones vistosas. **[Propuesta de Claude]:** al
  confirmar la jugada, la cámara cambia sola a la vista real y sigue al misil o al robot cruzando
  el mar hasta el impacto. Luego vuelve a la vista holográfica. La repetición del turno rival
  también se ve en vista real. Toda animación se puede saltar.
- **Transición** **[propuesta de Claude]:** un efecto de "escaneo", una línea de luz que barre la
  pantalla y convierte el mundo en holograma (y viceversa). Es un elemento de identidad del juego.

### 6.2 Estilo y producción del arte [propuesta de Claude]
- 3D low-poly estilizado con cámara isométrica o semicenital, orientación **vertical**, como en
  las maquetas.
- Se parte de paquetes de modelos con **licencia comercial** (Unity Asset Store) para el terreno,
  el agua y la vegetación. Armas, robots, banderas, comandantes, efectos e interfaz se diseñan a
  medida para darle identidad. Hay que guardar el registro de las licencias de cada recurso
  comprado.
- Debe correr fluido en celulares de gama media. El objetivo concreto de dispositivo y de
  cuadros por segundo queda **[a criterio del constructor]**, con un mínimo sugerido de 30 FPS
  estables.

### 6.3 Accesibilidad [lineamientos de Oscar, WCAG 2.1 AA adaptado a juego]
- Ninguna información se transmite solo por color: las zonas de terreno y los bandos usan además
  íconos, patrones o etiquetas (por el daltonismo en cian y rosa).
- Contraste de texto ≥ 4,5:1, tamaño de texto ajustable y áreas táctiles de al menos 44×44 pt.
- Etiquetas de accesibilidad para lectores de pantalla en los menús.
- Opción para reducir los destellos y el movimiento de cámara.

### 6.4 Idiomas [propuesta de Claude]
Español e inglés desde el lanzamiento.

## 7. Plataformas y tecnología [propuesta de Claude; Oscar delegó la elección]
- **Motor:** Unity 6 (versión LTS vigente, fijada al crear el proyecto) con **C#** y el Universal
  Render Pipeline (URP).
- **Plataformas:** solo **Android** por ahora **[decisión de Oscar, 2026-11-09]**. No usar APIs
  exclusivas de Android en la lógica del juego, para que agregar iOS después no exija reescribirla.
- **Cuando se retome iOS:** hará falta una Mac o un servicio de compilación en la nube (Unity Build
  Automation o una Mac alquilada), además de una cuenta de Apple Developer (USD 99/año).
- **Datos:** la campaña funciona **sin conexión**. El progreso se guarda localmente, con respaldo
  opcional en la nube mediante Google Play Games Services o Game Center **[propuesta de Claude]**.
- **Dependencias:** cada paquete o plugin nuevo se verifica en su registro oficial (Unity
  Registry, Asset Store o NuGet/OpenUPM) y se fija en una versión concreta **[lineamientos]**.

## 8. Calidad, estilo y proceso [lineamientos de Oscar]
- **Proyecto personal:** no aplica Docker y se trabaja directamente en la rama `main`
  (Conventional Commits, `Co-Authored-By` para la asistencia de IA, y SemVer con changelog).
- **Excepción de nombres por lenguaje (avisada a Oscar):** se siguen las convenciones oficiales de
  C# y Unity: archivos y clases en PascalCase (Unity exige que el archivo se llame igual que la
  clase), métodos y propiedades en PascalCase, constantes en PascalCase y campos privados con
  `_camelCase`. Esto contradice las reglas generales de kebab-case y SCREAMING_SNAKE_CASE. Se debe
  dejar documentado en el README.
- **Linter y formateador:** `dotnet format` con analizadores Roslyn y StyleCop, configurados en
  `.editorconfig`.
- **Pruebas:** Unity Test Framework (EditMode para la lógica y PlayMode para la integración).
  Cobertura mínima: **60 % en general** y **80 % en las compras dentro de la app y el manejo de
  datos personales**. La lógica de reglas (sección 11) es pura y fácil de probar, y debe tener
  buena cobertura.
- **CI:** lint, compilación, pruebas y análisis de dependencias en cada cambio. La herramienta
  queda **[a criterio del constructor]**; por ejemplo, GitHub Actions con GameCI.
- **README** con las once secciones obligatorias de los lineamientos.

## 9. Seguridad y aspectos legales
- Sin credenciales en el código; llaves de servicios (anuncios, analítica) fuera del repositorio.
- **Política de privacidad** conforme a la Ley 1581 de 2012 y a las exigencias de Google Play y
  App Store (formularios de seguridad de datos y etiquetas de privacidad).
- **Consentimiento para anuncios** (por ejemplo, Google UMP) y transparencia de seguimiento en iOS
  (ATT).
- **Público objetivo: mayores de 13 años [decisión de Oscar].** El juego no se inscribe en
  programas para niños (como la política de familias de Google), y la clasificación por edad en
  las tiendas se configura acorde. Se pueden usar anuncios personalizados con el consentimiento
  correspondiente. Aun así, el contenido visual se mantiene apto para todo público.
- Validación de compras con los mecanismos oficiales de cada tienda.
- Logs sin datos personales.

## 10. Monetización [propuesta de Claude, aceptada por Oscar]
- **Gratis para descargar.**
- Se venden **solo apariencias**: aspectos de robots, estelas de misiles, banderas personalizadas
  y temas de isla. Las apariencias exclusivas de jefes se ganan, no se compran.
- **Anuncios opcionales con recompensa** ("mira un video y recibe monedas cosméticas"). Nunca
  anuncios forzados durante una partida.
- **Sin contenido aleatorio pagado [propuesta de Claude]:** las apariencias se compran
  directamente en la tienda, viendo exactamente qué se recibe (con dinero real o con monedas
  cosméticas). No hay cofres aleatorios a la venta. Así se evitan las reglas de "loot boxes"
  (publicar probabilidades, prohibiciones en algunos países) y se protege al público joven.
- **Pago único para quitar los anuncios** (los anuncios con recompensa siguen disponibles si el
  jugador los quiere).
- El **pase de temporada** llega con el multijugador, no en la 1.0.
- La retención se apoya en el tutorial jugable, las estrellas por isla y, más adelante, misiones
  diarias.

## 11. Preparación para el multijugador futuro [propuesta de Claude]
Aunque la 1.0 es para un jugador, el código debe permitir agregar después el multijugador
(asíncrono en línea, local cercano y "pasa el celular") sin reescribir el juego:

- **Simulación separada de la presentación:** las reglas (isla, terreno, banderas, energía,
  acciones, resolución) viven en C# puro, sin depender de Unity, y son **deterministas** (la
  misma entrada produce el mismo resultado, con semilla aleatoria explícita).
- **Turnos como lista de comandos:** cada turno es una lista serializable de acciones. Eso permite
  repeticiones, enviar turnos por red o Bluetooth y validarlos en un servidor en el futuro.
- La IA es un "jugador" más que produce comandos, igual que lo hará un jugador remoto.
- **Información oculta:** cada jugador solo recibe lo que puede ver. En el futuro multijugador en
  línea, la posición real de las banderas no debe viajar al dispositivo rival.

Notas para la fase de multijugador (fuera de la 1.0): el modo local usará Bluetooth entre
dispositivos del mismo sistema y Wi-Fi local o hotspot entre Android e iPhone, detrás de un solo
botón "Jugar con alguien cerca"; el multijugador en línea será asíncrono, con notificaciones.

## 12. Lo que queda a criterio del constructor
- La forma y el tamaño de la cuadrícula, y el tamaño de las islas.
- La lista concreta de armas y robots y todos los valores numéricos de equilibrio (dentro de las
  reglas de la sección 4.4).
- La técnica de la IA.
- Qué paquetes de arte comprar (con licencia comercial registrada).
- Las herramientas de CI, analítica y servicio de anuncios (respetando la privacidad).
- El orden interno de construcción, siempre que se pueda jugar una isla completa lo antes posible
  para hacer pruebas de juego.

## 13. Equipo, presupuesto y etapas

- **Equipo [decisión de Oscar]:** Oscar solo, con asistencia de IA (Claude Code). No hay artista
  ni otros desarrolladores.
- **Presupuesto y fecha:** no se definieron. **[Propuesta de Claude]:** se asume un presupuesto
  bajo (paquetes de arte de la Asset Store y cuenta de Google Play Console, USD 25 de pago único) y
  ninguna fecha fija. Sin costos de Apple mientras iOS esté pospuesto.
- **Consecuencia para el arte [propuesta de Claude]:** como no hay artista, el arte "a medida" se
  logra **ensamblando y recoloreando** piezas de paquetes con licencia comercial (robots y armas
  modulares), con efectos y la vista holográfica hechos con shaders, y con una interfaz propia.
  Los retratos de los cuatro comandantes jefe pueden encargarse a un ilustrador independiente o
  hacerse con herramientas cuya licencia permita uso comercial. Antes de usar cualquier recurso
  hay que verificar y registrar su licencia.
- **Etapas internas [propuesta de Claude]:** el alcance de la 1.0 sigue siendo de 20 islas, pero
  se construye por hitos verificables:
  1. **Prototipo de reglas:** una isla jugable contra la IA, en la vista holográfica y con
     gráficos provisionales, para probar si el juego divierte.
  2. **Rebanada vertical:** la región 1 completa (5 islas con su jefe), con la vista real, la
     transición, el tutorial y el guardado.
  3. **Contenido:** las regiones 2 a 4, los idiomas y el modo Difícil.
  4. **Producto:** tienda de apariencias, anuncios, privacidad, accesibilidad, rendimiento y
     publicación en Google Play.

  Si la etapa 1 muestra que las reglas no divierten, se ajustan antes de invertir en arte.

## 14. Preguntas abiertas
- **Verificación oficial del nombre "Flagstorm"** (SIC, USPTO, tiendas, dominio).
