using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Flagstorm.Core.Arsenal;
using Flagstorm.Core.Map;
using Flagstorm.Core.Rules;

namespace Flagstorm.Presentation
{
    public static class ModifierLabelFormatter
    {
        public static string Format(int percent)
        {
            if (percent == 0)
            {
                return string.Empty;
            }

            // Signo menos tipográfico (U+2212): más legible que el guion en tamaños pequeños.
            var sign = percent > 0 ? "+" : "−";
            return sign + System.Math.Abs(percent).ToString(CultureInfo.InvariantCulture) + " %";
        }
    }

    public static class RejectionTextFormatter
    {
        public static string Format(RejectionReason reason)
        {
            switch (reason)
            {
                case RejectionReason.WrongPhase: return "Eso no se puede hacer en esta fase";
                case RejectionReason.NotYourTurn: return "Espera tu turno";
                case RejectionReason.MatchFinished: return "La partida ya terminó";
                case RejectionReason.InvalidSetup:
                    return "Coloca exactamente 5 banderas y hasta 2 defensas gratis en tu isla";
                case RejectionReason.OutOfIsland: return "Esa casilla no es válida para esta acción";
                case RejectionReason.NotEnoughEnergy: return "No tienes energía suficiente";
                case RejectionReason.UnknownWeapon: return "Arma desconocida";
                case RejectionReason.WrongCategory: return "Esa arma no sirve para esta acción";
                case RejectionReason.NotCoast: return "Los robots solo desembarcan en la costa";
                case RejectionReason.InvalidPath: return "El robot no puede llegar hasta ahí este turno";
                case RejectionReason.UnknownRobot: return "Ese robot ya no existe";
                case RejectionReason.CellOccupied: return "Esa casilla está ocupada";
                case RejectionReason.NotInLoadout: return "Esa arma no está en tu arsenal de la partida";
                default: return string.Empty;
            }
        }
    }

