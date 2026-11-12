# Flagstorm: defensas, arsenal por terreno y desbloqueo — plan de implementación

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Que cada ataque tenga su defensa (escudo antidrones, antimisiles, robots defensores con
combate), con 5 robots de ataque y 5 defensores por terreno, un arsenal de 6 espacios y desbloqueo
por partidas ganadas, jugable en el prototipo holográfico de Unity.

**Architecture:** Las reglas nuevas viven en el núcleo determinista (`packages/com.flagstorm.core`),
con comandos, eventos y `PlayerView` como hasta ahora; la fase de defensa (torretas, combates y
avance de defensores) se resuelve al terminar el turno del atacante. Unity solo traduce toques,
dibuja el estado y guarda las victorias en `PlayerPrefs`.

**Tech Stack:** C# 9 / .NET Standard 2.1 (núcleo), .NET SDK 10.0.401, NUnit 4.6.1, StyleCop;
Unity 6000.3.28f1 + URP 17.3 + UI Toolkit + Input System. Sin dependencias nuevas.

**Spec:** `docs/superpowers/specs/2026-11-12-flagstorm-defensas-design.md` (y el diseño general
`docs/superpowers/specs/2026-11-09-flagstorm-design.md` §4.4, §5.2, §6.3 y §11).

## Global Constraints

- Núcleo: `netstandard2.1`, `LangVersion 9.0`, sin APIs de Unity ni `System.Random`; determinista
  (recorrer colecciones siempre ordenadas con `CellOrder` o por id al producir salidas o eventos).
- Nombres según C#/Unity (excepción ya aprobada): PascalCase para tipos, constantes y archivos;
  campos privados `_camelCase`; ids de armas en kebab-case sin tildes ni ñ.
- Todo texto visible en español. Ninguna información solo por color (WCAG 1.4.1): cada pieza nueva
  tiene forma propia.
- Antes de cada commit: `dotnet build -warnaserror`, `dotnet format --verify-no-changes` y
  `dotnet test tests/Flagstorm.Core.Tests` en verde. Las tareas de Unity además corren EditMode.
- Cobertura de línea del núcleo ≥ 60 % (lineamientos); reglas de combate e intercepción cubiertas
  por pruebas propias.
- Commits en `main`, Conventional Commits en español (`feat`, `fix`, `docs`, `style`, `refactor`,
  `test`, `chore`), terminados en `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>`; push a
  `origin main` permitido.
- Unity por lotes siempre con `timeout`:
  `timeout 900 "/d/TRABAJO/UnityEditor/6000.3.28f1/Editor/Unity.exe" -batchmode -projectPath unity/Flagstorm -runTests -testPlatform EditMode -testResults "$PWD/TestResults/editmode.xml" -logFile TestResults/unity-editmode.log`
  (PlayMode igual con `-testPlatform PlayMode`). Ninguna prueba PlayMode usa `WaitForEndOfFrame`.

## Valores iniciales de equilibrio

Decisión del constructor (spec §7); se ajustan en la prueba de juego. Las pruebas usan estos valores.

| Id | Nombre | Categoría | Energía | Poder | Radio | PV | Mov. | Nivel |
|---|---|---|---|---|---|---|---|---|
| `dron-explorador` | Dron explorador | Reveal | 2 | 0 | 1 | 0 | 0 | 0 |
| `escudo-antidrones` | Escudo antidrones | Defend (intercepta Reveal) | 3 | 0 | 1 | 3 | 0 | 0 |
| `robot-anfibio` | Robot anfibio | Capture (Playa) | 4 | 2 | 0 | 4 | 3 | 0 |
| `defensor-anfibio` | Defensor anfibio | Guard (Playa) | 3 | 2 | 2 | 4 | 1 | 0 |
| `misil-estandar` | Misil estándar | Destroy | 3 | 3 | 1 | 0 | 0 | 1 |
| `antimisiles` | Antimisiles | Defend (intercepta Destroy) | 4 | 0 | 1 | 3 | 0 | 1 |
| `robot-trepador` / `defensor-trepador` | … trepador | Capture / Guard (Selva) | 4 / 3 | 2 | 0 / 2 | 4 | 3 / 1 | 2 |
| `robot-nieve` / `defensor-nieve` | … de nieve | Capture / Guard (Hielo) | 4 / 3 | 2 | 0 / 2 | 4 | 3 / 1 | 3 |
| `robot-orugas` / `defensor-orugas` | … de orugas | Capture / Guard (Roca) | 4 / 3 | 2 | 0 / 2 | 4 | 3 / 1 | 4 |
| `robot-magma` / `defensor-magma` | … de magma | Capture / Guard (Volcán) | 4 / 3 | 2 | 0 / 2 | 4 | 3 / 1 | 5 |
| `misil-incendiario` | Misil incendiario | Destroy | 3 | 3 | 1 | 0 | 0 | 6 |
| `torreta` | Torreta | Defend | 3 | 2 | 1 | 4 | 0 | 6 |
| `senuelo` | Señuelo | Defend | 2 | 0 | 0 | 1 | 0 | 7 |

