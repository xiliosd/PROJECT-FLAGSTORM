using System.Globalization;
using Flagstorm.Core.Arsenal;
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
                case RejectionReason.InvalidSetup: return "Coloca exactamente 5 banderas y hasta 2 torretas en tu isla";
                case RejectionReason.OutOfIsland: return "Esa casilla no es válida para esta acción";
                case RejectionReason.NotEnoughEnergy: return "No tienes energía suficiente";
                case RejectionReason.UnknownWeapon: return "Arma desconocida";
                case RejectionReason.WrongCategory: return "Esa arma no sirve para esta acción";
                case RejectionReason.NotCoast: return "Los robots solo desembarcan en la costa";
                case RejectionReason.InvalidPath: return "El robot no puede llegar hasta ahí este turno";
                case RejectionReason.UnknownRobot: return "Ese robot ya no existe";
                case RejectionReason.CellOccupied: return "Esa casilla está ocupada";
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
                    return $"{Owner(damaged.Owner, viewer)} {NameOf(damaged.WeaponId)} "
                        + $"quedó con {damaged.RemainingHitPoints} PV";
                case DefenseDestroyed destroyed:
                    return $"{Owner(destroyed.Owner, viewer)} {NameOf(destroyed.WeaponId)} fue destruida";
                case RobotDeployed deployed:
                    return deployed.Owner == viewer
                        ? $"Desembarcaste: {NameOf(deployed.WeaponId)}"
                        : "¡Un robot rival desembarcó en tu costa!";
                case RobotMoved moved:
                    return moved.Owner == viewer ? $"Tu robot avanzó {moved.Path.Count} casillas" : null;
                case FlagCaptured captured:
                    return captured.By == viewer
                        ? $"¡Capturaste una bandera! ({captured.TotalCaptured}/{flagsPerSide})"
                        : $"El rival capturó una de tus banderas ({captured.TotalCaptured}/{flagsPerSide})";
                case DecoyExposed exposed:
                    return exposed.By == viewer ? "Era un señuelo" : "El rival descubrió uno de tus señuelos";
                case MatchWon won:
                    return won.Winner == viewer ? "¡Victoria!" : "Derrota";
                case DefenseBuilt built:
                    return built.Owner == viewer ? $"Construiste: {NameOf(built.WeaponId)}" : null;
                case TurretFired fired:
                    return fired.Owner == viewer
                        ? $"Tu torreta disparó (−{fired.Damage} PV)"
                        : $"Una torreta rival dañó a tu robot (−{fired.Damage} PV)";
                case RobotDestroyed destroyedRobot:
                    return destroyedRobot.Owner == viewer ? "Perdiste un robot" : "Destruiste un robot rival";
                case SetupCompleted setup:
                    return setup.Side == viewer ? "Tus banderas están escondidas" : "El rival escondió sus banderas";
                case EnergyChanged _:
                    return null;
                default:
                    return matchEvent.GetType().Name;
            }
        }

        private static string Owner(Side owner, Side viewer) => owner == viewer ? "Tu" : "La";

        private static string NameOf(string weaponId)
        {
            foreach (var weapon in PrototypeArsenal.All)
            {
                if (weapon.Id == weaponId)
                {
                    return weapon.DisplayName.ToLowerInvariant();
                }
            }

            return weaponId;
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
                    + $"{match.Rules.FlagsPerSide} y luego poner torretas {session.PendingTurrets.Count}/"
                    + $"{match.Rules.InitialFreeTurrets}. Después pulsa «Confirmar banderas».";
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
                default:
                    return "Elige un arma abajo o toca uno de tus robots";
            }
        }
    }
}