    public static class EventTextFormatter
    {
        // Devuelve null para eventos que no merecen una línea en el registro.
        public static string Format(IMatchEvent matchEvent, Side viewer, int flagsPerSide)
        {
            switch (matchEvent)
            {
                case MatchStarted _:
                    return "¡Comienza la partida!";
                case TurnEnded ended:
                    return ended.NextSide == viewer ? "Tu turno" : "Turno del rival";
                case CellsRevealed revealed:
                    return revealed.By == viewer
                        ? $"Tu dron reveló {revealed.Cells.Count} casillas"
                        : "Un dron rival sobrevoló tu isla";
                case MissileImpact impact:
                    return impact.By == viewer
                        ? $"Disparaste: {NameOf(impact.WeaponId)}"
                        : $"¡Impacto rival en tu isla! ({NameOf(impact.WeaponId)})";
                case DefenseDamaged damaged:
                    return $"{DefenseOf(damaged.Owner, viewer)} ({NameOf(damaged.WeaponId)}) "
                        + $"quedó con {damaged.RemainingHitPoints} PV";
                case DefenseDestroyed destroyed:
                    return $"{DefenseOf(destroyed.Owner, viewer)} ({NameOf(destroyed.WeaponId)}) fue destruida";
                case RobotDeployed deployed:
                    return deployed.Owner == viewer
                        ? $"Desembarcaste: {NameOf(deployed.WeaponId)}"
                        : "¡Un robot rival desembarcó en tu costa!";
                case RobotMoved moved:
                    return moved.Owner == viewer ? $"Tu robot avanzó {moved.Path.Count} casillas" : null;
                case FlagCaptured captured:
                    return captured.By == viewer
                        ? $"¡Tu {NameOf(captured.WeaponId)} capturó una bandera! "
                            + $"({captured.TotalCaptured}/{flagsPerSide})"
                        : $"El {NameOf(captured.WeaponId)} rival capturó una de tus banderas "
                            + $"({captured.TotalCaptured}/{flagsPerSide})";
                case DecoyExposed exposed:
                    return exposed.By == viewer ? "Era un señuelo" : "El rival descubrió uno de tus señuelos";
                case MatchWon won:
                    return EndReasonFormatter.Format(won.Winner, viewer, flagsPerSide);
                case DefenseBuilt built:
                    return built.Owner == viewer ? $"Construiste: {NameOf(built.WeaponId)}" : null;
                case TurretFired fired:
                    return fired.Owner == viewer
                        ? $"Tu torreta disparó (−{fired.Damage} PV)"
                        : $"Una torreta rival dañó a tu robot (−{fired.Damage} PV)";
                case RobotDestroyed destroyedRobot:
                    return destroyedRobot.Owner == viewer ? "Perdiste un robot" : "Destruiste un robot rival";
                case DroneShotDown shotDown:
                    return shotDown.By == viewer
                        ? "Tu dron fue derribado por un escudo"
                        : "Tu escudo derribó un dron rival";
                case MissileIntercepted intercepted:
                    return intercepted.By == viewer
                        ? $"Tu {NameOf(intercepted.WeaponId)} fue interceptado por unos antimisiles"
                        : $"Tus antimisiles interceptaron un {NameOf(intercepted.WeaponId)} rival "
                            + $"(quedan {intercepted.RemainingHitPoints} PV)";
                case GuardPlaced placed:
                    return placed.Owner == viewer
                        ? $"Colocaste: {NameOf(placed.WeaponId)}"
                        : "El rival colocó un defensor";
                case GuardMoved guardMoved:
                    return guardMoved.Owner == viewer ? "Tu defensor avanzó" : "Un defensor rival avanzó";
                case GuardDamaged guardDamaged:
                    return $"{(guardDamaged.Owner == viewer ? "Tu defensor" : "Un defensor rival")} "
                        + $"quedó con {guardDamaged.RemainingHitPoints} PV";
                case GuardDestroyed guardDestroyed:
                    return guardDestroyed.Owner == viewer ? "Perdiste un defensor" : "Destruiste un defensor rival";
                case RobotsClashed clash:
                    return clash.Attacker == viewer
                        ? $"Choque: tu robot −{clash.DamageToAttacker} PV, defensor −{clash.DamageToGuard} PV"
                        : $"Choque: tu defensor −{clash.DamageToGuard} PV, robot rival −{clash.DamageToAttacker} PV";
                case RobotHitDefense hit:
                    return hit.Attacker == viewer
                        ? $"Tu robot golpeó una defensa rival (−{hit.Damage} PV)"
                        : $"Un robot rival golpeó una de tus defensas (−{hit.Damage} PV)";
                case SetupCompleted setup:
                    return setup.Side == viewer ? "Tus banderas están escondidas" : "El rival escondió sus banderas";
                case EnergyChanged _:
                    return null;
                default:
                    return matchEvent.GetType().Name;
            }
        }

        private static string DefenseOf(Side owner, Side viewer) =>
            owner == viewer ? "Tu defensa" : "Una defensa rival";

        private static string NameOf(string weaponId)
        {
            foreach (var weapon in PrototypeArsenal.All)
            {
                if (weapon.Id == weaponId)
                {
                    return weapon.DisplayName.ToLowerInvariant();
                }
            }

            return weaponId ?? "robot";
        }
    }

    public static class EndReasonFormatter
    {
        // La única forma de ganar es capturar todas las banderas rivales, así que esa es también la causa.
        public static string Format(Side winner, Side viewer, int flagsPerSide) =>
            winner == viewer
                ? $"¡Victoria! Capturaste las {flagsPerSide} banderas rivales"
                : $"Derrota: el rival capturó tus {flagsPerSide} banderas";
    }

