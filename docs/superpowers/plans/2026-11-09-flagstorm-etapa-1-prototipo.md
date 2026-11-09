# Flagstorm, etapa 1 (prototipo de reglas): plan de implementación

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Construir una isla jugable de Flagstorm contra la IA, en vista holográfica con gráficos
provisionales, para comprobar si las reglas divierten antes de invertir en arte.

**Architecture:** Las reglas viven en un núcleo de C# puro (`Flagstorm.Core`), determinista y sin
dependencias de Unity. Se publica como paquete local de Unity y se prueba con `dotnet test` fuera
de Unity. Cada acción del jugador es un comando que el `Match` valida y aplica, y cada comando
produce eventos. La IA solo recibe una `PlayerView` sin información oculta. Unity (tareas 10 a 12)
dibuja la vista holográfica y traduce toques de pantalla en comandos.

**Tech Stack:** C# 9 / .NET Standard 2.1 (núcleo), .NET SDK 10.0.401 (pruebas), NUnit 4.6.1,
NUnit3TestAdapter 6.3.1, Microsoft.NET.Test.Sdk 18.11.0, coverlet.collector 10.1.0,
StyleCop.Analyzers 1.2.0-beta.556, Unity 6 LTS + URP + UI Toolkit + Input System.

**Spec:** `docs/superpowers/specs/2026-11-09-flagstorm-design.md` (secciones 4, 5.2, 6.1, 6.3, 7, 8 y 11).

## Global Constraints