- Robots (ataque y defensores): poder **+50 %** en su terreno y **−50 %** en su terreno opuesto
  (Hielo↔Volcán, Selva↔Roca, Playa→Volcán). Con poder 2: 3 en casa, 2 neutral, 1 en contra.
- Movimiento de robots de ataque: 1 en su terreno y en Playa, 2 en el resto, 3 en Volcán (salvo
  magma, 1 en Volcán y 2 en Hielo). El misil incendiario conserva +40 % Selva / −50 % Hielo.
- `Radius` de un defensor = radio de su zona de guardia; su `MovementPoints` = 1 casilla por turno.

## Review Focus

1. **Arsenal sin robot de ataque:** un arsenal que no incluye ningún arma `Capture` no puede ganar;
   `LoadoutRules.Validate` debe rechazarlo (tarea 2).
2. **Defensas en celdas no reveladas:** `PlayerView` del atacante nunca expone escudos,
   antimisiles, torretas ni defensores rivales que no estén en celdas reveladas o junto a un robot
   propio (tareas 3 y 4).
3. **Robot de ataque que intenta entrar a una casilla con defensor rival:** la ruta completa se
   rechaza con `InvalidPath` y nada cambia (tarea 3).
4. **Dos defensores contra un atacante, o un defensor entre dos atacantes:** el resultado es
   determinista y no depende del orden de inserción (tarea 5).
5. **Defensor fuera de su zona:** nunca termina un turno a más de su radio de su puesto (tarea 5).

---

## Estructura de archivos

```
packages/com.flagstorm.core/Runtime/
  Arsenal/WeaponCategory.cs        + Guard
  Arsenal/WeaponDefinition.cs      + Intercepts, UnlockLevel (init)
  Arsenal/PrototypeArsenal.cs      catálogo de 17 armas (tabla de arriba)
  Arsenal/ArsenalProgression.cs    NUEVO: qué se desbloquea con N victorias
  Arsenal/LoadoutRules.cs          NUEVO: validar una selección de 6
  Rules/Basics.cs                  RulesConfig.InitialFreeDefenses, RejectionReason.NotInLoadout
  Rules/Commands.cs                SetupCommand con DefensePlacement
  Rules/Events.cs                  eventos nuevos
  Rules/SideState.cs               + Loadout, Guards (GuardState)
  Rules/PlayerView.cs              OwnDefenses, KnownEnemyDefenses, OwnGuards, KnownEnemyGuards
  Rules/Match.cs                   constructor con loadouts, setup nuevo, CheckWeapon con loadout
  Rules/Match.Guards.cs            NUEVO: colocar defensores, combate y avance
  Rules/Match.Reveal.cs            escudo, antimisiles, daño de misiles a todo
  Rules/Match.Defense.cs           fase de defensa al final del turno
  Rules/Match.Hash.cs              + defensores
  Ai/BasicAiPlayer.cs              ChooseLoadout, setup y uso de lo nuevo
tests/Flagstorm.Core.Tests/        pruebas por tarea (Arsenal/, Rules/, Ai/)
unity/Flagstorm/Assets/Flagstorm/Presentation/
  MatchSession.cs, MatchController.cs, HudView.cs, TextFormatters.cs, HexMeshFactory.cs
  ProgressStore.cs                 NUEVO: victorias en PlayerPrefs
  LoadoutView.cs                   NUEVO: pantalla de selección de arsenal
```