    // El núcleo emite todos los eventos (para repeticiones); el registro del jugador solo cuenta lo que él ve.
    // Regla única para piezas rivales (defensas fijas y defensores): "destruida" se cuenta siempre y sin
    // coordenada; "dañada" o "se movió" solo si la pieza está a la vista del jugador.
    public static class EventVisibility
    {
        public static IReadOnlyList<IMatchEvent> Filter(
            IEnumerable<IMatchEvent> events,
            Side viewer,
            IEnumerable<int> knownEnemyGuardIds,
            IEnumerable<HexCoord> knownEnemyDefenseCells)
        {
            var all = events.ToList();
            var knownGuards = new HashSet<int>(knownEnemyGuardIds);
            var knownDefenses = new HashSet<HexCoord>(knownEnemyDefenseCells);

            // Un defensor que chocó con un robot del jugador queda a la vista aunque luego caiga destruido.
            foreach (var clash in all.OfType<RobotsClashed>().Where(c => c.Attacker == viewer))
            {
                knownGuards.Add(clash.GuardId);
            }

            return all.Where(e => IsVisible(e, viewer, knownGuards, knownDefenses)).ToList();
        }

        private static bool IsVisible(
            IMatchEvent matchEvent, Side viewer, ISet<int> knownGuardIds, ISet<HexCoord> knownDefenseCells)
        {
            switch (matchEvent)
            {
                case GuardPlaced placed:
                    return placed.Owner == viewer;
                case GuardMoved moved:
                    return moved.Owner == viewer || knownGuardIds.Contains(moved.RobotId);
                case GuardDamaged damaged:
                    return damaged.Owner == viewer || knownGuardIds.Contains(damaged.RobotId);
                case DefenseDamaged defenseDamaged:
                    return defenseDamaged.Owner == viewer || knownDefenseCells.Contains(defenseDamaged.Cell);
                default:
                    return true;
            }
        }
    }

    public static class WeaponCaptionFormatter
    {
        // En la preparación las defensas son gratis (cuota inicial); en juego cuestan su energía.
        public static string Format(WeaponDefinition weapon, string modifier, bool free)
        {
            var cost = free ? "gratis" : $"{weapon.EnergyCost} energía";
            return string.IsNullOrEmpty(modifier)
                ? $"{weapon.DisplayName}\n{cost}"
                : $"{weapon.DisplayName}\n{cost} · {modifier}";
        }
    }

    public static class HintTextFormatter
    {
        // Indica el siguiente paso posible: el prototipo no tiene tutorial, así que la pista lo reemplaza.
        public static string Format(MatchSession session)
        {
            var match = session.Match;
            if (match.Phase == MatchPhase.Setup)
            {
                return $"Toca tu isla (abajo) para esconder banderas {session.PendingFlags.Count}/"
                    + $"{match.Rules.FlagsPerSide} y luego poner la defensa elegida {session.PendingDefenses.Count}/"
                    + $"{match.Rules.InitialFreeDefenses}. Después pulsa «Confirmar banderas».";
            }

            if (match.Phase == MatchPhase.Finished)
            {
                return "La partida terminó";
            }

            if (session.IsAiTurn)
            {
                return "El rival está jugando…";
            }

            if (session.SelectedRobotId.HasValue)
            {
                return "Toca una casilla de la isla rival para mover el robot";
            }

            var weapon = session.WeaponById(session.SelectedWeaponId);
            switch (weapon?.Category)
            {
                case WeaponCategory.Reveal:
                    return "Toca la isla rival (arriba) para lanzar el dron";
                case WeaponCategory.Destroy:
                    return "Toca la isla rival (arriba) para disparar el misil";
                case WeaponCategory.Capture:
                    return "Toca una casilla de la costa rival para desembarcar";
                case WeaponCategory.Defend:
                    return "Toca una casilla libre de tu isla para construir";
                case WeaponCategory.Guard:
                    return "Toca una casilla libre de tu isla para colocar el defensor";
                default:
                    return "Elige un arma abajo o toca uno de tus robots";
            }
        }
    }
}
