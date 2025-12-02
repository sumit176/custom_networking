using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CustomNetworking.Client;

namespace CustomNetworking.UI
{
    public class ConnectionScreen : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameClient gameClient;
        [SerializeField] private GameObject connectionPanel;
        [SerializeField] private GameObject connectingPanel;
        
        [Header("Input Fields")]
        [SerializeField] private TMP_InputField ipAddressInput;
        [SerializeField] private TMP_InputField portInput;
        [SerializeField] private TMP_InputField playerNameInput;
        
        [Header("UI Elements")]
        [SerializeField] private Button connectButton;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private TextMeshProUGUI connectingText;
        
        private bool isConnecting = false;
        
        void Start()
        {
            // Set default values
            if (ipAddressInput != null)
                ipAddressInput.text = "127.0.0.1";
            
            if (portInput != null)
                portInput.text = "7777";
            
            if (playerNameInput != null)
                playerNameInput.text = "Player" + Random.Range(100, 999);
            
            // Setup button
            if (connectButton != null)
            {
                connectButton.onClick.AddListener(OnConnectButtonClicked);
            }
            
            // Subscribe to client events
            if (gameClient != null)
            {
                gameClient.OnConnected += OnConnected;
                gameClient.OnDisconnected += OnDisconnected;
                gameClient.OnConnectionFailed += OnConnectionFailed;
            }
            
            ShowConnectionPanel();
        }
        
        void OnDestroy()
        {
            if (gameClient != null)
            {
                gameClient.OnConnected -= OnConnected;
                gameClient.OnDisconnected -= OnDisconnected;
                gameClient.OnConnectionFailed -= OnConnectionFailed;
            }
        }
        
        void OnConnectButtonClicked()
        {
            string ipAddress = ipAddressInput != null ? ipAddressInput.text : "127.0.0.1";
            string portStr = portInput != null ? portInput.text : "7777";
            string playerName = playerNameInput != null ? playerNameInput.text : "Player";
            
            if (string.IsNullOrWhiteSpace(playerName))
            {
                ShowStatus("Please enter a player name", Color.red);
                return;
            }
            
            if (!int.TryParse(portStr, out int port))
            {
                ShowStatus("Invalid port number", Color.red);
                return;
            }
            
            if (port < 1 || port > 65535)
            {
                ShowStatus("Port must be between 1 and 65535", Color.red);
                return;
            }
            
            ShowStatus("Connecting...", Color.yellow);
            ShowConnectingPanel();
            
            gameClient.Connect(ipAddress, port, playerName);
            isConnecting = true;
        }
        
        void OnConnected()
        {
            isConnecting = false;
            HideAll();
        }
        
        void OnDisconnected(string reason)
        {
            isConnecting = false;
            ShowConnectionPanel();
            ShowStatus($"Disconnected: {reason}", Color.red);
        }
        
        void OnConnectionFailed(string reason)
        {
            isConnecting = false;
            ShowConnectionPanel();
            ShowStatus($"Connection failed: {reason}", Color.red);
        }
        
        void ShowConnectionPanel()
        {
            if (connectionPanel != null)
                connectionPanel.SetActive(true);
            if (connectingPanel != null)
                connectingPanel.SetActive(false);
        }
        
        void ShowConnectingPanel()
        {
            if (connectionPanel != null)
                connectionPanel.SetActive(false);
            if (connectingPanel != null)
                connectingPanel.SetActive(true);
        }
        
        void HideAll()
        {
            if (connectionPanel != null)
                connectionPanel.SetActive(false);
            if (connectingPanel != null)
                connectingPanel.SetActive(false);
        }
        
        void ShowStatus(string message, Color color)
        {
            if (statusText != null)
            {
                statusText.text = message;
                statusText.color = color;
            }
        }
        
        void Update()
        {
            // Animate connecting text
            if (isConnecting && connectingText != null)
            {
                int dots = ((int)(Time.time * 2)) % 4;
                connectingText.text = "Connecting" + new string('.', dots);
            }
        }
    }
}