---

### Task 1: Catálogo de 17 armas y progresión

**Files:**
- Modify: `Arsenal/WeaponCategory.cs`, `Arsenal/WeaponDefinition.cs`, `Arsenal/PrototypeArsenal.cs`
- Create: `Arsenal/ArsenalProgression.cs`
- Test: `tests/Flagstorm.Core.Tests/Arsenal/WeaponDefinitionTests.cs`, `Arsenal/ArsenalProgressionTests.cs`

**Interfaces:**
- Produces: `WeaponCategory.Guard`; en `WeaponDefinition` las propiedades
  `WeaponCategory? Intercepts { get; init; }` y `int UnlockLevel { get; init; }` (no se cambia el
  constructor posicional); constantes `PrototypeArsenal.Drone, DroneShield, BeachRobot, BeachGuard,
  StandardMissile, AntiMissile, JungleRobot, JungleGuard, SnowRobot, SnowGuard, TrackedRobot,
  TrackedGuard, MagmaRobot, MagmaGuard, IncendiaryMissile, Turret, Decoy` con los ids de la tabla;
  `PrototypeArsenal.HomeTerrainOf(string weaponId) → TerrainType?`;
  `static class ArsenalProgression { IReadOnlyList<WeaponDefinition> UnlockedAfter(int wins); int MaxLevel }`.

- [ ] **Step 1: Pruebas que fallan**

```csharp
[Test] public void Catalog_HasSeventeenWeapons_WithUniqueIds() { /* 17, ids distintos */ }
[Test] public void EveryAttackHasItsDefense()
{
    Assert.That(PrototypeArsenal.Get(PrototypeArsenal.DroneShield).Intercepts, Is.EqualTo(WeaponCategory.Reveal));
    Assert.That(PrototypeArsenal.Get(PrototypeArsenal.AntiMissile).Intercepts, Is.EqualTo(WeaponCategory.Destroy));
    foreach (TerrainType t in Enum.GetValues(typeof(TerrainType)))
        Assert.That(PrototypeArsenal.All.Count(w => w.Category == WeaponCategory.Guard
            && PrototypeArsenal.HomeTerrainOf(w.Id) == t), Is.EqualTo(1), t.ToString());
}
[Test] public void Robot_IsStrongAtHomeAndWeakOnOpposite()
{
    var snow = PrototypeArsenal.Get(PrototypeArsenal.SnowRobot);
    Assert.That(snow.EffectivePowerOn(TerrainType.Ice), Is.EqualTo(3));
    Assert.That(snow.EffectivePowerOn(TerrainType.Rock), Is.EqualTo(2));
    Assert.That(snow.EffectivePowerOn(TerrainType.Volcano), Is.EqualTo(1));
}
[Test] public void Progression_StartsWithDroneShieldAndBeachPair() =>
    Assert.That(ArsenalProgression.UnlockedAfter(0).Select(w => w.Id), Is.EquivalentTo(new[]
        { "dron-explorador", "escudo-antidrones", "robot-anfibio", "defensor-anfibio" }));
[Test] public void Progression_EachWinAddsTheNextLevel_AndEndsWithAll()
{
    Assert.That(ArsenalProgression.UnlockedAfter(1).Select(w => w.Id), Does.Contain("antimisiles"));
    Assert.That(ArsenalProgression.UnlockedAfter(ArsenalProgression.MaxLevel).Count, Is.EqualTo(17));
    Assert.That(ArsenalProgression.UnlockedAfter(-3).Count, Is.EqualTo(4));
}
```
`EveryTerrainHasAGoodRobot` y `NoWeaponIsBestEverywhere` existentes deben seguir pasando con el catálogo nuevo.

