using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;

/// <summary>
/// Real-time Play Mode load test: 4 users play DiceRace for 5 real minutes.
/// Each turn waits RollInterval seconds so actual wall-clock time passes.
/// Tracks both total turn time and roll response time (logic-only, excluding wait).
/// Reports average time per turn and average response time at the end.
///
/// Run via: Test Runner window > PlayMode tab > DiceRaceRealTimeLoadTest
/// </summary>
public class DiceRaceRealTimeLoadTest
{
    // -------------------------------------------------------------------------
    // Configuration
    // -------------------------------------------------------------------------
    private const int   UserCount           = 4;
    private const float TestDuration        = 300f; // 5 real minutes
    private const float RollInterval        = 2f;   // Real seconds between turns
    private const int   BoardSize           = 20;
    private const float MaxAvgResponseMs    = 50f;  // Response time must average under 50 ms

    // -------------------------------------------------------------------------
    // State (set up fresh per test)
    // -------------------------------------------------------------------------
    private struct PlayerState
    {
        public int   Id;
        public int   Position;
        public int   Rolls;
        public int   Laps;
        public float TotalTurnTime;     // Cumulative real seconds spent on this player's turns
        public float TotalResponseTime; // Cumulative roll-logic time (seconds) for this player
    }

    // -------------------------------------------------------------------------
    // Test
    // -------------------------------------------------------------------------

