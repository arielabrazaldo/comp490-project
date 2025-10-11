using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

public class NetworkManager : MonoBehaviour
{
    private static NetworkManager instance;
    public static NetworkManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<NetworkManager>();
                if (instance == null)
                {
                    GameObject go = new GameObject("NetworkManager");
                    instance = go.AddComponent<NetworkManager>();
                }
            }
            return instance;
        }
    }

    [SerializeField]
    private NetworkObject playerPrefab;

    private Unity.Netcode.NetworkManager netcodeManager;
    public string JoinCode { get; private set; }
    private string playerID;

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

        SetupNetcodeManager();
    }

    private void SetupNetcodeManager()
    {
        // First, try to get the existing Unity NetworkManager component
        netcodeManager = GetComponent<Unity.Netcode.NetworkManager>();
        
        if (netcodeManager == null)
        {
            Debug.LogError("Unity's NetworkManager component not found! Please add Unity's NetworkManager component to this GameObject.");
            return;
        }

        // Ensure it has a transport
        var transport = GetComponent<UnityTransport>();
        if (transport == null)
        {
            Debug.Log("Adding UnityTransport component");
            transport = gameObject.AddComponent<UnityTransport>();
        }

        // Ensure NetworkConfig exists
        if (netcodeManager.NetworkConfig == null)
        {
            netcodeManager.NetworkConfig = new NetworkConfig();
        }
        
        // Assign transport if not already assigned
        if (netcodeManager.NetworkConfig.NetworkTransport == null)
        {
            netcodeManager.NetworkConfig.NetworkTransport = transport;
        }

        Debug.Log("Using existing Unity NetworkManager component");

        // Subscribe to network events for better debugging
        netcodeManager.OnClientConnectedCallback += OnClientConnected;
        netcodeManager.OnClientDisconnectCallback += OnClientDisconnected;
        netcodeManager.OnServerStarted += OnServerStarted;
    }

    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"Client connected: {clientId}");
    }

    private void OnClientDisconnected(ulong clientId)
    {
        Debug.Log($"Client disconnected: {clientId}");
    }

    private void OnServerStarted()
    {
        Debug.Log("Server started successfully");
    }

    private async void Start()
    {
        await InitializeUnityServices();
    }

    private async Task InitializeUnityServices()
    {
        try
        {
            Debug.Log("Initializing Unity Services...");
            await UnityServices.InitializeAsync();

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                Debug.Log("Signing in anonymously...");
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }

            playerID = AuthenticationService.Instance.PlayerId;
            Debug.Log($"Player authenticated with ID: {playerID}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to initialize Unity Services: {e.Message}");
        }
    }

    public async Task<string> CreateRelay()
    {
        try
        {
            // Ensure we're not already hosting
            if (netcodeManager.IsListening)
            {
                Debug.Log("Already listening, shutting down first...");
                Shutdown();
                await System.Threading.Tasks.Task.Delay(500);
            }

            Debug.Log("Creating Relay allocation...");
            // Fixed: Match relay capacity with lobby capacity (4 players)
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(4);
            JoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            Debug.Log($"Relay allocation created. Join code: {JoinCode}");
            Debug.Log($"Relay server: {allocation.RelayServer.IpV4}:{allocation.RelayServer.Port}");

            var transport = netcodeManager.GetComponent<UnityTransport>();
            if (transport == null)
            {
                Debug.LogError("UnityTransport component not found!");
                return null;
            }
            
            // Use Unity Transport's SetHostRelayData method with proper type casting
            transport.SetHostRelayData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData
            );

            Debug.Log("Starting host...");
            bool started = netcodeManager.StartHost();
            if (started)
            {
                Debug.Log($"Host started successfully with join code: {JoinCode}");
                Debug.Log($"Network Manager status - IsHost: {netcodeManager.IsHost}, IsServer: {netcodeManager.IsServer}, IsListening: {netcodeManager.IsListening}");
                return JoinCode;
            }
            else
            {
                Debug.LogError("Failed to start host");
                return null;
            }
        }
        catch (RelayServiceException e)
        {
            Debug.LogError($"Relay create error: {e.Message} (Reason: {e.Reason})");
            return null;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Unexpected error creating relay: {e.Message}");
            Debug.LogError($"Stack trace: {e.StackTrace}");
            return null;
        }
    }

    public async Task<bool> JoinRelay(string joinCode)
    {
        try
        {
            Debug.Log($"Joining relay with code: {joinCode}");
            
            if (string.IsNullOrEmpty(joinCode))
            {
                Debug.LogError("Join code is null or empty");
                return false;
            }

            // Ensure we're not already connected
            if (netcodeManager.IsListening)
            {
                Debug.Log("Already listening, shutting down first...");
                Shutdown();
                await System.Threading.Tasks.Task.Delay(1000); // Give more time for shutdown
            }

            JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
            
            if (allocation == null)
            {
                Debug.LogError("Join allocation returned null");
                return false;
            }

            Debug.Log($"Relay join allocation received. Server: {allocation.RelayServer.IpV4}:{allocation.RelayServer.Port}");
            
            var transport = netcodeManager.GetComponent<UnityTransport>();
            if (transport == null)
            {
                Debug.LogError("UnityTransport component not found!");
                return false;
            }
            
            // Use Unity Transport's SetClientRelayData method with proper type casting
            transport.SetClientRelayData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData,
                allocation.HostConnectionData
            );

            Debug.Log("Starting client...");
            bool started = netcodeManager.StartClient();
            if (started)
            {
                Debug.Log($"Client started successfully, joining relay with code: {joinCode}");
                Debug.Log($"Network Manager status - IsClient: {netcodeManager.IsClient}, IsConnectedClient: {netcodeManager.IsConnectedClient}, IsListening: {netcodeManager.IsListening}");
                
                // Wait a moment for connection to establish
                float timeout = 10f;
                float elapsed = 0f;
                while (!netcodeManager.IsConnectedClient && elapsed < timeout)
                {
                    await System.Threading.Tasks.Task.Delay(100);
                    elapsed += 0.1f;
                }
                
                if (netcodeManager.IsConnectedClient)
                {
                    Debug.Log("Successfully connected to host!");
                    return true;
                }
                else
                {
                    Debug.LogWarning($"Client started but not connected after {timeout} seconds");
                    return true; // Still return true as the client started, connection might take longer
                }
            }
            else
            {
                Debug.LogError("Failed to start client");
                return false;
            }
        }
        catch (RelayServiceException e)
        {
            Debug.LogError($"Relay join error: {e.Message} (Reason: {e.Reason})");
            
            switch (e.Reason)
            {
                case RelayExceptionReason.JoinCodeNotFound:
                    Debug.LogError("Join code not found. The lobby may have expired or the code is incorrect.");
                    break;
                case RelayExceptionReason.AllocationNotFound:
                    Debug.LogError("Relay allocation not found.");
                    break;
                default:
                    Debug.LogError($"Relay service error: {e.Reason}");
                    break;
            }
            
            return false;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Unexpected error joining relay: {e.Message}");
            Debug.LogError($"Stack trace: {e.StackTrace}");
            return false;
        }
    }

    public bool IsConnected()
    {
        return netcodeManager != null && (netcodeManager.IsHost || netcodeManager.IsConnectedClient);
    }

    public bool IsHost()
    {
        return netcodeManager != null && netcodeManager.IsHost;
    }

    public bool IsClient()
    {
        return netcodeManager != null && netcodeManager.IsClient;
    }

    public void Shutdown()
    {
        if (netcodeManager != null && netcodeManager.IsListening)
        {
            Debug.Log("Shutting down network manager");
            netcodeManager.Shutdown();
            
            // Clear join code when shutting down
            JoinCode = null;
        }
    }

    /// <summary>
    /// Force a complete cleanup and reset of the network manager
    /// This is useful when switching between host/client modes
    /// </summary>
    public async Task ForceReset()
    {
        Debug.Log("Forcing network manager reset...");
        
        if (netcodeManager != null && netcodeManager.IsListening)
        {
            netcodeManager.Shutdown();
        }
        
        // Wait for shutdown to complete
        await System.Threading.Tasks.Task.Delay(1000);
        
        // Clear any cached data
        JoinCode = null;
        
        Debug.Log("Network manager reset complete");
    }

    /// <summary>
    /// Get the Unity NetworkManager component for direct access to NetworkConfig
    /// This is needed for accessing Network Prefabs and other Unity Netcode features
    /// </summary>
    public Unity.Netcode.NetworkManager GetUnityNetworkManager()
    {
        return netcodeManager;
    }

    private void OnDestroy()
    {
        if (netcodeManager != null)
        {
            // Unsubscribe from events
            netcodeManager.OnClientConnectedCallback -= OnClientConnected;
            netcodeManager.OnClientDisconnectCallback -= OnClientDisconnected;
            netcodeManager.OnServerStarted -= OnServerStarted;
            
            if (netcodeManager.IsListening)
            {
                netcodeManager.Shutdown();
            }
        }
    }
}