- [ ] **Step 2:** `dotnet test tests/Flagstorm.Core.Tests --filter "WeaponDefinitionTests|ArsenalProgressionTests"` → FAIL (no compila).
- [ ] **Step 3:** Implementar con los valores de la tabla. `UnlockedAfter(wins)` = armas con `UnlockLevel <= max(0, wins)`, en el orden del catálogo.
- [ ] **Step 4:** Toda la suite → PASS. Las pruebas existentes que dependan de movimientos o PV de `robot-orugas`/`robot-nieve` se ajustan a la tabla (no se borran).
- [ ] **Step 5:** Commit `feat: agrega catalogo de 17 armas por terreno y progresion por victorias`.

### Task 2: Arsenal por bando y preparación con defensas gratis

**Files:**
- Create: `Arsenal/LoadoutRules.cs`
- Modify: `Rules/Basics.cs`, `Rules/Commands.cs`, `Rules/SideState.cs`, `Rules/Match.cs`, `Ai/BasicAiPlayer.cs` (solo `ChooseSetup` para compilar), `tests/.../Support/MatchBuilder.cs`
- Test: `tests/Flagstorm.Core.Tests/Arsenal/LoadoutRulesTests.cs`, `Rules/MatchSetupAndTurnTests.cs`

**Interfaces:**
- Consumes: Task 1.
- Produces:
  - `public enum LoadoutError { None, TooMany, Duplicate, NotUnlocked, NoCaptureWeapon }`;
    `static class LoadoutRules { const int MaxSlots = 6; LoadoutError Validate(IReadOnlyList<string> selection, IReadOnlyList<WeaponDefinition> unlocked); }`
  - `RulesConfig(int FlagsPerSide, int EnergyPerTurn, int MaxEnergy, int InitialFreeDefenses)` (renombra `InitialFreeTurrets`; `Default` = 5, 6, 10, 2).
  - `RejectionReason.NotInLoadout`.
  - `public sealed record DefensePlacement(string WeaponId, HexCoord Cell);`
    `SetupCommand(IReadOnlyList<HexCoord> Flags, IReadOnlyList<DefensePlacement> FreeDefenses)`.
  - `Match(IslandMap blue, IslandMap red, RulesConfig rules, IReadOnlyList<WeaponDefinition> catalog, IReadOnlyList<string> blueLoadout, IReadOnlyList<string> redLoadout)`; el constructor de 4 parámetros sigue existiendo y da a ambos bandos el catálogo completo.
  - `Match.LoadoutOf(Side) → IReadOnlyList<string>`; `PlayerView.Loadout`.

- [ ] **Step 1: Pruebas que fallan**

```csharp
[Test] public void Validate_RejectsSevenWeapons() /* TooMany */
[Test] public void Validate_RejectsLoadoutWithoutCaptureWeapon() /* NoCaptureWeapon: dron + escudo */
[Test] public void Validate_RejectsLockedWeapon() /* NotUnlocked: antimisiles con UnlockedAfter(0) */
[Test] public void Validate_AcceptsStarterSet() /* None con los 4 iniciales */
[Test] public void Setup_PlacesFreeDefensesFromLoadout()
{
    // Azul con loadout {dron, escudo, robot-anfibio, defensor-anfibio}: un escudo y un defensor gratis.
    // Aceptado; OwnDefenses contiene el escudo con 3 PV y OwnGuards el defensor con 4 PV.
}
[Test] public void Setup_RejectsFreeDefenseNotInLoadout() /* torreta fuera del loadout → InvalidSetup */
[Test] public void Setup_RejectsAttackWeaponAsFreeDefense() /* robot-anfibio → InvalidSetup */
[Test] public void Command_WithWeaponOutsideLoadout_IsRejected() /* misil con loadout inicial → NotInLoadout, energía igual */
```
- [ ] **Step 2:** Ejecutar → FAIL.
- [ ] **Step 3:** Implementar. `CheckWeapon` comprueba el loadout del bando antes que la categoría. Las defensas gratis aceptan categorías `Defend` y `Guard`, hasta `InitialFreeDefenses`, en celdas distintas de las banderas. `MatchBuilder` pasa a usar `DefensePlacement` con torretas (catálogo completo) para no romper pruebas.
- [ ] **Step 4:** Suite completa → PASS.
- [ ] **Step 5:** Commit `feat: agrega arsenal por bando y defensas gratis en la preparacion`.

