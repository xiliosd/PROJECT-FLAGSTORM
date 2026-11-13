using System;
using System.Linq;
using Flagstorm.Core.Arsenal;
using Flagstorm.Core.Map;
using Flagstorm.Core.Rules;
using NUnit.Framework;

namespace Flagstorm.Presentation.Tests
{
    public class PresentationFormattingTests
    {
        [Test]
        public void Modifier_UsesTypographicMinus() =>
            Assert.That(ModifierLabelFormatter.Format(-50), Is.EqualTo("−50 %"));

        [Test]
        public void Modifier_PositiveAndZero()
        {
            Assert.That(ModifierLabelFormatter.Format(40), Is.EqualTo("+40 %"));
            Assert.That(ModifierLabelFormatter.Format(0), Is.EqualTo(string.Empty));
        }

        [Test]
        public void EveryTerrainHasLabelAndEveryRejectionHasText()
        {
            foreach (TerrainType terrain in Enum.GetValues(typeof(TerrainType)))
            {
                Assert.That(HoloPalette.TerrainLabel(terrain), Is.Not.Empty, terrain.ToString());
            }

            foreach (RejectionReason reason in Enum.GetValues(typeof(RejectionReason)))
            {
                if (reason != RejectionReason.None)
                {
                    Assert.That(RejectionTextFormatter.Format(reason), Is.Not.Empty, reason.ToString());
                }
            }
        }

        [Test]
        public void TerrainLabels_AreDistinct()
        {
            var labels = new System.Collections.Generic.HashSet<string>();
            foreach (TerrainType terrain in Enum.GetValues(typeof(TerrainType)))
            {
                Assert.That(labels.Add(HoloPalette.TerrainLabel(terrain)), Is.True, terrain.ToString());
            }
        }

        [Test]
        public void CaptureEvent_TextDependsOnViewer()
        {
            var captured = new FlagCaptured(Side.Blue, new HexCoord(1, 1), 3, PrototypeArsenal.BeachRobot);
            Assert.That(
                EventTextFormatter.Format(captured, Side.Blue, 5),
                Is.EqualTo("¡Tu robot anfibio capturó una bandera! (3/5)"));
            Assert.That(
                EventTextFormatter.Format(captured, Side.Red, 5),
                Is.EqualTo("El robot anfibio rival capturó una de tus banderas (3/5)"));
        }

        [Test]
        public void EveryEventType_HasText()
        {
            var cell = new HexCoord(0, 0);
            var events = new IMatchEvent[]
            {
                new SetupCompleted(Side.Red),
                new MatchStarted(Side.Blue),
                new TurnEnded(Side.Red, 2),
                new CellsRevealed(Side.Blue, new[] { cell }),
                new MissileImpact(Side.Blue, "misil-estandar", cell, new[] { cell }),
                new DefenseDamaged(Side.Red, cell, "torreta", 2),
                new DefenseDestroyed(Side.Red, cell, "torreta"),
                new RobotDeployed(Side.Blue, 1, "robot-orugas", cell),
                new RobotMoved(Side.Blue, 1, new[] { cell }),
                new FlagCaptured(Side.Blue, cell, 1, PrototypeArsenal.BeachRobot),
                new DecoyExposed(Side.Blue, cell),
                new MatchWon(Side.Blue),
                new DefenseBuilt(Side.Blue, cell, "torreta"),
                new TurretFired(Side.Red, cell, 1, 2),
                new RobotDestroyed(Side.Blue, 1, cell),
            };
            foreach (var matchEvent in events)
            {
                var text = EventTextFormatter.Format(matchEvent, Side.Blue, 5);
                Assert.That(text, Is.Not.Null, matchEvent.GetType().Name);
            }

            // La energía ya se ve en el HUD; no ensucia el registro.
            Assert.That(EventTextFormatter.Format(new EnergyChanged(Side.Blue, 4), Side.Blue, 5), Is.Null);
        }

        [Test]
        public void EveryNewEventHasText()
        {
            var cell = new HexCoord(0, 0);
            var events = new IMatchEvent[]
            {
                new DroneShotDown(Side.Blue, cell, cell),
                new MissileIntercepted(Side.Blue, PrototypeArsenal.StandardMissile, cell, cell, 2),
                new GuardPlaced(Side.Blue, 3, PrototypeArsenal.BeachGuard, cell),
                new GuardMoved(Side.Blue, 3, cell, new HexCoord(1, 0)),
                new GuardDamaged(Side.Blue, 3, 2),
                new GuardDestroyed(Side.Blue, 3, cell),
                new RobotsClashed(Side.Blue, 1, 3, 2, 2, 2, 2),
                new RobotHitDefense(Side.Blue, 1, cell, 2),
                new FlagCaptured(Side.Blue, cell, 1, PrototypeArsenal.BeachRobot),
            };
            foreach (var matchEvent in events)
            {
                foreach (var viewer in new[] { Side.Blue, Side.Red })
                {
                    Assert.That(
                        EventTextFormatter.Format(matchEvent, viewer, 5),
                        Is.Not.Null.And.Not.Empty,
                        $"{matchEvent.GetType().Name} para {viewer}");
                }
            }
        }

