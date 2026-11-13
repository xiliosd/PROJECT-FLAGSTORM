using System.Collections;
using System.IO;
using System.Linq;
using Flagstorm.Core.Arsenal;
using Flagstorm.Core.Rules;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Flagstorm.Presentation.PlayModeTests
{
    // Recorre el flujo del paso 5 de la tarea 11 sin manos: cualquier error en consola hace fallar la prueba.
    public class PrototypeSceneSmokeTests
    {
        private const string SceneName = "PrototypeHolo";

        // Tope de espera para el turno de la IA (0,6 s por jugada).
        private const float AiTurnTimeoutSeconds = 30f;

        [SetUp]
        public void ResetProgressBefore() => new ProgressStore().Reset();

        [TearDown]
        public void ResetProgressAfter() => new ProgressStore().Reset();

        // Con 2 victorias hay 8 armas: aparece la selección de arsenal y "Jugar" exige un robot de ataque.
        [UnityTest]
        public IEnumerator ShowsLoadoutWhenMoreThanSixWeaponsAreUnlocked()
        {
            var store = new ProgressStore();
            store.RecordWin();
            store.RecordWin();
            yield return SceneManager.LoadSceneAsync(SceneName);
            yield return null;
            yield return null;

            var controller = Object.FindAnyObjectByType<MatchController>();
            var loadout = controller.Loadout;
            Assert.That(loadout, Is.Not.Null, "Debe mostrarse la selección de arsenal");
            Assert.That(controller.Session, Is.Null, "La partida no empieza hasta pulsar Jugar");
            Assert.That(loadout.CanPlay, Is.False);
            Assert.That(loadout.ReasonText, Does.Contain("robot de ataque"));

            loadout.Toggle(PrototypeArsenal.Drone);
            Assert.That(loadout.CanPlay, Is.False, "Sin robot de ataque no se puede jugar");
            loadout.Toggle(PrototypeArsenal.JungleRobot);
            Assert.That(loadout.CanPlay, Is.True);
            loadout.Click();
            yield return null;

            Assert.That(controller.Loadout, Is.Null);
            Assert.That(controller.Session, Is.Not.Null);
            Assert.That(controller.Session.PlayerWeapons.Select(w => w.Id),
                Is.EqualTo(new[] { PrototypeArsenal.Drone, PrototypeArsenal.JungleRobot }));
        }

        [UnityTest]
        public IEnumerator PlaysSetupDroneRobotAndAiTurnWithoutErrors()
        {
            yield return SceneManager.LoadSceneAsync(SceneName);
            yield return null;
            yield return null;

            var controller = Object.FindAnyObjectByType<MatchController>();
            Assert.That(controller, Is.Not.Null, "La escena debe tener un MatchController");
            var session = controller.Session;
            Assert.That(session.Match.Phase, Is.EqualTo(MatchPhase.Setup));
            yield return Capture("01-preparacion");

            // Loadout inicial: dron, escudo, robot anfibio y defensor. Se ponen 5 banderas, el escudo y el defensor.
            var setupCells = session.View.OwnIsland.InteriorCells.Take(7).ToList();
            foreach (var cell in setupCells.Take(6))
            {
                controller.OnCellTapped(controller.OwnIsland, cell);
            }

            controller.OnSetupDefenseSelected(PrototypeArsenal.BeachGuard);
            controller.OnCellTapped(controller.OwnIsland, setupCells[6]);
            Assert.That(session.PendingDefenses.Select(p => p.WeaponId),
                Is.EqualTo(new[] { PrototypeArsenal.DroneShield, PrototypeArsenal.BeachGuard }));
            yield return Capture("02-banderas-marcadas");
            controller.ConfirmSetup();
            Assert.That(session.IsHumanTurn, Is.True, "Tras confirmar, empieza el jugador");
            Assert.That(session.View.OwnGuards, Has.Count.EqualTo(1), "El defensor gratis queda en la isla");
            yield return Capture("02b-defensas-colocadas");

            controller.OnWeaponSelected(PrototypeArsenal.Drone);
            controller.OnCellTapped(controller.EnemyIsland, session.View.EnemyIsland.InteriorCells[4]);
            Assert.That(session.View.RevealedEnemyCells, Is.Not.Empty, "El dron revela casillas");

            controller.OnWeaponSelected(PrototypeArsenal.BeachRobot);
            controller.OnCellTapped(controller.EnemyIsland, session.View.EnemyIsland.CoastCells[0]);
            Assert.That(session.View.OwnRobots, Is.Not.Empty, "El robot desembarca");
            controller.OnCellTapped(controller.EnemyIsland, session.View.EnemyIsland.CoastCells[0]);
            yield return Capture("03-dron-y-robot");

            controller.EndTurn();
            Assert.That(session.IsAiTurn, Is.True);
            var waited = 0f;
            while (session.IsAiTurn && waited < AiTurnTimeoutSeconds)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }

            var aiDone = session.IsHumanTurn || session.Match.Phase == MatchPhase.Finished;
            Assert.That(aiDone, Is.True, "La IA termina su turno");
            yield return Capture("04-despues-de-la-ia");
        }

        // Guarda una captura en TestResults/screens (fuera de Assets). En modo por lotes no hay fin de cuadro,
        // así que se dibujan la cámara y el HUD en texturas y se combinan a mano.
        private static IEnumerator Capture(string name)
        {
            const int Width = 1080;
            const int Height = 2160;
            var camera = Camera.main;
            var document = Object.FindAnyObjectByType<UIDocument>();
            var world = new RenderTexture(Width, Height, 24);
            var hud = new RenderTexture(Width, Height, 24);
            var originalTarget = document.panelSettings.targetTexture;

            camera.targetTexture = world;
            document.panelSettings.targetTexture = hud;
            yield return null;
            yield return null;
            yield return null;
            camera.Render();

            var worldPixels = ReadPixels(world);
            var hudPixels = ReadPixels(hud);
            camera.targetTexture = null;
            document.panelSettings.targetTexture = originalTarget;
            for (var i = 0; i < worldPixels.Length; i++)
            {
                var top = hudPixels[i];
                worldPixels[i] = Color.Lerp(worldPixels[i], new Color(top.r, top.g, top.b, 1f), top.a);
            }

            var image = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            image.SetPixels(worldPixels);
            var folder = Path.GetFullPath(
                Path.Combine(Application.dataPath, "..", "..", "..", "TestResults", "screens"));
            Directory.CreateDirectory(folder);
            File.WriteAllBytes(Path.Combine(folder, name + ".png"), image.EncodeToPNG());
            Object.Destroy(image);
            world.Release();
            hud.Release();
        }

        private static Color[] ReadPixels(RenderTexture source)
        {
            var previous = RenderTexture.active;
            RenderTexture.active = source;
            var texture = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
            texture.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
            texture.Apply();
            RenderTexture.active = previous;
            var pixels = texture.GetPixels();
            Object.Destroy(texture);
            return pixels;
        }
    }
}