### Task 3: Robots defensores (colocación, visibilidad y bloqueo)

**Files:**
- Create: `Rules/Match.Guards.cs`
- Modify: `Rules/SideState.cs`, `Rules/PlayerView.cs`, `Rules/Events.cs`, `Rules/Match.cs`, `Rules/Match.Robots.cs`, `Rules/Match.Hash.cs`
- Test: `tests/Flagstorm.Core.Tests/Rules/GuardTests.cs`

**Interfaces:**
- Consumes: Task 2.
- Produces:
  - `internal sealed class GuardState { int RobotId; string WeaponId; HexCoord Post; HexCoord Position; int HitPoints; }` en `SideState.Guards` (en la isla propia). Los ids salen del mismo contador `_nextRobotId` que los robots de ataque.
  - `public sealed record GuardInfo(int RobotId, string WeaponId, HexCoord Post, HexCoord Position, int HitPoints);`
    `PlayerView.OwnGuards`, `PlayerView.KnownEnemyGuards` (defensores rivales en celdas reveladas o a distancia ≤ 1 de un robot propio).
  - Evento `GuardPlaced(Side Owner, int RobotId, string WeaponId, HexCoord Cell)`.
  - `BuildDefenseCommand` con un arma `Guard` coloca un defensor (cuesta su energía; celda de la isla propia sin bandera, defensa ni defensor).

- [ ] **Step 1: Pruebas que fallan**

```csharp
[Test] public void BuildGuard_CostsEnergyAndAppearsInOwnView() /* energía 6→3, OwnGuards con Post=Position=celda, 4 PV */
[Test] public void BuildGuard_OnOccupiedCell_IsRejected() /* sobre bandera → CellOccupied */
[Test] public void EnemyGuard_IsHiddenUntilRevealedOrAdjacent()
{
    // Rojo coloca un defensor; la vista azul no lo muestra. Tras un dron azul sobre su celda, aparece.
}
[Test] public void AttackRobot_CannotWalkIntoGuardCell() /* ruta que pasa por el defensor → InvalidPath, posición igual */
[Test] public void StateHash_ChangesWhenGuardIsPlaced()
```
- [ ] **Step 2:** Ejecutar → FAIL.
- [ ] **Step 3:** Implementar. El despliegue de un robot de ataque sobre una celda con defensor rival se rechaza con `CellOccupied`.
- [ ] **Step 4:** Suite completa → PASS.
- [ ] **Step 5:** Commit `feat: agrega robots defensores con zona de guardia`.

### Task 4: Escudo antidrones, antimisiles y daño de misiles a todo

**Files:**
- Modify: `Rules/Match.Reveal.cs`, `Rules/PlayerView.cs`, `Rules/Events.cs`, `Rules/Match.cs`
- Test: `tests/Flagstorm.Core.Tests/Rules/InterceptionTests.cs`, ajustar `RevealAndMissileTests.cs` y `DefenseTests.cs`

**Interfaces:**
- Consumes: Tasks 1–3.
- Produces:
  - `public sealed record DefenseInfo(string WeaponId, int HitPoints);`
    `PlayerView.OwnDefenses : IReadOnlyDictionary<HexCoord, DefenseInfo>` (todas las propias, señuelos incluidos) y
    `PlayerView.KnownEnemyDefenses` (rivales en celdas reveladas, sin señuelos). Se eliminan `OwnTurrets`, `OwnDecoys` y `KnownEnemyTurrets`; se actualizan sus usos en IA y pruebas.
  - Eventos `DroneShotDown(Side By, HexCoord Target, HexCoord Shield)`,
    `MissileIntercepted(Side By, string WeaponId, HexCoord Target, HexCoord AntiMissile, int RemainingHitPoints)`,
    `GuardDamaged(Side Owner, int RobotId, int RemainingHitPoints)`, `GuardDestroyed(Side Owner, int RobotId, HexCoord Cell)`.