    [UnityTest]
    [Timeout(360_000)] // 6-minute hard timeout (5 min test + 1 min buffer)
    public IEnumerator FourPlayers_PlayDiceRace_ForFiveRealMinutes_ReportsAverageTurnTime()
    {
        var players          = new PlayerState[UserCount];
        var errors           = new List<string>();
        var turnTimings      = new List<float>(); // Real duration of every full turn
        var responseTimings  = new List<float>(); // Roll-logic duration of every turn (seconds)

        for (int i = 0; i < UserCount; i++)
        {
            players[i] = new PlayerState { Id = i };
        }

        int   currentTurn   = 0;
        int   totalRolls    = 0;
        int   totalCycles   = 0;
        float testStartTime = Time.realtimeSinceStartup;
        float elapsed       = 0f;

        var responseTimer = new Stopwatch();

        UnityEngine.Debug.Log($"[DiceRaceRealTimeLoadTest] ? Starting — {UserCount} players, {TestDuration}s real time");

        while (elapsed < TestDuration)
        {
            float turnStart = Time.realtimeSinceStartup;

            // --- Roll (response time measured here) ---
            responseTimer.Restart();

            int roll = Random.Range(1, 7);

            if (roll < 1 || roll > 6)
                errors.Add($"[t={elapsed:F1}s] User {players[currentTurn].Id}: invalid roll {roll}");

            int oldPos                      = players[currentTurn].Position;
            players[currentTurn].Position   = (players[currentTurn].Position + roll) % BoardSize;
            players[currentTurn].Rolls++;
            totalRolls++;

            if (players[currentTurn].Position < oldPos || (oldPos + roll) >= BoardSize)
                players[currentTurn].Laps++;

            if (players[currentTurn].Position < 0 || players[currentTurn].Position >= BoardSize)
                errors.Add($"[t={elapsed:F1}s] User {players[currentTurn].Id}: position {players[currentTurn].Position} out of bounds");

            responseTimer.Stop();

            float responseSeconds = (float)responseTimer.Elapsed.TotalSeconds;
            responseTimings.Add(responseSeconds);
            players[currentTurn].TotalResponseTime += responseSeconds;

            UnityEngine.Debug.Log($"[DiceRaceRealTimeLoadTest] t={elapsed:F0}s | User {players[currentTurn].Id} rolled {roll} ? tile {players[currentTurn].Position} | response {responseSeconds * 1000f:F3}ms");

            // --- Wait real time ---
            yield return new WaitForSeconds(RollInterval);

            // --- Record total turn timing ---
            float turnDuration = Time.realtimeSinceStartup - turnStart;
            turnTimings.Add(turnDuration);
            players[currentTurn].TotalTurnTime += turnDuration;

            // --- Advance turn ---
            currentTurn = (currentTurn + 1) % UserCount;
            if (currentTurn == 0) totalCycles++;

            elapsed = Time.realtimeSinceStartup - testStartTime;
        }

        float actualDuration = Time.realtimeSinceStartup - testStartTime;

        // --- Report ---
        PrintReport(players, actualDuration, totalRolls, totalCycles, turnTimings, responseTimings, errors);

        // --- Assertions ---
        Assert.AreEqual(0, errors.Count,
            $"{errors.Count} error(s):\n{string.Join("\n", errors)}");

        foreach (var p in players)
        {
            Assert.Greater(p.Rolls, 0, $"User {p.Id} never rolled.");
            Assert.IsTrue(p.Position >= 0 && p.Position < BoardSize,
                $"User {p.Id} has invalid final position {p.Position}.");
        }

        // Average turn time must be within ±20% of RollInterval
        float avgTurn = CalculateAverage(turnTimings);
        Assert.IsTrue(avgTurn >= RollInterval * 0.8f && avgTurn <= RollInterval * 1.2f,
            $"Average turn time {avgTurn:F3}s is outside expected range " +
            $"[{RollInterval * 0.8f:F2}s – {RollInterval * 1.2f:F2}s].");

        // Average response time must be under MaxAvgResponseMs
        float avgResponseMs = CalculateAverage(responseTimings) * 1000f;
        Assert.Less(avgResponseMs, MaxAvgResponseMs,
            $"Average response time {avgResponseMs:F3}ms exceeds limit of {MaxAvgResponseMs}ms.");

        UnityEngine.Debug.Log("[DiceRaceRealTimeLoadTest] ? PASSED");
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    private float CalculateAverage(List<float> values)
    {
        if (values.Count == 0) return 0f;
        float sum = 0f;
        foreach (float v in values) sum += v;
        return sum / values.Count;
    }

    private float CalculateMin(List<float> values)
    {
        float min = float.MaxValue;
        foreach (float v in values) if (v < min) min = v;
        return min;
    }

    private float CalculateMax(List<float> values)
    {
        float max = float.MinValue;
        foreach (float v in values) if (v > max) max = v;
        return max;
    }

    private float CalculatePercentile(List<float> values, float percentile)
    {
        if (values.Count == 0) return 0f;
        var sorted = new List<float>(values);
        sorted.Sort();
        int index = (int)System.Math.Ceiling(percentile / 100.0 * sorted.Count) - 1;
        return sorted[Mathf.Clamp(index, 0, sorted.Count - 1)];
    }

    private void PrintReport(PlayerState[] players, float duration, int totalRolls,
                              int totalCycles, List<float> timings, List<float> responseTimes,
                              List<string> errors)
    {
        float avg    = CalculateAverage(timings);
        float min    = timings.Count > 0 ? CalculateMin(timings) : 0f;
        float max    = timings.Count > 0 ? CalculateMax(timings) : 0f;

        float avgRespMs = CalculateAverage(responseTimes) * 1000f;
        float minRespMs = responseTimes.Count > 0 ? CalculateMin(responseTimes) * 1000f : 0f;
        float maxRespMs = responseTimes.Count > 0 ? CalculateMax(responseTimes) * 1000f : 0f;
        float p95RespMs = responseTimes.Count > 0 ? CalculatePercentile(responseTimes, 95f) * 1000f : 0f;
        float p99RespMs = responseTimes.Count > 0 ? CalculatePercentile(responseTimes, 99f) * 1000f : 0f;

        UnityEngine.Debug.Log("========== DICE RACE REAL-TIME LOAD TEST REPORT ==========");
        UnityEngine.Debug.Log($"Real duration       : {duration:F1}s / {TestDuration}s");
        UnityEngine.Debug.Log($"Players             : {UserCount}");
        UnityEngine.Debug.Log($"Board size          : {BoardSize} tiles");
        UnityEngine.Debug.Log($"Total turns         : {totalRolls}");
        UnityEngine.Debug.Log($"Full turn cycles    : {totalCycles}");
        UnityEngine.Debug.Log($"Errors              : {errors.Count}");
        UnityEngine.Debug.Log("--- Turn timing (real seconds, includes wait) ---");
        UnityEngine.Debug.Log($"  Average per turn  : {avg:F3}s");
        UnityEngine.Debug.Log($"  Fastest turn      : {min:F3}s");
        UnityEngine.Debug.Log($"  Slowest turn      : {max:F3}s");
        UnityEngine.Debug.Log("--- Response time (roll logic only, ms) ---");
        UnityEngine.Debug.Log($"  Average           : {avgRespMs:F3}ms");
        UnityEngine.Debug.Log($"  Fastest           : {minRespMs:F3}ms");
        UnityEngine.Debug.Log($"  Slowest           : {maxRespMs:F3}ms");
        UnityEngine.Debug.Log($"  95th percentile   : {p95RespMs:F3}ms");
        UnityEngine.Debug.Log($"  99th percentile   : {p99RespMs:F3}ms");
        UnityEngine.Debug.Log("--- Per-player stats ---");
        foreach (var p in players)
        {
            float perTurn     = p.Rolls > 0 ? p.TotalTurnTime / p.Rolls : 0f;
            float perResponse = p.Rolls > 0 ? p.TotalResponseTime / p.Rolls * 1000f : 0f;
            UnityEngine.Debug.Log($"  User {p.Id}: {p.Rolls} rolls | {p.Laps} laps | " +
                      $"final tile {p.Position} | avg turn {perTurn:F3}s | avg response {perResponse:F3}ms");
        }
        if (errors.Count > 0)
        {
            UnityEngine.Debug.LogWarning("--- Errors ---");
            foreach (var e in errors) UnityEngine.Debug.LogWarning(e);
        }
        UnityEngine.Debug.Log("==========================================================");
    }
}
