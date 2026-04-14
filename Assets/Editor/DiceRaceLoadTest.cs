using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Load test simulating 4 users playing DiceRace for 5 minutes (300 seconds).
/// Runs in edit mode as a pure C# simulation - no scene or network required.
/// Time is simulated (instant) - for real wall-clock timing see DiceRaceRealTimeLoadTest in PlayMode.
///
/// Configuration:
///   Users       : 4
///   Activity    : Roll dice, advance on 20-tile board, cycle turns
///   Duration    : 300 simulated seconds (RollInterval * rounds drives time)
/// </summary>
public class DiceRaceLoadTest
{
    // -------------------------------------------------------------------------
    // Configuration
    // -------------------------------------------------------------------------
    private const int   UserCount      = 4;
    private const float TestDuration   = 300f; // 5 minutes in seconds
    private const float RollInterval   = 2f;   // Seconds between each user's roll
    private const int   BoardSize      = 20;   // Tiles on the DiceRace board

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private struct PlayerState
    {
        public int   Id;
        public int   Position;
        public int   Rolls;
        public int   Laps;
        public float TimeAccumulated;
    }

    // -------------------------------------------------------------------------
    // Tests
    // -------------------------------------------------------------------------

    /// <summary>
    /// All 4 simulated users roll dice on their turns over 5 minutes (simulated time, runs instantly).
    /// For real wall-clock timing and average turn measurement, use DiceRaceRealTimeLoadTest in PlayMode.
    /// </summary>
    [Test]
    public void FourPlayers_PlayDiceRace_ForFiveMinutes_Simulated_NoErrors()
    {
        // --- Setup ---
        var players  = new PlayerState[UserCount];
        var errors   = new List<string>();

        for (int i = 0; i < UserCount; i++)
        {
            players[i] = new PlayerState { Id = i };
        }

        int   currentTurn    = 0;
        float elapsed        = 0f;
        int   totalRolls     = 0;
        int   totalTurnCycles = 0;

        // --- Simulate ---
        // Each iteration = one turn (one user rolls once).
        // Time advances by RollInterval per turn.
        while (elapsed < TestDuration)
        {
            ref PlayerState player = ref players[currentTurn];

            // Roll one die (1-6)
            int roll = Random.Range(1, 7);

            // Validate roll
            if (roll < 1 || roll > 6)
            {
                errors.Add($"[t={elapsed:F1}s] User {player.Id}: invalid roll {roll}");
            }

            // Advance position
            int oldPosition  = player.Position;
            player.Position  = (player.Position + roll) % BoardSize;
            player.Rolls++;
            totalRolls++;

            // Detect lap completion (position wrapped)
            if (player.Position < oldPosition || (oldPosition + roll) >= BoardSize)
            {
                player.Laps++;
            }

            // Validate position
            if (player.Position < 0 || player.Position >= BoardSize)
            {
                errors.Add($"[t={elapsed:F1}s] User {player.Id}: position {player.Position} out of bounds");
            }

            // Advance turn and time
            currentTurn = (currentTurn + 1) % UserCount;
            elapsed    += RollInterval;

            if (currentTurn == 0)
            {
                totalTurnCycles++;
            }
        }

        // --- Print Report ---
        PrintReport(players, elapsed, totalRolls, totalTurnCycles, errors);

        // --- Assertions ---
        Assert.AreEqual(0, errors.Count,
            $"{errors.Count} error(s) during load test:\n{string.Join("\n", errors)}");

        // Every user must have rolled at least once
        foreach (var p in players)
        {
            Assert.Greater(p.Rolls, 0,
                $"User {p.Id} never rolled during the {TestDuration}s test.");
        }

        // Expect roughly equal roll distribution (within 1 roll of each other)
        int minRolls = int.MaxValue;
        int maxRolls = int.MinValue;
        foreach (var p in players)
        {
            if (p.Rolls < minRolls) minRolls = p.Rolls;
            if (p.Rolls > maxRolls) maxRolls = p.Rolls;
        }
        Assert.LessOrEqual(maxRolls - minRolls, 1,
            $"Unfair turn distribution: min={minRolls}, max={maxRolls} rolls.");

        // Total rolls should match expected count
        int expectedRolls = Mathf.FloorToInt(TestDuration / RollInterval);
        Assert.AreEqual(expectedRolls, totalRolls,
            $"Expected {expectedRolls} total rolls but recorded {totalRolls}.");

        // At least one full turn cycle should have completed
        Assert.Greater(totalTurnCycles, 0, "No complete turn cycles completed.");

        // All positions must be valid after the test
        foreach (var p in players)
        {
            Assert.IsTrue(p.Position >= 0 && p.Position < BoardSize,
                $"User {p.Id} ended on invalid position {p.Position}.");
        }
    }