- Reglas: si el objetivo de un dron está a ≤ `Radius` de un escudo rival, el dron se derriba (gasta energía, no revela nada, revela la celda del escudo). Igual para misiles y antimisiles; la intercepción quita 1 PV al antimisiles (puede destruirlo). Con varias defensas, gana la más cercana al objetivo y luego `CellOrder`. Un misil no interceptado daña con `EffectivePowerOn(terreno de la celda)` a toda defensa rival no señuelo y a todo defensor rival en su área; nunca a robots propios.

- [ ] **Step 1: Pruebas que fallan**

```csharp
[Test] public void Drone_OverShield_IsShotDown_AndRevealsOnlyTheShield()
[Test] public void Drone_OutsideShieldRange_RevealsNormally()
[Test] public void Missile_OverAntiMissile_IsIntercepted_AndAntiMissileLosesOnePoint()
[Test] public void AntiMissile_DestroyedAfterThreeInterceptions()
[Test] public void Missile_DamagesShieldAndGuardInArea_ButNotOwnRobot()
[Test] public void Missile_PowerOnGuardDependsOnTerrain() /* incendiario sobre defensor en Selva: 4 de daño */
[Test] public void KnownEnemyDefenses_NeverIncludesDecoys()
```
- [ ] **Step 2:** Ejecutar → FAIL.
- [ ] **Step 3:** Implementar.
- [ ] **Step 4:** Suite completa → PASS.
- [ ] **Step 5:** Commit `feat: agrega escudo antidrones, antimisiles y dano de misiles a defensas`.

### Task 5: Fase de defensa: combates, ataque a defensas fijas y avance de defensores

**Files:**
- Modify: `Rules/Match.Defense.cs` (`OnTurnEnding`), `Rules/Match.Guards.cs`, `Rules/Events.cs`, `Rules/Match.Robots.cs`
- Test: `tests/Flagstorm.Core.Tests/Rules/CombatTests.cs`

**Interfaces:**
- Consumes: Tasks 3–4.
- Produces: eventos `RobotsClashed(Side Attacker, int AttackerId, int GuardId, int DamageToGuard, int DamageToAttacker)`,
  `GuardMoved(Side Owner, int RobotId, HexCoord From, HexCoord To)`,
  `RobotHitDefense(Side Attacker, int RobotId, HexCoord Defense, int Damage)`;
  `FlagCaptured(Side By, HexCoord Cell, int TotalCaptured, string WeaponId)` (agrega el arma que capturó).
- Orden al terminar el turno del atacante A (isla del defensor D), siempre por id o `CellOrder`:
  1. Disparan las torretas de D (regla actual).
  2. **Combate:** cada defensor de D con un robot de A a distancia ≤ 1 elige el de menor id; cada robot de A con defensores a distancia ≤ 1 elige el de menor id. Los golpes son simultáneos: se calculan todos y luego se aplican. El daño es el poder del robot ajustado por el terreno de **su propia** celda.
  3. **Robots contra defensas fijas:** cada robot de A que no combatió y está a distancia ≤ 1 de un escudo, antimisiles o torreta de D (no señuelo) daña a la primera por `CellOrder` con su poder ajustado por su terreno.
  4. **Avance:** cada defensor que no combatió y tiene un robot de A dentro de su zona (distancia a su `Post` ≤ `Radius`) avanza 1 casilla hacia el más cercano por una casilla de la zona libre de otros defensores y robots; si no hay atacantes en la zona y no está en su puesto, avanza 1 hacia su puesto.
  Lo que llega a 0 PV se elimina (`RobotDestroyed`, `GuardDestroyed`, `DefenseDestroyed`).

- [ ] **Step 1: Pruebas que fallan**