- Núcleo: `netstandard2.1`, `LangVersion 9.0` (el C# que compila Unity 6). Sin APIs de Unity y sin
  `System.Random` (su algoritmo no está garantizado entre runtimes; se usa `SeededRandom`).
- Nombres según C#/Unity (excepción aprobada en el spec, §8): archivos y tipos en PascalCase y
  campos privados `_camelCase`. Los ids de datos (armas) van en kebab-case, sin tildes ni ñ.
- Lógica determinista: el mismo estado y los mismos comandos producen el mismo resultado. Sin
  `DateTime.Now`, sin recorrer `HashSet`/`Dictionary` sin ordenar al producir salidas.
- Dependencias con versión fijada exactamente como en el Tech Stack. Toda dependencia nueva se
  verifica primero en nuget.org o en el Unity Registry.
- Nunca se venden ventajas: el catálogo de armas es dato de juego, no de tienda (anti-meta del spec).
- Accesibilidad (§6.3): ninguna información se transmite solo por color. Cada terreno y cada bando
  llevan además una etiqueta o un ícono.
- Todo el texto visible está en español en esta etapa (inglés en etapas posteriores).
- `dotnet format --verify-no-changes` y `dotnet build -warnaserror` en verde antes de cada commit.
- Commits en `main`, Conventional Commits en español, terminados con
  `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`. Sin push (no hay remoto).
- Cobertura de línea del núcleo ≥ 60 % (lineamientos). Esta etapa no tiene compras ni datos personales.

## Review Focus

1. **Comandos con coordenadas fuera de la isla o sobre la isla equivocada:** se rechazan con
   `OutOfIsland`, sin gastar energía ni cambiar el estado (tareas 5 y 7).
2. **Gastar más energía de la disponible:** se rechaza con `NotEnoughEnergy` y la energía no
   cambia (tarea 4).
3. **Ruta de robot no adyacente o más larga que su movimiento:** se rechaza **completa**, el robot
   no se mueve y no captura nada (tarea 6).
4. **Comandos fuera de turno, en la fase equivocada o con la partida terminada:** se rechazan con
   `NotYourTurn`, `WrongPhase` o `MatchFinished` (tareas 4 y 6).
5. **Fuga de información oculta:** la `PlayerView` nunca expone banderas, señuelos ni torretas del
   rival en celdas no reveladas, y una señal no distingue entre bandera y señuelo (tareas 5 y 7).

---

## Estructura de archivos

```
.editorconfig                                  estilo + severidades de analizadores
stylecop.json                                  configuración de StyleCop
.gitignore                                     dotnet + Unity + carpetas locales
README.md                                      11 secciones de los lineamientos
Flagstorm.sln
packages/com.flagstorm.core/                   paquete local de Unity (fuente única del núcleo)
  package.json
  Runtime/Flagstorm.Core.asmdef                noEngineReferences: true
  Runtime/Internal/IsExternalInit.cs           shim para `init` y records en netstandard2.1
  Runtime/Map/HexCoord.cs, TerrainType.cs, IslandMap.cs, IslandLayoutParser.cs,
              PrototypeIslands.cs, PathFinder.cs
  Runtime/Arsenal/WeaponCategory.cs, WeaponDefinition.cs, PrototypeArsenal.cs
  Runtime/Match/Side.cs, MatchPhase.cs, RulesConfig.cs, RejectionReason.cs, CommandResult.cs,
                Commands.cs, Events.cs, Match.cs, MatchState*.cs (partials por responsabilidad),
                PlayerView.cs, RobotInfo.cs, StateHasher.cs, MatchReplayer.cs
  Runtime/Ai/IRandomSource.cs, SeededRandom.cs, BasicAiPlayer.cs
src/Flagstorm.Core/Flagstorm.Core.csproj       compila los .cs de packages/com.flagstorm.core/Runtime
tests/Flagstorm.Core.Tests/                    NUnit; Support/TestIslands.cs, Support/MatchBuilder.cs
unity/Flagstorm/                               proyecto Unity (tarea 10)
  Assets/Flagstorm/Presentation/*.cs + Flagstorm.Presentation.asmdef
  Assets/Flagstorm/Tests/EditMode/*.cs + Flagstorm.Presentation.Tests.asmdef
  Assets/Flagstorm/Scenes/PrototypeHolo.unity
  Assets/Flagstorm/UI/Hud.uxml, Hud.uss
docs/playtests/2026-xx-xx-prototipo.md         notas de la prueba de juego (tarea 12)
```

`Match` se reparte en clases parciales para mantener archivos enfocados: `Match.cs` (fases, turnos,
energía y despacho), `MatchState.Reveal.cs` (drones y misiles), `MatchState.Robots.cs` (robots y
capturas) y `MatchState.Defense.cs` (defensas y disparo de torretas).

---

### Task 1: Andamiaje de la solución, estilo, README y `HexCoord`

**Files:**
- Create: `Flagstorm.sln`, `src/Flagstorm.Core/Flagstorm.Core.csproj`,
  `tests/Flagstorm.Core.Tests/Flagstorm.Core.Tests.csproj`, `.editorconfig`, `stylecop.json`,
  `README.md`, `packages/com.flagstorm.core/package.json`,
  `packages/com.flagstorm.core/Runtime/Flagstorm.Core.asmdef`,
  `packages/com.flagstorm.core/Runtime/Internal/IsExternalInit.cs`,
  `packages/com.flagstorm.core/Runtime/Map/HexCoord.cs`
- Modify: `.gitignore` (agregar `bin/`, `obj/`, `TestResults/`, y para Unity `unity/Flagstorm/[Ll]ibrary/`,
  `[Tt]emp/`, `[Oo]bj/`, `[Bb]uild*/`, `[Ll]ogs/`, `[Uu]ser[Ss]ettings/`, `*.csproj` y `*.sln` dentro de `unity/Flagstorm/`)
- Test: `tests/Flagstorm.Core.Tests/Map/HexCoordTests.cs`

**Interfaces:**
- Produces: `readonly record struct HexCoord(int Q, int R)` (coordenadas axiales, hex "pointy-top"):
  `int DistanceTo(HexCoord other)`; `IReadOnlyList<HexCoord> Neighbors()` en el orden fijo
  E(+1,0), NE(+1,−1), NO(0,−1), O(−1,0), SO(−1,+1), SE(0,+1);
  `static IEnumerable<HexCoord> WithinRadius(HexCoord center, int radius)` (incluye el centro y
  sale ordenado por R y luego por Q).

- [ ] **Step 1: Crear los proyectos**

`src/Flagstorm.Core/Flagstorm.Core.csproj`: `netstandard2.1`, `LangVersion 9.0`,
`TreatWarningsAsErrors true`, `EnableDefaultCompileItems false`,
`<Compile Include="../../packages/com.flagstorm.core/Runtime/**/*.cs" />`,
`StyleCop.Analyzers 1.2.0-beta.556` con `PrivateAssets all`, y `stylecop.json` como `AdditionalFiles`.
Los tests: `net10.0` con NUnit 4.6.1, NUnit3TestAdapter 6.3.1, Microsoft.NET.Test.Sdk 18.11.0,
coverlet.collector 10.1.0 y una referencia al proyecto del núcleo.
`package.json`: `{"name":"com.flagstorm.core","version":"0.1.0","displayName":"Flagstorm Core","unity":"6000.0"}`.
`Flagstorm.Core.asmdef`: `{"name":"Flagstorm.Core","rootNamespace":"Flagstorm.Core","noEngineReferences":true}`.
`.editorconfig`: indentación de 4 espacios, `max_line_length = 120`, `_camelCase` para campos
privados, y estas reglas en `none`: SA1101 (prefijo `this.`), SA1309 (guion bajo), SA1600, SA1602
y SA1633 (documentación y encabezados obligatorios). El porqué va en comentarios donde haga falta.

- [ ] **Step 2: Escribir la prueba que falla**

```csharp
[TestFixture]
public class HexCoordTests
{
    [Test]
    public void DistanceTo_IsHexDistance()
    {
        Assert.That(new HexCoord(0, 0).DistanceTo(new HexCoord(2, -1)), Is.EqualTo(2));
        Assert.That(new HexCoord(1, 1).DistanceTo(new HexCoord(-1, 3)), Is.EqualTo(2));
        Assert.That(new HexCoord(3, 0).DistanceTo(new HexCoord(3, 0)), Is.EqualTo(0));
    }

    [Test]
    public void Neighbors_AreSixInFixedOrder()
    {
        Assert.That(new HexCoord(1, 1).Neighbors(), Is.EqualTo(new[]
        {
            new HexCoord(2, 1), new HexCoord(2, 0), new HexCoord(1, 0),
            new HexCoord(0, 1), new HexCoord(0, 2), new HexCoord(1, 2),
        }));
    }

    [Test]
    public void WithinRadius_OneReturnsCenterAndNeighborsSorted()
    {
        var cells = HexCoord.WithinRadius(new HexCoord(0, 0), 1).ToList();
        Assert.That(cells, Has.Count.EqualTo(7));
        Assert.That(cells, Is.Ordered.By(nameof(HexCoord.R)).Then.By(nameof(HexCoord.Q)));
        Assert.That(HexCoord.WithinRadius(new HexCoord(5, 5), 0), Is.EqualTo(new[] { new HexCoord(5, 5) }));
    }
}
```

- [ ] **Step 3: Ejecutar y verificar que falla**

Run: `dotnet test tests/Flagstorm.Core.Tests --filter HexCoordTests`
Expected: falla de compilación, `HexCoord` no existe.

- [ ] **Step 4: Implementar `HexCoord`** en `Runtime/Map/HexCoord.cs`, junto con el shim
`namespace System.Runtime.CompilerServices { internal static class IsExternalInit { } }`.
La distancia es `(|dq| + |dr| + |dq+dr|) / 2`.

- [ ] **Step 5: Escribir el README con las 11 secciones de los lineamientos**

Las secciones son: nombre y descripción, enlaces de documentación (el spec y este plan),
referencias de API ("No aplica"), autores (Oscar Dueñas con asistencia de Claude Code),
distintivos ("No aplica"), ambiente de pruebas (`dotnet test`), deployment ("No aplica en esta
etapa"), environment variables ("No aplica"), screenshots ("Pendiente, tarea 12"), tech stack
(con versiones) y usage/examples. Incluir la nota de excepciones: nombres de C#/Unity en
PascalCase, y sin Docker ni varias ramas por ser un proyecto personal.

- [ ] **Step 6: Verificar todo**

Run: `dotnet test` → PASS (3 pruebas); `dotnet format --verify-no-changes` → sin salida y código 0;
`dotnet build -warnaserror` → 0 advertencias.

- [ ] **Step 7: Commit** — `chore: crea solucion del nucleo con HexCoord y README`

---

### Task 2: Terreno, mapa de isla, parser de diseños y búsqueda de rutas

**Files:**
- Create: `Runtime/Map/TerrainType.cs`, `IslandMap.cs`, `IslandLayoutParser.cs`,
  `PrototypeIslands.cs`, `PathFinder.cs`, `tests/Flagstorm.Core.Tests/Support/TestIslands.cs`
- Test: `tests/Flagstorm.Core.Tests/Map/IslandLayoutParserTests.cs`, `Map/PathFinderTests.cs`

**Interfaces:**
- Consumes: `HexCoord`.
- Produces:
  - `enum TerrainType { Beach, Jungle, Ice, Volcano, Rock }`.
  - `sealed class IslandMap`: `IReadOnlyDictionary<HexCoord, TerrainType> Cells`,
    `bool Contains(HexCoord)`, `TerrainType TerrainAt(HexCoord)` (lanza
    `ArgumentOutOfRangeException` si la celda no es tierra), `bool IsCoast(HexCoord)` (si algún
    vecino no es tierra), `IReadOnlyList<HexCoord> CoastCells` e `InteriorCells` (ordenadas por R
    y luego por Q).
  - `static class IslandLayoutParser`: `IslandMap Parse(IReadOnlyList<string> rows)`. Los
    caracteres son `B J I V R` para los terrenos y `.` para el agua. La fila es R y la columna se
    convierte con offset "odd-r": `Q = col - (row - (row & 1)) / 2`. Un carácter desconocido lanza
    `FormatException` con un mensaje que incluye la fila y la columna.
  - `static class PrototypeIslands`: `IReadOnlyList<string> DefaultLayout`, `IslandMap CreateDefault()`.
  - `static class PathFinder`:
    `IReadOnlyList<HexCoord> FindPath(IslandMap map, WeaponDefinition robot, HexCoord from, HexCoord to)`
    devuelve la ruta más barata, sin la celda de inicio, o `null` si no hay ruta.
    `int PathCost(IslandMap map, WeaponDefinition robot, IReadOnlyList<HexCoord> path)`.
    `IReadOnlyList<HexCoord> TruncateToBudget(IslandMap, WeaponDefinition, IReadOnlyList<HexCoord> path, int budget)`.
    **Nota:** `WeaponDefinition` llega en la tarea 3. En esta tarea `PathFinder` recibe un
    `Func<TerrainType, int> movementCost`, y la tarea 3 agrega las sobrecargas con `WeaponDefinition`.
- `TestIslands.Small` (5×5), usada por todas las pruebas de partida:
  ```
  "BBBBB",
  "BJJIB",
  "BJVIB",
  "BRRIB",
  "BBBBB",
  ```
  Celdas interiores (9): (1,1)J (2,1)J (3,1)I (0,2)J (1,2)V (2,2)I (0,3)R (1,3)R (2,3)I. Las otras
  16 son costa.

- [ ] **Step 1: Escribir las pruebas que fallan**

```csharp
[Test]
public void Parse_SmallIsland_MapsOddRowOffsetToAxial()
{
    var map = IslandLayoutParser.Parse(TestIslands.SmallLayout);
    Assert.That(map.Cells, Has.Count.EqualTo(25));
    Assert.That(map.TerrainAt(new HexCoord(1, 1)), Is.EqualTo(TerrainType.Jungle));
    Assert.That(map.TerrainAt(new HexCoord(1, 2)), Is.EqualTo(TerrainType.Volcano));
    Assert.That(map.TerrainAt(new HexCoord(-2, 4)), Is.EqualTo(TerrainType.Beach));
    Assert.That(map.InteriorCells, Has.Count.EqualTo(9));
    Assert.That(map.IsCoast(new HexCoord(0, 1)), Is.True);
    Assert.That(map.IsCoast(new HexCoord(2, 2)), Is.False);
}

[Test]
public void Parse_UnknownChar_ThrowsWithPosition()
{
    var ex = Assert.Throws<FormatException>(() => IslandLayoutParser.Parse(new[] { "BB", "BX" }));
    Assert.That(ex.Message, Does.Contain("fila 1").And.Contain("columna 1"));
}

[Test]
public void Parse_WaterIsNotLand()
{
    var map = IslandLayoutParser.Parse(new[] { ".B", "B." });
    Assert.That(map.Contains(new HexCoord(0, 0)), Is.False);
    Assert.Throws<ArgumentOutOfRangeException>(() => map.TerrainAt(new HexCoord(0, 0)));
}

[Test]
public void DefaultPrototypeIsland_HasAllTerrainsAndRoomToHide()
{
    var map = PrototypeIslands.CreateDefault();
    Assert.That(map.Cells.Values.Distinct(), Is.EquivalentTo(Enum.GetValues(typeof(TerrainType))));
    Assert.That(map.InteriorCells, Has.Count.GreaterThanOrEqualTo(10));
}

// PathFinderTests (costo: Volcano 3, Jungle 2, resto 1)
[Test]
public void FindPath_PrefersCheaperDetour()
{
    var map = TestIslands.Small();
    var path = PathFinder.FindPath(map, Cost, new HexCoord(0, 2), new HexCoord(2, 2));
    Assert.That(path.Last(), Is.EqualTo(new HexCoord(2, 2)));
    Assert.That(path, Does.Not.Contain(new HexCoord(1, 2)));            // evita el volcán
    Assert.That(PathFinder.PathCost(map, Cost, path), Is.EqualTo(3));
}

[Test]
public void TruncateToBudget_StopsBeforeExceeding()
{
    var map = TestIslands.Small();
    var path = new[] { new HexCoord(1, 1), new HexCoord(1, 2) };          // 2 + 3
    Assert.That(PathFinder.TruncateToBudget(map, Cost, path, 3), Is.EqualTo(new[] { new HexCoord(1, 1) }));
}
```

- [ ] **Step 2: Ejecutar y verificar que fallan** — `dotnet test --filter "IslandLayoutParserTests|PathFinderTests"` → errores de compilación.

- [ ] **Step 3: Implementar.** `PrototypeIslands.DefaultLayout`:
```
".BBBBBB..",
"BBJJJRBB.",
"BJJVVRRIB",
"BJVVVRIIB",
"BBJJRRIIB",
".BBRRIIB.",
"..BBBBB..",
```
`FindPath` es Dijkstra con desempate determinista: a igual costo gana la celda descubierta primero,
y los vecinos se recorren en el orden fijo de `Neighbors()`.

- [ ] **Step 4: Ejecutar** — `dotnet test` → PASS; `dotnet format --verify-no-changes` → OK.

- [ ] **Step 5: Commit** — `feat: agrega mapa de isla, parser de diseños y busqueda de rutas`

---

### Task 3: Catálogo de armas con modificadores por terreno

**Files:**
- Create: `Runtime/Arsenal/WeaponCategory.cs`, `WeaponDefinition.cs`, `PrototypeArsenal.cs`
- Modify: `Runtime/Map/PathFinder.cs` (sobrecargas con `WeaponDefinition`)
- Test: `tests/Flagstorm.Core.Tests/Arsenal/WeaponDefinitionTests.cs`

**Interfaces:**
- Produces:
  - `enum WeaponCategory { Reveal, Destroy, Capture, Defend }`.
  - `sealed record WeaponDefinition(string Id, string DisplayName, WeaponCategory Category, int EnergyCost, int Power, int Radius, int HitPoints, int MovementPoints, IReadOnlyDictionary<TerrainType, int> PowerModifierPercent, IReadOnlyDictionary<TerrainType, int> MovementCost)`
    con `int ModifierPercentOn(TerrainType)` (0 si no está definido),
    `int EffectivePowerOn(TerrainType)` = `Power × (100 + mod) / 100`, redondeado con
    `MidpointRounding.AwayFromZero` y con mínimo 0, e `int MovementCostOn(TerrainType)` (1 si no
    está definido).
  - `static class PrototypeArsenal`: las constantes de id `Drone = "dron-explorador"`,
    `StandardMissile = "misil-estandar"`, `IncendiaryMissile = "misil-incendiario"`,
    `TrackedRobot = "robot-orugas"`, `SnowRobot = "robot-nieve"`, `Turret = "torreta"` y
    `Decoy = "senuelo"`, además de `IReadOnlyList<WeaponDefinition> All` y `WeaponDefinition Get(string id)`
    (lanza `KeyNotFoundException`).

Valores del prototipo (todos se pueden ajustar en la tarea 12):

| id | categoría | costo | poder | radio | PV | mov. | modificadores |
|---|---|---|---|---|---|---|---|
| dron-explorador | Reveal | 2 | 0 | 1 | 0 | 0 | — |
| misil-estandar | Destroy | 3 | 3 | 1 | 0 | 0 | — |
| misil-incendiario | Destroy | 3 | 3 | 1 | 0 | 0 | Jungle +40, Ice −50 (valores del spec) |
| robot-orugas | Capture | 4 | 0 | 0 | 4 | 3 | mov.: Beach 1, Rock 1, Jungle 1, Ice 2, Volcano 3 |
| robot-nieve | Capture | 4 | 0 | 0 | 3 | 3 | mov.: Ice 1, Beach 1, Rock 2, Jungle 2, Volcano 3 |
| torreta | Defend | 3 | 2 | 1 | 4 | 0 | — |
| senuelo | Defend | 2 | 0 | 0 | 1 | 0 | — |

- [ ] **Step 1: Escribir las pruebas que fallan**

```csharp
[Test]
public void IncendiaryMissile_UsesSpecTerrainModifiers()
{
    var missile = PrototypeArsenal.Get(PrototypeArsenal.IncendiaryMissile);
    Assert.That(missile.ModifierPercentOn(TerrainType.Jungle), Is.EqualTo(40));
    Assert.That(missile.ModifierPercentOn(TerrainType.Ice), Is.EqualTo(-50));
    Assert.That(missile.EffectivePowerOn(TerrainType.Jungle), Is.EqualTo(4));   // 3 × 1,4 = 4,2
    Assert.That(missile.EffectivePowerOn(TerrainType.Ice), Is.EqualTo(2));      // 1,5 → 2
    Assert.That(missile.EffectivePowerOn(TerrainType.Rock), Is.EqualTo(3));
}

[Test]
public void EveryTerrainHasAGoodRobot()
{
    var robots = PrototypeArsenal.All.Where(w => w.Category == WeaponCategory.Capture).ToList();
    foreach (TerrainType terrain in Enum.GetValues(typeof(TerrainType)))
    {
        if (terrain == TerrainType.Volcano) { continue; } // sin robot volcánico hasta la región 3 (spec §5)
        Assert.That(robots.Any(r => r.MovementCostOn(terrain) == 1), terrain.ToString());
    }
}

[Test]
public void NoWeaponIsBestEverywhere()
{
    var destroyers = PrototypeArsenal.All.Where(w => w.Category == WeaponCategory.Destroy).ToList();
    var terrains = Enum.GetValues(typeof(TerrainType)).Cast<TerrainType>();
    foreach (var weapon in destroyers)
    {
        var bestEverywhere = terrains.All(t => destroyers.All(o => weapon.EffectivePowerOn(t) >= o.EffectivePowerOn(t)));
        Assert.That(bestEverywhere, Is.False, weapon.Id);
    }
}

[Test]
public void Get_UnknownId_Throws() =>
    Assert.Throws<KeyNotFoundException>(() => PrototypeArsenal.Get("no-existe"));
```

Con estos valores el incendiario gana en selva (4 contra 3) y el estándar en hielo (3 contra 2),
y los robots se reparten igual: orugas en selva, roca y playa; nieve en hielo. Por eso las dos
pruebas de la regla §4.4 del spec pasan. Si la tarea 12 ajusta valores, deben seguir pasando.

- [ ] **Step 2: Ejecutar y verificar que fallan.**
- [ ] **Step 3: Implementar**, junto con las sobrecargas de `PathFinder` que delegan en `robot.MovementCostOn`.
- [ ] **Step 4: Ejecutar** — `dotnet test` → PASS; formato OK.
- [ ] **Step 5: Commit** — `feat: agrega catalogo de armas con modificadores por terreno`

---

### Task 4: Partida: preparación, turnos, energía y rechazos

**Files:**
- Create: `Runtime/Match/Side.cs`, `MatchPhase.cs`, `RulesConfig.cs`, `RejectionReason.cs`,
  `CommandResult.cs`, `Commands.cs`, `Events.cs`, `Match.cs`, `PlayerView.cs`, `RobotInfo.cs`,
  `tests/Flagstorm.Core.Tests/Support/MatchBuilder.cs`
- Test: `tests/Flagstorm.Core.Tests/Match/MatchSetupAndTurnTests.cs`

**Interfaces:**
- Produces:
  - `enum Side { Blue, Red }` (Blue es el jugador humano en la campaña) y `Side Opponent(this Side)`.
  - `enum MatchPhase { Setup, Playing, Finished }`.
  - `sealed record RulesConfig(int FlagsPerSide, int EnergyPerTurn, int MaxEnergy, int InitialFreeTurrets)`
    con `static RulesConfig Default => new(5, 6, 10, 2)`.
  - `enum RejectionReason { None, WrongPhase, NotYourTurn, MatchFinished, InvalidSetup, OutOfIsland, NotEnoughEnergy, UnknownWeapon, WrongCategory, NotCoast, InvalidPath, UnknownRobot, CellOccupied }`.
  - `sealed record CommandResult(bool IsAccepted, RejectionReason Reason, IReadOnlyList<IMatchEvent> Events)`
    con `static Accepted(IReadOnlyList<IMatchEvent>)` y `static Rejected(RejectionReason)`.
  - `interface IMatchCommand`, con los records `SetupCommand(IReadOnlyList<HexCoord> Flags, IReadOnlyList<HexCoord> Turrets)`,
    `LaunchDroneCommand(string WeaponId, HexCoord Target)`, `FireMissileCommand(string WeaponId, HexCoord Target)`,
    `DeployRobotCommand(string WeaponId, HexCoord LandingCell)`, `MoveRobotCommand(int RobotId, IReadOnlyList<HexCoord> Path)`,
    `BuildDefenseCommand(string WeaponId, HexCoord Cell)` y `EndTurnCommand()`.
  - `interface IMatchEvent`, con los records `SetupCompleted(Side Side)`, `MatchStarted(Side FirstSide)`,
    `TurnEnded(Side NextSide, int TurnNumber)`, `EnergyChanged(Side Side, int Energy)`. Las
    tareas 5 a 7 agregan sus eventos en `Events.cs`.
  - `sealed class Match`: `Match(IslandMap blueIsland, IslandMap redIsland, RulesConfig rules, IReadOnlyList<WeaponDefinition> arsenal)`;
    `MatchPhase Phase`, `Side CurrentSide`, `Side? Winner`, `int TurnNumber` (empieza en 1 y
    aumenta en cada `EndTurn`), `int EnergyOf(Side)`, `RulesConfig Rules`,
    `IslandMap IslandOf(Side)`, `CommandResult Submit(Side side, IMatchCommand command)`,
    `PlayerView ViewFor(Side side)` e `IReadOnlyList<RecordedCommand> History` (solo los comandos
    aceptados). `record RecordedCommand(Side Side, IMatchCommand Command)`.
  - `sealed class PlayerView`: `Side Viewer`, `MatchPhase Phase`, `Side CurrentSide`,
    `int TurnNumber`, `int Energy`, `RulesConfig Rules`, `IslandMap OwnIsland`, `IslandMap EnemyIsland`,
    `IReadOnlyCollection<HexCoord> OwnFlags`, `OwnDecoys`, `IReadOnlyDictionary<HexCoord, int> OwnTurrets` (PV),
    `IReadOnlyCollection<HexCoord> RevealedEnemyCells`, `EnemySignals`,
    `IReadOnlyDictionary<HexCoord, int> KnownEnemyTurrets`, `IReadOnlyList<RobotInfo> OwnRobots`,
    `EnemyRobotsOnOwnIsland`, `int FlagsCapturedByViewer`, `int FlagsLostByViewer`. Las colecciones
    salen ordenadas por R y luego por Q.
  - `sealed record RobotInfo(int RobotId, string WeaponId, HexCoord Position, int HitPoints, int MovementPointsLeft)`.
  - Test support: `MatchBuilder.NewSetup(RulesConfig rules = null)` crea un `Match` con
    `TestIslands.Small()` para ambos bandos y `PrototypeArsenal.All`.
    `MatchBuilder.NewPlaying(RulesConfig rules = null)` además envía para ambos bandos el
    `SetupCommand` con `DefaultFlags = (1,1),(2,1),(1,2),(2,2),(0,3)` y
    `DefaultTurrets = (3,1),(0,2)` (con `FlagsPerSide = 1` usa solo `(1,1)`).

Reglas: un bando puede enviar `SetupCommand` en `Setup` una sola vez. Es válido si las banderas
son exactamente `FlagsPerSide` celdas de tierra distintas de su isla, las torretas son como mucho
`InitialFreeTurrets` celdas de tierra distintas sin bandera, y ninguna se repite. Si no, se rechaza
con `InvalidSetup`. Cuando ambos bandos terminan, `Phase = Playing`, `CurrentSide = Blue` y Blue
recibe energía. Al empezar el turno de un bando: `energía = min(MaxEnergy, energía + EnergyPerTurn)`.
Orden de validación de cualquier comando: `MatchFinished` → `WrongPhase` → `NotYourTurn` → los
propios del comando. Un comando rechazado no cambia nada.

- [ ] **Step 1: Escribir las pruebas que fallan**

```csharp
[Test]
public void BothSetups_StartMatchWithBlueAndInitialEnergy()
{
    var match = MatchBuilder.NewPlaying();
    Assert.That(match.Phase, Is.EqualTo(MatchPhase.Playing));
    Assert.That(match.CurrentSide, Is.EqualTo(Side.Blue));
    Assert.That(match.EnergyOf(Side.Blue), Is.EqualTo(6));
    Assert.That(match.EnergyOf(Side.Red), Is.EqualTo(0));
}

[TestCase("duplicada")] [TestCase("agua")] [TestCase("cuatro")] [TestCase("torretas")] [TestCase("torreta-sobre-bandera")]
public void InvalidSetup_IsRejectedWithoutChangingPhase(string caseName)
{
    var match = MatchBuilder.NewSetup();
    // InvalidSetups.For (helper local): "duplicada" repite (1,1); "agua" usa (10,10); "cuatro" da 4 banderas;
    // "torretas" da 3 torretas; "torreta-sobre-bandera" pone una torreta en (1,1).
    var result = match.Submit(Side.Blue, InvalidSetups.For(caseName));
    Assert.That(result.Reason, Is.EqualTo(RejectionReason.InvalidSetup));
    Assert.That(match.Phase, Is.EqualTo(MatchPhase.Setup));
}

[Test]
public void CommandsOutsideTurnOrPhase_AreRejected()
{
    var setup = MatchBuilder.NewSetup();
    Assert.That(setup.Submit(Side.Blue, new EndTurnCommand()).Reason, Is.EqualTo(RejectionReason.WrongPhase));
    var match = MatchBuilder.NewPlaying();
    Assert.That(match.Submit(Side.Red, new EndTurnCommand()).Reason, Is.EqualTo(RejectionReason.NotYourTurn));
    Assert.That(match.Submit(Side.Blue, new SetupCommand(MatchBuilder.DefaultFlags, new HexCoord[0])).Reason,
        Is.EqualTo(RejectionReason.WrongPhase));
}

[Test]
public void UnspentEnergy_CarriesOverUpToMax()
{
    var match = MatchBuilder.NewPlaying();
    match.Submit(Side.Blue, new EndTurnCommand());
    match.Submit(Side.Red, new EndTurnCommand());
    Assert.That(match.EnergyOf(Side.Blue), Is.EqualTo(10)); // min(10, 6 + 6)
    Assert.That(match.TurnNumber, Is.EqualTo(3));
}

[Test]
public void RejectedCommands_AreNotRecordedInHistory()
{
    var match = MatchBuilder.NewPlaying();
    var before = match.History.Count;
    match.Submit(Side.Red, new EndTurnCommand());
    Assert.That(match.History.Count, Is.EqualTo(before));
}
```

La prueba de `NotEnoughEnergy` (Review Focus 2) va en la tarea 5, que es la primera con
acciones que cuestan energía.

- [ ] **Step 2: Ejecutar y verificar que fallan.**
- [ ] **Step 3: Implementar** `Match` con un despacho `switch` por tipo de comando hacia métodos
  privados (`ApplySetup`, `ApplyEndTurn`). Las tareas 5 a 7 agregan sus métodos en las partials.
  Los comandos que aún no existen devuelven `WrongCategory` de forma provisional, y la tarea que
  los implementa elimina esa rama.
- [ ] **Step 4: Ejecutar** — `dotnet test` → PASS; formato OK.
- [ ] **Step 5: Commit** — `feat: agrega partida con preparacion, turnos y energia`

---

### Task 5: Revelar y destruir: drones, misiles y niebla de guerra

**Files:**
- Create: `Runtime/Match/MatchState.Reveal.cs`
- Modify: `Runtime/Match/Match.cs` (despacho), `Events.cs`, `PlayerView.cs`
- Test: `tests/Flagstorm.Core.Tests/Match/RevealAndMissileTests.cs`

**Interfaces:**
- Consumes: `Match`, `PrototypeArsenal`, `WeaponDefinition.EffectivePowerOn`, `HexCoord.WithinRadius`.
- Produces events: `CellsRevealed(Side By, IReadOnlyList<HexCoord> Cells)`,
  `MissileImpact(Side By, string WeaponId, HexCoord Target, IReadOnlyList<HexCoord> Cells)`,
  `DefenseDamaged(Side Owner, HexCoord Cell, string WeaponId, int RemainingHitPoints)` y
  `DefenseDestroyed(Side Owner, HexCoord Cell, string WeaponId)`. El `WeaponId` de la defensa
  revela si era una torreta o un señuelo: es intencional, porque destruir algo te dice qué era.

Reglas: drones y misiles apuntan a la isla **rival**. Si `Target` no es tierra de esa isla →
`OutOfIsland`. Si el id no existe → `UnknownWeapon`. Si la categoría no corresponde →
`WrongCategory`. Si el costo es mayor que la energía → `NotEnoughEnergy`. El dron revela
`WithinRadius(Target, Radius)` filtrado a tierra. El misil aplica a cada defensa rival (torreta o
señuelo) dentro del radio un daño igual a `EffectivePowerOn(terreno de esa celda)`. Las banderas
son indestructibles. En la `PlayerView`, una celda revelada muestra su estado actual: `EnemySignals`
incluye las banderas **y** los señuelos sin distinguirlos, y `KnownEnemyTurrets` incluye las
torretas con sus PV.

- [ ] **Step 1: Escribir las pruebas que fallan**

```csharp
[Test]
public void NewMatch_ViewHidesEverythingOfTheEnemy()
{
    var view = MatchBuilder.NewPlaying().ViewFor(Side.Blue);
    Assert.That(view.RevealedEnemyCells, Is.Empty);
    Assert.That(view.EnemySignals, Is.Empty);
    Assert.That(view.KnownEnemyTurrets, Is.Empty);
    Assert.That(view.OwnFlags, Is.EquivalentTo(MatchBuilder.DefaultFlags));
}

[Test]
public void Drone_RevealsRadiusAndShowsSignalsAndTurrets()
{
    var match = MatchBuilder.NewPlaying();
    var result = match.Submit(Side.Blue, new LaunchDroneCommand(PrototypeArsenal.Drone, new HexCoord(1, 1)));
    Assert.That(result.IsAccepted, Is.True);
    var view = match.ViewFor(Side.Blue);
    Assert.That(view.RevealedEnemyCells, Has.Count.EqualTo(7));
    Assert.That(view.EnemySignals, Is.EquivalentTo(new[] { new HexCoord(1, 1), new HexCoord(2, 1), new HexCoord(1, 2) }));
    Assert.That(view.KnownEnemyTurrets.Keys, Is.EquivalentTo(new[] { new HexCoord(0, 2) }));
    Assert.That(match.EnergyOf(Side.Blue), Is.EqualTo(4));
}

[Test]
public void IncendiaryMissile_DamageDependsOnTerrain()
{
    var match = MatchBuilder.NewPlaying();
    var onIce = match.Submit(Side.Blue, new FireMissileCommand(PrototypeArsenal.IncendiaryMissile, new HexCoord(3, 1)));
    Assert.That(onIce.Events.OfType<DefenseDamaged>().Single().RemainingHitPoints, Is.EqualTo(2)); // 4 − 2
    var onJungle = match.Submit(Side.Blue, new FireMissileCommand(PrototypeArsenal.IncendiaryMissile, new HexCoord(0, 2)));
    Assert.That(onJungle.Events.OfType<DefenseDestroyed>().Single().Cell, Is.EqualTo(new HexCoord(0, 2))); // 4 ≥ 4
}

[Test]
public void NotEnoughEnergy_IsRejectedAndEnergyUnchanged()        // Review Focus 2
{
    var match = MatchBuilder.NewPlaying();
    match.Submit(Side.Blue, new FireMissileCommand(PrototypeArsenal.StandardMissile, new HexCoord(1, 1)));
    match.Submit(Side.Blue, new FireMissileCommand(PrototypeArsenal.StandardMissile, new HexCoord(1, 1)));
    var third = match.Submit(Side.Blue, new FireMissileCommand(PrototypeArsenal.StandardMissile, new HexCoord(1, 1)));
    Assert.That(third.Reason, Is.EqualTo(RejectionReason.NotEnoughEnergy));
    Assert.That(match.EnergyOf(Side.Blue), Is.EqualTo(0));
}

[Test]
public void TargetOutsideEnemyIsland_IsRejectedWithoutCost()     // Review Focus 1
{
    var match = MatchBuilder.NewPlaying();
    var result = match.Submit(Side.Blue, new LaunchDroneCommand(PrototypeArsenal.Drone, new HexCoord(10, 10)));
    Assert.That(result.Reason, Is.EqualTo(RejectionReason.OutOfIsland));
    Assert.That(match.EnergyOf(Side.Blue), Is.EqualTo(6));
}

[Test]
public void WrongCategoryAndUnknownWeapon_AreRejected()
{
    var match = MatchBuilder.NewPlaying();
    Assert.That(match.Submit(Side.Blue, new FireMissileCommand(PrototypeArsenal.Drone, new HexCoord(1, 1))).Reason,
        Is.EqualTo(RejectionReason.WrongCategory));
    Assert.That(match.Submit(Side.Blue, new LaunchDroneCommand("no-existe", new HexCoord(1, 1))).Reason,
        Is.EqualTo(RejectionReason.UnknownWeapon));
}
```

- [ ] **Step 2: Ejecutar y verificar que fallan.**
- [ ] **Step 3: Implementar** `ApplyDrone` y `ApplyMissile` en `MatchState.Reveal.cs`, y el cálculo de la niebla en `ViewFor`.
- [ ] **Step 4: Ejecutar** — `dotnet test` → PASS; formato OK.
- [ ] **Step 5: Commit** — `feat: agrega drones, misiles y niebla de guerra`

---

### Task 6: Robots, capturas y victoria

**Files:**
- Create: `Runtime/Match/MatchState.Robots.cs`
- Modify: `Match.cs`, `Events.cs`, `PlayerView.cs`
- Test: `tests/Flagstorm.Core.Tests/Match/RobotAndCaptureTests.cs`

**Interfaces:**
- Consumes: `PathFinder.PathCost`, `WeaponDefinition.MovementCostOn`, `IslandMap.IsCoast`.
- Produces events: `RobotDeployed(Side Owner, int RobotId, string WeaponId, HexCoord Cell)`,
  `RobotMoved(Side Owner, int RobotId, IReadOnlyList<HexCoord> Path)`,
  `FlagCaptured(Side By, HexCoord Cell, int TotalCaptured)`, `DecoyExposed(Side By, HexCoord Cell)` y
  `MatchWon(Side Winner)`. Los `RobotId` son enteros globales que empiezan en 1 y crecen en orden de despliegue.

Reglas: `DeployRobotCommand` exige una celda de **costa** de la isla rival (si no, `NotCoast`;
si no es tierra, `OutOfIsland`) sin otro robot (`CellOccupied`). El robot aparece con el PV de su
definición y su movimiento completo. `MoveRobotCommand` exige un robot propio y vivo
(`UnknownRobot`) y una ruta no vacía de celdas de tierra adyacentes paso a paso, cuyo costo total no
supere el movimiento restante. Cualquier violación da `InvalidPath`, y el comando entero se rechaza.
Al entrar en una celda con bandera rival: se captura (se elimina) y, si `TotalCaptured == FlagsPerSide`,
la partida pasa a `Finished` con `Winner`, se emite `MatchWon` y el resto de la ruta se ignora. Al
entrar en una celda con señuelo: se emite `DecoyExposed` y el señuelo se elimina. Al empezar el
turno de su dueño, cada robot recupera su movimiento.

- [ ] **Step 1: Escribir las pruebas que fallan**

```csharp
[Test]
public void RobotLandsOnCoastAndCapturesFlag()
{
    var match = MatchBuilder.NewPlaying();
    var deploy = match.Submit(Side.Blue, new DeployRobotCommand(PrototypeArsenal.TrackedRobot, new HexCoord(0, 1)));
    var robotId = deploy.Events.OfType<RobotDeployed>().Single().RobotId;
    var move = match.Submit(Side.Blue, new MoveRobotCommand(robotId, new[] { new HexCoord(1, 1) }));
    Assert.That(move.Events.OfType<FlagCaptured>().Single().TotalCaptured, Is.EqualTo(1));
    var view = match.ViewFor(Side.Blue);
    Assert.That(view.FlagsCapturedByViewer, Is.EqualTo(1));
    Assert.That(view.OwnRobots.Single().MovementPointsLeft, Is.EqualTo(2)); // 3 − selva 1 (orugas)
    Assert.That(match.ViewFor(Side.Red).FlagsLostByViewer, Is.EqualTo(1));
}

[Test]
public void DeployOnInterior_IsRejected()
{
    var match = MatchBuilder.NewPlaying();
    Assert.That(match.Submit(Side.Blue, new DeployRobotCommand(PrototypeArsenal.TrackedRobot, new HexCoord(1, 1))).Reason,
        Is.EqualTo(RejectionReason.NotCoast));
}

[TestCase(2, 2, TestName = "NoAdyacente")]
public void NonAdjacentPath_IsRejectedAtomically(int q, int r)   // Review Focus 3
{
    var (match, robotId) = MatchBuilder.NewPlayingWithBlueRobotAt(new HexCoord(0, 1));
    var result = match.Submit(Side.Blue, new MoveRobotCommand(robotId, new[] { new HexCoord(q, r) }));
    Assert.That(result.Reason, Is.EqualTo(RejectionReason.InvalidPath));
    Assert.That(match.ViewFor(Side.Blue).OwnRobots.Single().Position, Is.EqualTo(new HexCoord(0, 1)));
}

[Test]
public void PathOverBudget_IsRejectedWithoutCapturing()         // Review Focus 3
{
    var (match, robotId) = MatchBuilder.NewPlayingWithBlueRobotAt(new HexCoord(0, 1));
    var result = match.Submit(Side.Blue, new MoveRobotCommand(robotId, new[] { new HexCoord(1, 1), new HexCoord(1, 2) })); // 1 + 3 > 3
    Assert.That(result.Reason, Is.EqualTo(RejectionReason.InvalidPath));
    Assert.That(match.ViewFor(Side.Blue).FlagsCapturedByViewer, Is.EqualTo(0));
}

[Test]
public void CapturingLastFlag_WinsAndFreezesMatch()             // Review Focus 4
{
    var rules = RulesConfig.Default with { FlagsPerSide = 1 };
    var (match, robotId) = MatchBuilder.NewPlayingWithBlueRobotAt(new HexCoord(0, 1), rules);
    var result = match.Submit(Side.Blue, new MoveRobotCommand(robotId, new[] { new HexCoord(1, 1) }));
    Assert.That(result.Events.OfType<MatchWon>().Single().Winner, Is.EqualTo(Side.Blue));
    Assert.That(match.Phase, Is.EqualTo(MatchPhase.Finished));
    Assert.That(match.Submit(Side.Blue, new EndTurnCommand()).Reason, Is.EqualTo(RejectionReason.MatchFinished));
}
```

`MatchBuilder.NewPlayingWithBlueRobotAt(HexCoord coast, RulesConfig rules = null)` →
`(Match, int RobotId)`: despliega un `robot-orugas` de Blue en esa costa y devuelve su id.

- [ ] **Step 2: Ejecutar y verificar que fallan.**
- [ ] **Step 3: Implementar** `ApplyDeploy` y `ApplyMove` en `MatchState.Robots.cs`, y la recarga del movimiento al empezar el turno.
- [ ] **Step 4: Ejecutar** — `dotnet test` → PASS; formato OK.
- [ ] **Step 5: Commit** — `feat: agrega robots, captura de banderas y victoria`

---

### Task 7: Defensas: construir, señuelos y disparo de torretas

**Files:**
- Create: `Runtime/Match/MatchState.Defense.cs`
- Modify: `Match.cs`, `Events.cs`
- Test: `tests/Flagstorm.Core.Tests/Match/DefenseTests.cs`

**Interfaces:**
- Produces events: `DefenseBuilt(Side Owner, HexCoord Cell, string WeaponId)`,
  `TurretFired(Side Owner, HexCoord Turret, int TargetRobotId, int Damage)` y
  `RobotDestroyed(Side Owner, int RobotId, HexCoord Cell)`.

Reglas: `BuildDefenseCommand` es para la **propia** isla (`OutOfIsland` si no es tierra propia),
requiere la categoría `Defend` y una celda sin bandera ni otra defensa (`CellOccupied`). Al
procesar `EndTurnCommand` del bando X, **antes** de cambiar de turno, cada torreta del rival de X,
en orden por R y luego por Q, dispara una vez a un robot de X dentro de su radio. Elige el más
cercano y, si hay empate, el de menor `RobotId`, y le hace `Power` de daño. Un robot con PV ≤ 0 se
destruye.

- [ ] **Step 1: Escribir las pruebas que fallan**

```csharp
[Test]
public void EnemyTurret_ShootsRobotThatEndsTurnInRange()
{
    var (match, robotId) = MatchBuilder.NewPlayingWithBlueRobotAt(new HexCoord(0, 1)); // vecina de la torreta roja (0,2)
    var result = match.Submit(Side.Blue, new EndTurnCommand());
    var shot = result.Events.OfType<TurretFired>().Single();
    Assert.That(shot.TargetRobotId, Is.EqualTo(robotId));
    Assert.That(match.ViewFor(Side.Blue).OwnRobots.Single().HitPoints, Is.EqualTo(2));
}

[Test]
public void TwoTurns_InRange_DestroyRobot()
{
    var (match, robotId) = MatchBuilder.NewPlayingWithBlueRobotAt(new HexCoord(0, 1));
    match.Submit(Side.Blue, new EndTurnCommand());
    match.Submit(Side.Red, new EndTurnCommand());
    var result = match.Submit(Side.Blue, new EndTurnCommand());
    Assert.That(result.Events.OfType<RobotDestroyed>().Single().RobotId, Is.EqualTo(robotId));
    Assert.That(match.ViewFor(Side.Blue).OwnRobots, Is.Empty);
}

[Test]
public void Decoy_LooksLikeAFlagUntilARobotStepsOnIt()          // Review Focus 5
{
    var match = MatchBuilder.NewPlaying();
    match.Submit(Side.Blue, new EndTurnCommand());
    match.Submit(Side.Red, new BuildDefenseCommand(PrototypeArsenal.Decoy, new HexCoord(1, 3)));
    match.Submit(Side.Red, new EndTurnCommand());
    match.Submit(Side.Blue, new LaunchDroneCommand(PrototypeArsenal.Drone, new HexCoord(1, 3)));
    var signals = match.ViewFor(Side.Blue).EnemySignals;
    Assert.That(signals, Does.Contain(new HexCoord(1, 3)));        // señuelo
    Assert.That(signals, Does.Contain(new HexCoord(0, 3)));        // bandera real
}

[Test]
public void BuildOnEnemyIslandOrOccupiedCell_IsRejected()         // Review Focus 1
{
    var match = MatchBuilder.NewPlaying();
    Assert.That(match.Submit(Side.Blue, new BuildDefenseCommand(PrototypeArsenal.Turret, new HexCoord(10, 10))).Reason,
        Is.EqualTo(RejectionReason.OutOfIsland));
    Assert.That(match.Submit(Side.Blue, new BuildDefenseCommand(PrototypeArsenal.Turret, new HexCoord(1, 1))).Reason,
        Is.EqualTo(RejectionReason.CellOccupied));
}
```

- [ ] **Step 2: Ejecutar y verificar que fallan.**
- [ ] **Step 3: Implementar** `ApplyBuildDefense` y `FireTurretsAgainst(Side attacker)`, y llamar a esta última desde `ApplyEndTurn`.
- [ ] **Step 4: Ejecutar** — `dotnet test` → PASS; formato OK.
- [ ] **Step 5: Commit** — `feat: agrega defensas, señuelos y disparo de torretas`

---

### Task 8: Determinismo: huella del estado y repetición

**Files:**
- Create: `Runtime/Match/StateHasher.cs`, `Runtime/Match/MatchReplayer.cs`
- Test: `tests/Flagstorm.Core.Tests/Match/ReplayTests.cs`

**Interfaces:**
- Produces: `string Match.ComputeStateHash()`: SHA-256 en hexadecimal de una serialización
  canónica de fase, turno, bando actual, energía, banderas, defensas con PV, robots (id, posición,
  PV, movimiento), celdas reveladas por bando, banderas capturadas y ganador, todo ordenado por
  claves. `static Match MatchReplayer.Replay(Func<Match> createFresh, IReadOnlyList<RecordedCommand> history)`
  lanza `InvalidOperationException` si un comando del historial es rechazado.

- [ ] **Step 1: Escribir las pruebas que fallan**

```csharp
[Test]
public void ReplayingHistory_ReproducesExactState()
{
    var original = ScriptedMatches.PlayMixedTurns();     // helper: dron, misil, robot, defensa y varios EndTurn
    var replayed = MatchReplayer.Replay(() => MatchBuilder.NewSetup(), original.History);
    Assert.That(replayed.ComputeStateHash(), Is.EqualTo(original.ComputeStateHash()));
}

[Test]
public void DifferentCommands_ProduceDifferentHash()
{
    var a = MatchBuilder.NewPlaying();
    var b = MatchBuilder.NewPlaying();
    b.Submit(Side.Blue, new LaunchDroneCommand(PrototypeArsenal.Drone, new HexCoord(1, 1)));
    Assert.That(b.ComputeStateHash(), Is.Not.EqualTo(a.ComputeStateHash()));
}

[Test]
public void Replay_WithInvalidHistory_Throws()
{
    var bad = new[] { new RecordedCommand(Side.Red, new EndTurnCommand()) };
    Assert.Throws<InvalidOperationException>(() => MatchReplayer.Replay(() => MatchBuilder.NewSetup(), bad));
}
```

- [ ] **Step 2: Ejecutar y verificar que fallan.**
- [ ] **Step 3: Implementar** con `System.Security.Cryptography.SHA256` y `StringBuilder`, con números formateados con `CultureInfo.InvariantCulture`.
- [ ] **Step 4: Ejecutar** — `dotnet test` → PASS; formato OK.
- [ ] **Step 5: Commit** — `feat: agrega huella de estado y repeticion de partidas`

---

### Task 9: IA básica y partidas IA contra IA

**Files:**
- Create: `Runtime/Ai/IRandomSource.cs`, `SeededRandom.cs`, `BasicAiPlayer.cs`
- Test: `tests/Flagstorm.Core.Tests/Ai/SeededRandomTests.cs`, `Ai/BasicAiPlayerTests.cs`

**Interfaces:**
- Consumes: `PlayerView`, `PathFinder`, todos los comandos y `RulesConfig`.
- Produces:
  - `interface IRandomSource { int Next(int minInclusive, int maxExclusive); }`.
  - `sealed class SeededRandom : IRandomSource` (xorshift32, con semilla 0 → 1), `SeededRandom(uint seed)`.
  - `sealed class BasicAiPlayer`: `BasicAiPlayer(IRandomSource random)`;
    `SetupCommand ChooseSetup(PlayerView view)`;
    `IMatchCommand NextCommand(PlayerView view, IReadOnlyList<WeaponDefinition> arsenal)`, que
    devuelve un comando a la vez y `EndTurnCommand` cuando ya no quiere hacer nada más. El llamador
    (pruebas o Unity) aplica cada comando y vuelve a pedir el siguiente con la vista actualizada.
    **Solo** recibe `PlayerView`, nunca `Match`: así se garantiza que no haga trampa (spec §5.2).

Estrategia (las pruebas no la determinan): en la preparación, banderas en celdas interiores al
azar, separadas entre sí al menos 2 si es posible, y torretas en interiores vecinos a banderas. En
cada turno, por prioridad mientras alcance la energía: (1) mover los robots propios hacia la señal
conocida más cercana con `FindPath` + `TruncateToBudget`; (2) si hay señales conocidas sin robot
asignado, desplegar en la costa más cercana a una señal el robot de menor costo de ruta; (3) misil
estándar sobre la torreta conocida más cercana a la ruta de un robot; (4) dron sobre la celda no
revelada que maximice las celdas nuevas reveladas (empate al azar); (5) con la energía sobrante,
torreta o señuelo cerca de las banderas propias. Como la IA pide un comando a la vez, siempre
decide con la vista actualizada. Para no quedarse en un bucle, devuelve `EndTurnCommand` cuando
ninguna prioridad aplica con la energía restante.

- [ ] **Step 1: Escribir las pruebas que fallan**

```csharp
[Test]
public void SeededRandom_IsDeterministicAndInRange()
{
    var a = new SeededRandom(42); var b = new SeededRandom(42);
    for (var i = 0; i < 100; i++)
    {
        var x = a.Next(3, 9);
        Assert.That(x, Is.EqualTo(b.Next(3, 9)));
        Assert.That(x, Is.InRange(3, 8));
    }
}

[TestCaseSource(nameof(Seeds))]   // Seeds = 1..20
public void AiVsAi_OnPrototypeIsland_FinishesWithoutRejectedCommands(uint seed)
{
    var match = new Match(PrototypeIslands.CreateDefault(), PrototypeIslands.CreateDefault(), RulesConfig.Default, PrototypeArsenal.All);
    var players = new Dictionary<Side, BasicAiPlayer>
    {
        [Side.Blue] = new BasicAiPlayer(new SeededRandom(seed)),
        [Side.Red] = new BasicAiPlayer(new SeededRandom(seed + 1000)),
    };
    foreach (var side in new[] { Side.Blue, Side.Red })
        Assert.That(match.Submit(side, players[side].ChooseSetup(match.ViewFor(side))).IsAccepted, Is.True);

    var guard = 0;
    while (match.Phase == MatchPhase.Playing && guard++ < 5000)
    {
        var side = match.CurrentSide;
        var command = players[side].NextCommand(match.ViewFor(side), PrototypeArsenal.All);
        var result = match.Submit(side, command);
        Assert.That(result.IsAccepted, Is.True, $"{command} → {result.Reason}");
    }
    Assert.That(match.Phase, Is.EqualTo(MatchPhase.Finished));
    Assert.That(match.TurnNumber, Is.LessThanOrEqualTo(200));
}
```

- [ ] **Step 2: Ejecutar y verificar que fallan.**
- [ ] **Step 3: Implementar** `SeededRandom` (`x ^= x << 13; x ^= x >> 17; x ^= x << 5`) y `BasicAiPlayer`.
- [ ] **Step 4: Ejecutar** — `dotnet test` → PASS (las 20 semillas). Si alguna semilla no termina,
  ajustar la estrategia, no la prueba. Registrar la mediana de turnos por partida en el mensaje
  del commit: sirve como dato de equilibrio.
- [ ] **Step 5: Verificar la cobertura** —
  `dotnet test --collect:"XPlat Code Coverage"` y luego leer el `line-rate` de
  `tests/Flagstorm.Core.Tests/TestResults/*/coverage.cobertura.xml`. Esperado: ≥ 0.60.
- [ ] **Step 6: Commit** — `feat: agrega IA basica con partidas IA contra IA`

---

### Task 10: Proyecto Unity que consume el núcleo

**Prerrequisito (lo hace Oscar):** instalar Unity Hub y, desde él, la versión **Unity 6 LTS** más
reciente que ofrezca, con los módulos *Android Build Support* (OpenJDK + SDK/NDK). El módulo de
iOS no hace falta todavía. Anotar la versión exacta en el README (Tech Stack).

**Files:**
- Create: `unity/Flagstorm/` (plantilla **Universal 3D**), `unity/Flagstorm/Packages/manifest.json`
  (agregar `"com.flagstorm.core": "file:../../../packages/com.flagstorm.core"`),
  `Assets/Flagstorm/Tests/EditMode/Flagstorm.Presentation.Tests.asmdef` y `CoreSmokeTests.cs`
- Create: los `.meta` que Unity genera dentro de `packages/com.flagstorm.core/` (se versionan)

**Interfaces:**
- Consumes: el ensamblado `Flagstorm.Core`.
- Produces: el proyecto Unity con el núcleo disponible, el Input System activo
  (*Active Input Handling = Input System Package*) y orientación vertical (*Default Orientation = Portrait*).

- [ ] **Step 1: Crear el proyecto** desde Unity Hub en `unity/Flagstorm` y agregar el paquete local al manifest.
- [ ] **Step 2: Escribir la prueba EditMode**

```csharp
public class CoreSmokeTests
{
    [Test]
    public void CoreRunsInsideUnity()
    {
        var match = new Match(PrototypeIslands.CreateDefault(), PrototypeIslands.CreateDefault(), RulesConfig.Default, PrototypeArsenal.All);
        var ai = new BasicAiPlayer(new SeededRandom(7));
        Assert.That(match.Submit(Side.Blue, ai.ChooseSetup(match.ViewFor(Side.Blue))).IsAccepted, Is.True);
    }
}
```

- [ ] **Step 3: Ejecutar las pruebas de Unity en modo batch**

Run: `"C:\Program Files\Unity\Hub\Editor\<versión>\Editor\Unity.exe" -batchmode -projectPath unity/Flagstorm -runTests -testPlatform EditMode -testResults TestResults/editmode.xml -logFile -`
Expected: código 0 y `result="Passed"` en `TestResults/editmode.xml`. Esto demuestra que el
núcleo compila con el C# de Unity (records, shim `IsExternalInit`).

- [ ] **Step 4: Commit** — `chore: crea proyecto Unity que consume el nucleo`

---

### Task 11: Escena de prototipo en vista holográfica

**Files:**
- Create in `unity/Flagstorm/Assets/Flagstorm/`: `Presentation/Flagstorm.Presentation.asmdef`
  (que referencia `Flagstorm.Core`, `Unity.InputSystem` y `Unity.TextMeshPro`), `Presentation/HexLayout.cs`,
  `HexMeshFactory.cs`, `HoloPalette.cs`, `EventTextFormatter.cs`, `RejectionTextFormatter.cs`,
  `ModifierLabelFormatter.cs`, `IslandRenderer.cs`, `MatchController.cs`, `HudController.cs`,
  `CellPicker.cs`, `UI/Hud.uxml`, `UI/Hud.uss`, `Scenes/PrototypeHolo.unity`
- Test: `Tests/EditMode/PresentationFormattingTests.cs`, `Tests/EditMode/HexLayoutTests.cs`

**Interfaces:**
- Consumes: `Match`, `PlayerView`, `BasicAiPlayer.NextCommand`, `PathFinder`, todos los eventos.
- Produces (clases puras, con pruebas):
  - `static class HexLayout`: `Vector3 ToWorld(HexCoord c, float size, Vector3 origin)` para hex
    pointy-top, con `x = size·√3·(Q + R/2)` y `z = −size·1,5·R`.
  - `static class HoloPalette`: `Color OwnColor` (cian `#2DE2FF`), `Color EnemyColor` (rosa `#FF3D7F`),
    `Color TerrainColor(TerrainType)` (Jungle `#3DFF8A`, Ice `#8AD8FF`, Volcano `#FF7A3C`,
    Beach `#F2D48A`, Rock `#B8B8C8`) y `string TerrainLabel(TerrainType)` (`"SELVA"`, `"HIELO"`,
    `"VOLCÁN"`, `"PLAYA"`, `"ROCA"`). La etiqueta cumple la regla de no depender solo del color.
  - `static class ModifierLabelFormatter`: `string Format(int percent)`, que da `"+40 %"`,
    `"−50 %"` (signo menos U+2212) o `""` para 0.
  - `static class EventTextFormatter`: `string Format(IMatchEvent e, Side viewer)` con un texto en
    español por cada evento de las tareas 4 a 7 (por ejemplo, `FlagCaptured` visto por quien
    captura da `"¡Capturaste una bandera! (3/5)"`).
  - `static class RejectionTextFormatter`: `string Format(RejectionReason r)` con un texto en
    español por cada valor (por ejemplo, `NotEnoughEnergy` da `"No tienes energía suficiente"`).

Comportamiento de la escena (sin pruebas automáticas; se verifica en la tarea 12):
- La cámara es ortográfica, vertical y con una ligera inclinación. El fondo es un océano oscuro
  `#040916`. La isla propia queda abajo y la rival arriba, como en las maquetas aprobadas.
- Cada hexágono es una malla plana (`HexMeshFactory.CreateFlatHex(size)`) con un material URP Unlit
  del color de su terreno, más un contorno del color del bando y la etiqueta de terreno (TextMeshPro)
  que se puede ocultar. En la isla rival, las celdas no reveladas se ven al 25 % de opacidad. Las
  señales (`◆`, rosa), las torretas conocidas (`■`, rosa con sus PV), las banderas propias
  (`⚑`, cian), los robots (`●`, del color del bando y con sus PV) y los señuelos propios (`◇`)
  llevan cada uno un ícono distinto.
- **Preparación:** se tocan celdas propias para marcar o desmarcar 5 banderas y luego hasta 2
  torretas. "Confirmar" envía el `SetupCommand`. La IA hace su preparación al iniciar.
- **Turno:** la barra inferior tiene un botón por arma del arsenal (nombre + costo). Al elegir un
  arma y mantener el dedo o pasar el cursor sobre una celda rival, el botón muestra
  `ModifierLabelFormatter.Format(ModifierPercentOn(terreno))`. Al tocar la celda se envía el
  comando correspondiente. Para mover un robot: se toca el robot y luego el destino, y se envía
  `MoveRobotCommand` con `FindPath` + `TruncateToBudget`. El botón "Terminar turno" envía `EndTurnCommand`.
- Arriba se ven `⚡ energía/MaxEnergy`, `Banderas rivales: capturadas/5` y `Tus banderas: restantes/5`.
- Un registro muestra los últimos 6 eventos con `EventTextFormatter`. Los rechazos se muestran
  como un aviso de 2 s con `RejectionTextFormatter`.
- **Turno de la IA:** `MatchController` pide `NextCommand` y aplica un comando cada 0,6 s, para
  que se pueda seguir lo que hace, hasta su `EndTurnCommand`.
- Al terminar, un panel muestra "¡Victoria!" o "Derrota" y un botón "Jugar de nuevo".
- Los botones tienen texto visible y un área táctil de al menos 44 px. El contraste de texto sobre
  `#040916` es ≥ 4,5:1 (cian y blanco cumplen).

- [ ] **Step 1: Escribir las pruebas que fallan**

```csharp
[Test] public void Modifier_UsesTypographicMinus() =>
    Assert.That(ModifierLabelFormatter.Format(-50), Is.EqualTo("\u221250 %"));
[Test] public void Modifier_PositiveAndZero()
{
    Assert.That(ModifierLabelFormatter.Format(40), Is.EqualTo("+40 %"));
    Assert.That(ModifierLabelFormatter.Format(0), Is.EqualTo(""));
}
[Test] public void EveryTerrainHasLabelAndEveryRejectionHasText()
{
    foreach (TerrainType t in Enum.GetValues(typeof(TerrainType))) Assert.That(HoloPalette.TerrainLabel(t), Is.Not.Empty);
    foreach (RejectionReason r in Enum.GetValues(typeof(RejectionReason)))
        if (r != RejectionReason.None) Assert.That(RejectionTextFormatter.Format(r), Is.Not.Empty);
}
[Test] public void CaptureEvent_TextForCapturer() =>
    Assert.That(EventTextFormatter.Format(new FlagCaptured(Side.Blue, new HexCoord(1, 1), 3), Side.Blue),
        Is.EqualTo("¡Capturaste una bandera! (3/5)"));
[Test] public void HexLayout_NeighborsAreEquidistant()
{
    var center = HexLayout.ToWorld(new HexCoord(0, 0), 1f, Vector3.zero);
    foreach (var n in new HexCoord(0, 0).Neighbors())
        Assert.That(Vector3.Distance(center, HexLayout.ToWorld(n, 1f, Vector3.zero)), Is.EqualTo(Mathf.Sqrt(3f)).Within(1e-4));
}
```

`EventTextFormatter.Format` recibe el total de banderas de `RulesConfig`. Si hace falta, se
agrega un parámetro `int flagsPerSide` a la firma y se ajusta la prueba.

- [ ] **Step 2: Ejecutar y verificar que fallan** (con el comando batch de la tarea 10).
- [ ] **Step 3: Implementar las clases puras** y ejecutar → PASS.
- [ ] **Step 4: Construir la escena y los MonoBehaviours** según el comportamiento descrito y
  agregar `PrototypeHolo` a *Build Settings* como escena 0.
- [ ] **Step 5: Verificar en el editor** — Play: completar la preparación, lanzar un dron, ver las
  señales, desplegar un robot, terminar el turno y ver jugar a la IA. Sin errores en la consola.
- [ ] **Step 6: Commit** — `feat: agrega escena de prototipo en vista holografica`

---

### Task 12: Prueba de juego en Android y registro de resultados

**Files:**
- Create: `docs/playtests/<fecha>-prototipo.md`
- Modify: `README.md` (secciones *screenshots* y *usage/examples*),
  `packages/com.flagstorm.core/Runtime/Arsenal/PrototypeArsenal.cs` y `RulesConfig.cs` (solo si la
  prueba pide ajustar valores, y volviendo a ejecutar `dotnet test`)

- [ ] **Step 1: Compilar un APK de desarrollo** (*Build Settings → Android → Development Build*) e
  instalarlo en un celular Android real.
- [ ] **Step 2: Jugar 3 partidas completas** y anotar en `docs/playtests/` lo siguiente: duración de
  cada partida (meta del spec: 5 a 10 min), turnos, quién ganó, si el terreno cambió alguna
  decisión de arsenal, momentos aburridos o confusos, y FPS percibidos (meta ≥ 30).
- [ ] **Step 3: Ajustar valores** en `PrototypeArsenal` y `RulesConfig` si una partida dura más de
  10 min o si el terreno no influyó en ninguna decisión. Volver a ejecutar `dotnet test`, incluida
  la prueba IA contra IA.
- [ ] **Step 4: Agregar capturas** de la vista holográfica al README.
- [ ] **Step 5: Verificación final** — `dotnet test` → PASS; `dotnet format --verify-no-changes`
  → OK; pruebas EditMode de Unity → PASS; cobertura del núcleo ≥ 60 %.
- [ ] **Step 6: Commit** — `docs: registra prueba de juego del prototipo`
- [ ] **Step 7: Pedir a Oscar el veredicto de la etapa** ("¿divierte?"): define si se pasa a la
  etapa 2 (rebanada vertical) o si se iteran las reglas.

---

## Fuera de esta etapa (planes siguientes)
Vista real 3D y transición de escaneo, tutorial, guardado, jefes, regiones, arsenal previo a la
partida (6 espacios), CI con GameCI, analizadores Roslyn dentro de Unity, iOS, tienda, anuncios,
privacidad e idioma inglés: etapas 2 a 4 del spec (§13).
