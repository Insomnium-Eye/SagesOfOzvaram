using System;
using System.Runtime.InteropServices;

namespace SagesOfOzvaram
{
    /// <summary>
    /// Plain-text event log written to whichever terminal launched the game (`dotnet run`, or
    /// the built .exe run directly from a shell) - every attack, miss, damage number, card cast,
    /// and movement (with tile coordinates), separated into one block per unit's own turn (see
    /// LogUnitTurnStart) - the exact same separator format Game1.LogUnitTurnDivider prints to the
    /// in-game dev console, just mirrored here too.
    /// The game is a WinExe (no console flashes for a player who just double-clicks it), so
    /// Initialize() uses AttachConsole to hook into the parent process's console when one
    /// actually launched it, rather than switching the whole project to the Exe subsystem (which
    /// would force a console window open for every player, shipped build included). It must run
    /// before anything else touches Console - .NET caches Console.Out on first access, and if
    /// that happens before the attach, the attach has no visible effect. With no parent console
    /// (e.g. double-clicked from Explorer), AttachConsole just fails silently and every Log call
    /// below becomes a no-op - exactly the behavior a shipped game should have.
    /// </summary>
    public static class GameLogger
    {
        [DllImport("kernel32.dll")]
        private static extern bool AttachConsole(int dwProcessId);

        private const int ATTACH_PARENT_PROCESS = -1;

        private static bool _everLogged;

        /// <summary>Call once, as the very first thing the program does (see Program.cs).</summary>
        public static void Initialize()
        {
            AttachConsole(ATTACH_PARENT_PROCESS);
        }

        /// <summary>
        /// Marks a single unit's turn starting - called from Game1.LogUnitTurnDivider, which is
        /// itself subscribed to TurnSystem.UnitTurnStarted, so this fires exactly once per unit's
        /// actual turn (never for a Fainted unit's forced skip). Prints a "====="-style separator
        /// (skipped before the very first unit's turn of a match, so nothing precedes it) then a
        /// "{UnitName} - Turn {N}" header.
        /// </summary>
        public static void LogUnitTurnStart(string unitName, int turnNumber)
        {
            if (_everLogged)
                Console.WriteLine(new string('=', 40));
            Console.WriteLine($"{unitName} - Turn {turnNumber}");
            _everLogged = true;
        }

        /// <summary>Resets turn/unit tracking for a fresh match (EnterTestMode/EnterOriginalMode) so the next LogUnitTurnStart prints "Turn 1" with no leading divider, instead of treating it as a continuation of whatever match just ended.</summary>
        public static void ResetForNewMatch()
        {
            _everLogged = false;
        }

        /// <summary>A single event line - attacks, casts, equips, failures, and everything else ShowCombatMessage already surfaces in-game, plus movement (logged separately - see MoveUnitTowards - since it never went through ShowCombatMessage).</summary>
        public static void Log(string message)
        {
            Console.WriteLine(message);
        }
    }
}