```csharp
[Test] public void AdjacentRobots_ClashOncePerAttackerTurn() /* 4 PV cada uno, en terreno neutro: 2 y 2 */
[Test] public void HomeTerrain_GivesAdvantageInClash() /* defensor de nieve en Hielo: le quita 3 al atacante */
[Test] public void Clash_DestroysRobotAtZeroHitPoints()
[Test] public void TwoGuardsAgainstOneRobot_IsDeterministic() /* mismo resultado con colocación en orden inverso */
[Test] public void Guard_AdvancesOneCellTowardIntruder()
[Test] public void Guard_NeverLeavesItsZone() /* tras muchos turnos con el atacante fuera, distancia al puesto ≤ 2 */
[Test] public void Guard_ReturnsTowardPostWhenZoneIsEmpty()
[Test] public void RobotNextToShield_DamagesItEachTurn_UntilDestroyed() /* escudo 3 PV, robot poder 2: 2 turnos */
[Test] public void FlagCaptured_ReportsTheCapturingWeapon()
[Test] public void Replay_WithCombat_ReachesSameHash()
```
- [ ] **Step 2:** Ejecutar → FAIL.
- [ ] **Step 3:** Implementar en `Match.Guards.cs` (combate y avance) llamado desde `OnTurnEnding`.
- [ ] **Step 4:** Suite completa → PASS.
- [ ] **Step 5:** Commit `feat: agrega combate entre robots y avance de defensores`.

### Task 6: IA con arsenal, defensas y combate

**Files:**
- Modify: `Ai/BasicAiPlayer.cs`
- Test: `tests/Flagstorm.Core.Tests/Ai/BasicAiPlayerTests.cs`

**Interfaces:**
- Consumes: Tasks 1–5.
- Produces: `IReadOnlyList<string> ChooseLoadout(IslandMap enemyIsland, IReadOnlyList<WeaponDefinition> available, int slots)`
  (siempre válido según `LoadoutRules`; prefiere el robot de ataque del terreno más común de la isla rival y la defensa de cada ataque que lleve);
  `ChooseSetup` coloca defensas gratis de su loadout junto a banderas;
  `NextCommand` además construye escudo/antimisiles/defensores cerca de banderas, dispara misiles a defensas y defensores rivales conocidos que estorban, y elige robots por terreno.

- [ ] **Step 1: Pruebas que fallan**

```csharp
[TestCase(0)] [TestCase(3)] [TestCase(8)]
public void ChooseLoadout_IsAlwaysValid(int wins) /* Validate == None, Count == min(6, desbloqueadas) */
[Test] public void ChooseLoadout_PrefersRobotOfMostCommonEnemyTerrain() /* isla mayormente Hielo → robot-nieve */
[TestCase(0u)] [TestCase(7u)] [TestCase(42u)]
public void AiVsAi_WithStarterLoadouts_FinishesWithoutRejectedCommands(uint seed)
[TestCase(0u)] [TestCase(7u)] [TestCase(42u)]
public void AiVsAi_WithFullLoadouts_FinishesWithoutRejectedCommands(uint seed)
[Test] public void Ai_FiresMissileAtKnownGuardBlockingItsRobot()
```
Las partidas IA contra IA tienen un tope de 200 turnos; superarlo falla la prueba.

- [ ] **Step 2:** Ejecutar → FAIL.
- [ ] **Step 3:** Implementar.
- [ ] **Step 4:** Suite completa → PASS. Registrar en `.superpowers/sdd/.../progress.md` la mediana de turnos y cuántas gana Blue en 20 partidas con loadout completo.
- [ ] **Step 5:** Commit `feat: agrega ia que elige arsenal y usa defensas`.

### Task 7: Prototipo holográfico con las piezas nuevas

**Files:**
- Modify (Unity, `Presentation/`): `MatchSession.cs`, `MatchController.cs`, `HudView.cs`, `TextFormatters.cs`, `HexMeshFactory.cs`
- Test: `Tests/EditMode/MatchSessionTests.cs`, `PresentationFormattingTests.cs`, `HexMeshFactoryTests.cs`