        [Test]
        public void NewEvents_UseTheExpectedWording()
        {
            var cell = new HexCoord(0, 0);

            Assert.That(
                EventTextFormatter.Format(new DroneShotDown(Side.Blue, cell, cell), Side.Blue, 5),
                Is.EqualTo("Tu dron fue derribado por un escudo"));
            Assert.That(
                EventTextFormatter.Format(new RobotsClashed(Side.Blue, 1, 3, 2, 2, 2, 2), Side.Blue, 5),
                Is.EqualTo("Choque: tu robot −2 PV, defensor −2 PV"));
        }

        [Test]
        public void EndReason_ExplainsDefeat()
        {
            Assert.That(
                EndReasonFormatter.Format(Side.Blue, Side.Blue, 5),
                Is.EqualTo("¡Victoria! Capturaste las 5 banderas rivales"));
            Assert.That(
                EndReasonFormatter.Format(Side.Red, Side.Blue, 5),
                Is.EqualTo("Derrota: el rival capturó tus 5 banderas"));
            Assert.That(
                EventTextFormatter.Format(new MatchWon(Side.Red), Side.Blue, 5),
                Is.EqualTo("Derrota: el rival capturó tus 5 banderas"));
        }

        [Test]
        public void EventLog_HidesEnemyGuardsThePlayerCannotSee()
        {
            var cell = new HexCoord(0, 0);
            var events = new IMatchEvent[]
            {
                new GuardPlaced(Side.Red, 10, PrototypeArsenal.BeachGuard, cell),
                new GuardMoved(Side.Red, 10, cell, new HexCoord(1, 0)),
                new GuardDamaged(Side.Red, 10, 2),
                new GuardDestroyed(Side.Red, 10, cell),
                new GuardMoved(Side.Red, 11, cell, new HexCoord(1, 0)),
                new GuardMoved(Side.Blue, 12, cell, new HexCoord(1, 0)),
                new DefenseDestroyed(Side.Red, cell, PrototypeArsenal.Turret),
            };

            var visible = EventVisibility.Filter(events, Side.Blue, new[] { 11 });

            Assert.That(visible.OfType<GuardPlaced>(), Is.Empty);
            Assert.That(visible.OfType<GuardDamaged>(), Is.Empty);
            Assert.That(visible.OfType<GuardDestroyed>(), Is.Empty);
            Assert.That(visible.OfType<GuardMoved>().Select(m => m.RobotId), Is.EqualTo(new[] { 11, 12 }));
            Assert.That(visible.OfType<DefenseDestroyed>(), Is.Not.Empty);
        }

        [Test]
        public void EventLog_ShowsAnEnemyGuardThatClashedWithAPlayerRobot()
        {
            var cell = new HexCoord(0, 0);
            var events = new IMatchEvent[]
            {
                new RobotsClashed(Side.Blue, 1, 10, 2, 2, 2, 0),
                new GuardDestroyed(Side.Red, 10, cell),
            };

            var visible = EventVisibility.Filter(events, Side.Blue, new int[0]);

            Assert.That(visible, Has.Count.EqualTo(2));
        }

        [Test]
        public void EventLog_KeepsEveryOwnGuardEvent()
        {
            var cell = new HexCoord(0, 0);
            var events = new IMatchEvent[]
            {
                new GuardPlaced(Side.Blue, 5, PrototypeArsenal.BeachGuard, cell),
                new GuardMoved(Side.Blue, 5, cell, cell),
                new GuardDamaged(Side.Blue, 5, 1),
                new GuardDestroyed(Side.Blue, 5, cell),
                new RobotsClashed(Side.Red, 9, 5, 2, 2, 2, 2),
            };

            Assert.That(EventVisibility.Filter(events, Side.Blue, new int[0]), Has.Count.EqualTo(5));
        }

        [Test]
        public void Hint_DuringSetupCountsPendingFlags()
        {
            var session = MatchSession.CreateDefault(3);
            session.TapOwnCell(session.View.OwnIsland.InteriorCells[0]);

            Assert.That(HintTextFormatter.Format(session), Does.Contain("banderas 1/5"));
        }

        [Test]
        public void Hint_FollowsTheSelectedWeapon()
        {
            var session = MatchSession.CreateDefault(3);
            foreach (var cell in session.View.OwnIsland.InteriorCells.Take(5))
            {
                session.TapOwnCell(cell);
            }

            session.ConfirmSetup();
            Assert.That(HintTextFormatter.Format(session), Does.Contain("Elige un arma"));

            session.SelectWeapon(PrototypeArsenal.Drone);
            Assert.That(HintTextFormatter.Format(session), Does.Contain("dron"));
        }
    }
}
