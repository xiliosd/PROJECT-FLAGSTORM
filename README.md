# Flagstorm

Juego móvil de estrategia por turnos sobre islas para Android (iPhone pospuesto). Cada bando esconde
5 banderas en su isla y usa drones, misiles y robots especializados por terreno para capturar las
del rival. Nombre de trabajo; falta la verificación oficial de marca.

Estado: **etapa 1, prototipo de reglas** (núcleo de reglas en C# + escena holográfica en Unity).

## Enlaces de documentación

- Documento de diseño: [docs/superpowers/specs/2026-11-09-flagstorm-design.md](docs/superpowers/specs/2026-11-09-flagstorm-design.md)
- Repositorio: https://github.com/xiliosd/PROJECT-FLAGSTORM
- Plan de la etapa 1: [docs/superpowers/plans/2026-11-09-flagstorm-etapa-1-prototipo.md](docs/superpowers/plans/2026-11-09-flagstorm-etapa-1-prototipo.md)

## Referencias de API

No aplica: el juego de un jugador no expone ni consume APIs en esta etapa.

## Autores

- Oscar Dueñas, con asistencia de IA (Claude Code).

## Distintivos

No aplica todavía (sin CI configurado).

## Ambiente de pruebas

Requiere el .NET SDK 10.0.401 o superior.

```bash
dotnet test                                        # pruebas del núcleo de reglas
dotnet test --collect:"XPlat Code Coverage"        # con cobertura (meta ≥ 60 %)
dotnet format --verify-no-changes                  # estilo (StyleCop + .editorconfig)
```

Pruebas EditMode de Unity (desde la raíz del repo):

```bash
"D:/TRABAJO/UnityEditor/6000.3.28f1/Editor/Unity.exe" -batchmode -nographics -projectPath unity/Flagstorm -runTests -testPlatform EditMode -testResults TestResults/editmode.xml
```

Unity 6000.3.28f1 está instalado en `D:/TRABAJO/UnityEditor`. Para PlayMode usa
`-testPlatform PlayMode` (sin `-nographics`). Con el proyecto abierto en el editor, el modo por lotes
falla: ciérralo antes. El APK de desarrollo se compila con el menú `Flagstorm → Compilar APK de
desarrollo` o por lotes con `-quit -executeMethod Flagstorm.EditorTools.AndroidBuilder.BuildDevelopmentApk`
(sale en `unity/Flagstorm/Builds/Android/flagstorm-dev.apk`).

## Deployment

No aplica en esta etapa (prototipo local, sin publicación en tiendas).

## Environment variables

No aplica: el núcleo no usa variables de entorno ni credenciales.

## Screenshots

Pendiente (tarea 12 del plan).

## Tech stack

- Núcleo de reglas: C# 9, .NET Standard 2.1 (`packages/com.flagstorm.core`, compilado por
  `src/Flagstorm.Core` para dotnet y como paquete local por Unity).
- Pruebas: NUnit 4.6.1, NUnit3TestAdapter 6.3.1, Microsoft.NET.Test.Sdk 18.11.0,
  coverlet.collector 10.1.0.
- Estilo: StyleCop.Analyzers 1.2.0-beta.556 + `.editorconfig`.
- Motor: Unity 6000.3.28f1 + URP 17.6.0 + Input System 1.20.1 + Test Framework 1.8.0 (`unity/Flagstorm`).

### Excepciones a los lineamientos

- **Nombres:** se siguen las convenciones oficiales de C# y Unity: archivos, tipos, métodos y
  constantes en PascalCase, y campos privados en `_camelCase`. Unity exige que el archivo se llame
  igual que su clase.
- **Proyecto personal:** no se usa Docker y se trabaja directamente en `main`.

## Usage / examples

```csharp
var match = new Match(PrototypeIslands.CreateDefault(), PrototypeIslands.CreateDefault(),
    RulesConfig.Default, PrototypeArsenal.All);
match.Submit(Side.Blue, new SetupCommand(flags, turrets));
match.Submit(Side.Blue, new LaunchDroneCommand(PrototypeArsenal.Drone, new HexCoord(1, 1)));
```

### Arsenal y progreso en el juego

- Cada victoria desbloquea el siguiente nivel de armas (la primera: misil estándar y antimisiles). El
  progreso es solo el número de victorias, guardado en el dispositivo con la clave
  `flagstorm.victorias`; no se guarda ningún dato personal.
- Antes de cada partida, si hay más de 6 armas desbloqueadas, aparece la pantalla "Elige tu arsenal":
  se marcan hasta 6 y "Jugar" solo se habilita con un arsenal válido (al menos un robot de ataque).
  Con 6 armas o menos se juega con todas.
- Al ganar, el panel final avisa qué se desbloqueó ("Nuevo: ...").
- En builds de desarrollo, la pantalla de arsenal muestra "Reiniciar progreso" para volver a 0 victorias.