**Interfaces:**
- Consumes: Tasks 1–6.
- Produces:
  - `MatchSession.Create(uint seed, IReadOnlyList<string> playerLoadout, IReadOnlyList<WeaponDefinition> unlocked)` (la IA elige con `ChooseLoadout` entre `unlocked`); `CreateDefault(seed)` = loadout inicial.
  - En preparación: `SelectedSetupDefenseId` (defensa elegida en la barra entre las `Defend`/`Guard` del loadout); tras 5 banderas, cada toque coloca esa defensa (hasta 2). `PendingDefenses : IReadOnlyList<DefensePlacement>` reemplaza `PendingTurrets`.
  - `MarkerShape` agrega `HollowHexagon` (escudo), `HollowTriangle` (antimisiles) y `Pentagon` (defensor).
  - `EventTextFormatter` con texto para cada evento nuevo (por ejemplo `DroneShotDown` del jugador: "Tu dron fue derribado por un escudo"; `RobotsClashed`: "Choque: tu robot −2 PV, defensor −2 PV"); `FlagCaptured` nombra el arma ("El robot anfibio rival capturó una de tus banderas (3/5)").
  - `EndReasonFormatter.Format(Side winner, Side viewer, int flagsPerSide)`: "¡Victoria! Capturaste las 5 banderas rivales" / "Derrota: el rival capturó tus 5 banderas"; el panel final lo usa.
  - Las zonas de escudos, antimisiles y defensores propios se tiñen 15 % del color del bando; los defensores muestran sus PV.

- [ ] **Step 1: Pruebas que fallan** — en EditMode:
  `EveryNewEventHasText` (todos los eventos nuevos dan texto no vacío para ambos bandos),
  `EndReason_ExplainsDefeat`, `Setup_PlacesSelectedFreeDefense`, `Setup_CannotSelectAttackWeaponAsDefense`,
  `EveryShapeFacesUp` (ya existe; cubre las formas nuevas).
- [ ] **Step 2:** EditMode → FAIL.
- [ ] **Step 3:** Implementar y actualizar los usos de `OwnTurrets`/`OwnDecoys`/`KnownEnemyTurrets`.
- [ ] **Step 4:** EditMode → PASS; PlayMode (`PrototypeSceneSmokeTests`, ajustado al loadout inicial: dron, robot anfibio y una defensa gratis) → PASS; revisar capturas en `TestResults/screens`.
- [ ] **Step 5:** Commit `feat: muestra defensas, combates y causa de la derrota en el prototipo`.

### Task 8: Selección de arsenal, desbloqueo y APK

**Files:**
- Create: `Presentation/ProgressStore.cs`, `Presentation/LoadoutView.cs`
- Modify: `Presentation/MatchController.cs`, `Tests/PlayMode/PrototypeSceneSmokeTests.cs`, `README.md` (ambiente de pruebas con la ruta y versión nuevas de Unity, usage/examples), `docs/playtests/2026-11-12-prototipo.md`
- Test: `Tests/EditMode/ProgressStoreTests.cs`

**Interfaces:**
- Consumes: Tasks 1–7.
- Produces:
  - `sealed class ProgressStore { int Wins { get; } void RecordWin(); void Reset(); }` sobre `PlayerPrefs`, clave `"flagstorm.victorias"`; sin datos personales.
  - `LoadoutView` (UI Toolkit en código, botones ≥ 44 px con texto visible): lista de armas desbloqueadas con nombre, costo y terreno; se marcan hasta 6; "Jugar" se habilita solo si `LoadoutRules.Validate == None` y muestra el motivo si no. Si hay 6 o menos desbloqueadas, se omite y se usan todas. En builds de desarrollo muestra "Reiniciar progreso".
  - Al ganar, `RecordWin()` y el panel final avisa qué se desbloqueó ("Nuevo: misil estándar y antimisiles").

- [ ] **Step 1: Pruebas que fallan** — `ProgressStoreTests`: `StartsAtZero_AfterReset`, `RecordWin_Increments`, `Persists_AcrossInstances`.
- [ ] **Step 2:** EditMode → FAIL.
- [ ] **Step 3:** Implementar; la prueba PlayMode llama `Reset()` al empezar y al terminar.
- [ ] **Step 4:** EditMode y PlayMode → PASS; `Flagstorm → Compilar APK de desarrollo` por lotes → `Succeeded`; copiar a `/sdcard/Download` con `adb push` (`MSYS_NO_PATHCONV=1`).
- [ ] **Step 5:** Commit `feat: agrega seleccion de arsenal y desbloqueo por victorias`; pedir a Oscar la prueba de juego y registrar resultados en `docs/playtests/`.
