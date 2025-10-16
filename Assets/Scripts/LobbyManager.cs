using UnityEngine;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Authentication;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Collections;

public class LobbyManager : MonoBehaviour
{
    private static LobbyManager instance;
    public static LobbyManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<LobbyManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("LobbyManager");
                    instance = go.AddComponent<LobbyManager>();
                }
            }
            return instance;
        }
    }

    private Lobby currentLobby;
    private float heartbeatTimer;
    private float lobbyUpdateTimer;
    private const float LOBBY_UPDATE_INTERVAL = 1.1f;
    private const float HEARTBEAT_INTERVAL = 15f;

    // Events for UI feedback
    public System.Action<string> OnStatusUpdate;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        HandleLobbyHeartbeat();
        HandleLobbyPollForUpdates();
    }

    private void HandleLobbyHeartbeat()
    {
        if (currentLobby != null && IsLobbyHost())
        {
            heartbeatTimer -= Time.deltaTime;
            if (heartbeatTimer < 0f)
            {
                heartbeatTimer = HEARTBEAT_INTERVAL;
                _ = LobbyService.Instance.SendHeartbeatPingAsync(currentLobby.Id);
            }
        }
    }

    private void HandleLobbyPollForUpdates()
    {
        if (currentLobby != null)
        {
            lobbyUpdateTimer -= Time.deltaTime;
            if (lobbyUpdateTimer < 0f)
            {
                lobbyUpdateTimer = LOBBY_UPDATE_INTERVAL;
                UpdateLobby();
            }
        }
    }

    private async void UpdateLobby()
    {
        try
        {
            currentLobby = await LobbyService.Instance.GetLobbyAsync(currentLobby.Id);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Failed to update lobby: {e.Message} (Reason: {e.Reason})");
            
            // If lobby doesn't exist anymore, clean up
            if (e.Reason == LobbyExceptionReason.LobbyNotFound)
            {
                Debug.Log("Lobby no longer exists, cleaning up...");
                currentLobby = null;
            }
        }
    }

    public async Task<string> CreateLobby(string lobbyName)
    {
        try
        {
            Debug.Log($"Creating lobby: {lobbyName}");
            
            // Ensure we're not already in a lobby
            if (currentLobby != null)
            {
                Debug.Log("Already in a lobby, leaving first...");
                await LeaveLobby();
                await System.Threading.Tasks.Task.Delay(1000);
            }
            
            // First create the relay
            Debug.Log("Creating relay server...");
            string relayCode = await NetworkManager.Instance.CreateRelay();
            
            if (string.IsNullOrEmpty(relayCode))
            {
                Debug.LogError("Failed to create relay server");
                return null;
            }
            
            Debug.Log($"Relay server created with code: {relayCode}");

            // Then create the lobby with the relay code
            CreateLobbyOptions options = new CreateLobbyOptions
            {
                IsPrivate = false, // Make lobby discoverable
                Data = new Dictionary<string, DataObject>
                {
                    { "RelayCode", new DataObject(DataObject.VisibilityOptions.Member, relayCode) }
                }
            };

            Debug.Log("Creating Unity Lobby...");
            currentLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, 4, options); // Match relay capacity
            
            heartbeatTimer = HEARTBEAT_INTERVAL;
            lobbyUpdateTimer = LOBBY_UPDATE_INTERVAL;

            Debug.Log($"Lobby created successfully! Lobby ID: {currentLobby.Id}, Lobby Code: {currentLobby.LobbyCode}");
            Debug.Log($"Lobby max players: {currentLobby.MaxPlayers}, Current players: {currentLobby.Players.Count}");
            
            return currentLobby.LobbyCode; // Return the lobby code, not relay code
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Failed to create lobby: {e.Message} (Reason: {e.Reason})");
            return null;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Unexpected error creating lobby: {e.Message}");
            return null;
        }
    }

    public async Task<bool> JoinLobby(string lobbyCode)
    {
        try
        {
            Debug.Log($"Attempting to join lobby with code: {lobbyCode}");
            
            // Validate input
            if (string.IsNullOrEmpty(lobbyCode))
            {
                Debug.LogError("Lobby code is null or empty");
                return false;
            }

            // Check if we're already in a lobby
            if (currentLobby != null)
            {
                Debug.Log("Already in a lobby, leaving first...");
                await LeaveLobby();
                
                // Wait for cleanup to complete
                await System.Threading.Tasks.Task.Delay(1000);
            }

            // Ensure network manager is properly reset
            if (NetworkManager.Instance.IsConnected())
            {
                Debug.Log("Network still connected, shutting down first...");
                NetworkManager.Instance.Shutdown();
                await System.Threading.Tasks.Task.Delay(1000);
            }

            // Join the lobby directly without pre-validation
            Debug.Log("Joining Unity Lobby...");
            currentLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode);
            
            if (currentLobby == null)
            {
                Debug.LogError("Join lobby returned null");
                return false;
            }

            Debug.Log($"Successfully joined lobby: {currentLobby.Id} with {currentLobby.Players.Count} players");
            Debug.Log($"Lobby details - Name: {currentLobby.Name}, Max players: {currentLobby.MaxPlayers}, Available slots: {currentLobby.AvailableSlots}");

            // Get the relay code from lobby data
            if (currentLobby.Data == null || !currentLobby.Data.ContainsKey("RelayCode"))
            {
                Debug.LogError("Lobby data is missing RelayCode");
                await LeaveLobby(); // Clean up the lobby join
                return false;
            }

            string relayCode = currentLobby.Data["RelayCode"].Value;
            Debug.Log($"Got relay code from lobby: {relayCode}");

            // Join the relay
            Debug.Log("Joining relay server...");
            bool relayJoined = await NetworkManager.Instance.JoinRelay(relayCode);
            
            if (!relayJoined)
            {
                Debug.LogError("Failed to join relay server");
                // Clean up lobby if relay join failed
                await LeaveLobby();
                return false;
            }

            Debug.Log("Successfully joined both lobby and relay!");
            
            lobbyUpdateTimer = LOBBY_UPDATE_INTERVAL;
            return true;
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Failed to join lobby: {e.Message} (Reason: {e.Reason})");
            
            // Provide more specific error messages
            switch (e.Reason)
            {
                case LobbyExceptionReason.LobbyNotFound:
                    Debug.LogError("Lobby not found. The lobby code may be incorrect or the lobby has expired.");
                    break;
                case LobbyExceptionReason.LobbyFull:
                    Debug.LogError("Lobby is full. Cannot join at this time.");
                    break;
                case LobbyExceptionReason.RateLimited:
                    Debug.LogError("Too many requests. Please wait before trying again.");
                    break;
                case LobbyExceptionReason.ValidationError:
                    Debug.LogError("Invalid lobby code format.");
                    break;
                case LobbyExceptionReason.InvalidJoinCode:
                    Debug.LogError("Invalid join code. Please check the code and try again.");
                    break;
                default:
                    Debug.LogError($"Lobby service error: {e.Reason}");
                    break;
            }
            
            return false;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Unexpected error joining lobby: {e.Message}");
            Debug.LogError($"Stack trace: {e.StackTrace}");
            return false;
        }
    }

    public async Task LeaveLobby()
    {
        if (currentLobby != null)
        {
            try
            {
                Debug.Log($"Leaving lobby: {currentLobby.Id}");
                await LobbyService.Instance.RemovePlayerAsync(currentLobby.Id, AuthenticationService.Instance.PlayerId);
                Debug.Log("Successfully left lobby");
            }
            catch (LobbyServiceException e)
            {
                Debug.LogError($"Error leaving lobby: {e.Message} (Reason: {e.Reason})");
            }
            finally
            {
                currentLobby = null;
            }
        }

        // Ensure network is properly shut down
        if (NetworkManager.Instance.IsConnected())
        {
            Debug.Log("Shutting down network connection...");
            NetworkManager.Instance.Shutdown();
        }
    }

    public bool IsLobbyHost()
    {
        return currentLobby != null && currentLobby.HostId == AuthenticationService.Instance.PlayerId;
    }

    /// <summary>
    /// Helper method to create a short, readable player name from the full player ID
    /// </summary>
    /// <param name="playerId">The full player ID</param>
    /// <returns>A shortened player name like "Player-1234"</returns>
    private string GetShortPlayerName(string playerId)
    {
        if (string.IsNullOrEmpty(playerId))
            return "Player-Unknown";

        // Take the last 4 characters of the player ID
        string shortId = playerId.Length >= 4 ? playerId.Substring(playerId.Length - 4) : playerId;
        return $"Player-{shortId}";
    }

    public List<Dictionary<string, string>> GetPlayersInfo()
    {
        List<Dictionary<string, string>> players = new List<Dictionary<string, string>>();

        if (currentLobby != null && currentLobby.Players != null)
        {
            Debug.Log($"Getting player info for {currentLobby.Players.Count} players");
            
            foreach (Player player in currentLobby.Players)
            {
                Dictionary<string, string> playerInfo = new Dictionary<string, string>
                {
                    { "Id", player.Id },
                    { "DisplayName", GetShortPlayerName(player.Id) }, // Add shortened display name
                    { "IsHost", (currentLobby.HostId == player.Id).ToString() }
                };
                players.Add(playerInfo);
                
                Debug.Log($"Player: {playerInfo["DisplayName"]}, Host: {playerInfo["IsHost"]}");
            }
        }

        return players;
    }

    public bool IsInLobby()
    {
        return currentLobby != null;
    }

    public string GetCurrentLobbyId()
    {
        return currentLobby?.Id;
    }

    public string GetCurrentLobbyCode()
    {
        return currentLobby?.LobbyCode;
    }

    public int GetCurrentPlayerCount()
    {
        return currentLobby?.Players?.Count ?? 0;
    }

    public int GetMaxPlayerCount()
    {
        return currentLobby?.MaxPlayers ?? 0;
    }

    public int GetAvailableSlots()
    {
        return currentLobby?.AvailableSlots ?? 0;
    }

    private void OnDestroy()
    {
        if (currentLobby != null)
        {
            try
            {
                if (IsLobbyHost())
                {
                    _ = LobbyService.Instance.DeleteLobbyAsync(currentLobby.Id);
                }
                else
                {
                    _ = LobbyService.Instance.RemovePlayerAsync(currentLobby.Id, AuthenticationService.Instance.PlayerId);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error in LobbyManager.OnDestroy: {e.Message}");
            }
        }
    }
}