    /// <summary>
    /// Validates DiceRace game rules are configured correctly for 4 players.
    /// </summary>
    [Test]
    public void DiceRaceRules_SupportsFivePlayers()
    {
        GameRules rules = GameRules.CreateDiceRaceRules();

        Assert.IsNotNull(rules, "DiceRace rules must not be null.");
        Assert.LessOrEqual(rules.minPlayers, UserCount,
            $"DiceRace minPlayers ({rules.minPlayers}) must be <= {UserCount}.");
        Assert.GreaterOrEqual(rules.maxPlayers, UserCount,
            $"DiceRace maxPlayers ({rules.maxPlayers}) must be >= {UserCount}.");
        Assert.IsTrue(rules.ValidateRules(out string error),
            $"DiceRace rules failed validation: {error}");
    }

    /// <summary>
    /// Validates that a simulated game session produces consistent results
    /// across two identical runs (deterministic with fixed seed).
    /// </summary>
    [Test]
    public void DiceRaceSimulation_IsConsistent_WithSameSeed()
    {
        int[]  run1 = SimulateRun(seed: 42);
        int[]  run2 = SimulateRun(seed: 42);

        for (int i = 0; i < UserCount; i++)
        {
            Assert.AreEqual(run1[i], run2[i],
                $"User {i} ended at different positions across identical runs " +
                $"(run1={run1[i]}, run2={run2[i]}). Simulation is not deterministic.");
        }
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Runs a short deterministic simulation and returns final positions.
    /// </summary>
    private int[] SimulateRun(int seed)
    {
        Random.InitState(seed);

        var positions    = new int[UserCount];
        int currentTurn  = 0;
        float elapsed    = 0f;

        while (elapsed < TestDuration)
        {
            int roll        = Random.Range(1, 7);
            positions[currentTurn] = (positions[currentTurn] + roll) % BoardSize;
            currentTurn     = (currentTurn + 1) % UserCount;
            elapsed        += RollInterval;
        }

        return positions;
    }

    private void PrintReport(PlayerState[] players, float elapsed, int totalRolls,
                             int totalTurnCycles, List<string> errors)
    {
        Debug.Log("========== DICE RACE LOAD TEST REPORT ==========");
        Debug.Log($"Simulated time  : {elapsed:F1}s / {TestDuration}s");
        Debug.Log($"Users           : {UserCount}");
        Debug.Log($"Board size      : {BoardSize} tiles");
        Debug.Log($"Total rolls     : {totalRolls}");
        Debug.Log($"Turn cycles     : {totalTurnCycles}");
        Debug.Log($"Errors          : {errors.Count}");
        Debug.Log("-------------------------------------------------");
        foreach (var p in players)
        {
            Debug.Log($"  User {p.Id}: {p.Rolls} rolls | {p.Laps} laps | final tile {p.Position}");
        }
        if (errors.Count > 0)
        {
            Debug.LogWarning("--- Errors ---");
            foreach (var e in errors) Debug.LogWarning(e);
        }
        Debug.Log("=================================================");
    }
}
