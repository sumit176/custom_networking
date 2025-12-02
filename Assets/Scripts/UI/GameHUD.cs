using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CustomNetworking.Client;

namespace CustomNetworking.UI
{
    public class GameHUD : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameClient gameClient;
        [SerializeField] private GameObject hudPanel;
        
        [Header("UI Elements")]
        [SerializeField] private TextMeshProUGUI pingText;
        [SerializeField] private TextMeshProUGUI fpsText;
        [SerializeField] private TextMeshProUGUI connectionStatusText;
        [SerializeField] private Button disconnectButton;
        
        private float fpsUpdateTimer;
        private int frameCount;
        private float fps;
        
        void Start()
        {
            if (disconnectButton != null)
            {
                disconnectButton.onClick.AddListener(OnDisconnectClicked);
            }
            
            if (gameClient != null)
            {
                gameClient.OnConnected += OnConnected;
                gameClient.OnDisconnected += OnDisconnected;
            }
            
            HideHUD();
        }
        
        void OnDestroy()
        {
            if (gameClient != null)
            {
                gameClient.OnConnected -= OnConnected;
                gameClient.OnDisconnected -= OnDisconnected;
            }
        }
        
        void Update()
        {
            if (gameClient == null || !gameClient.IsConnected)
            {
                HideHUD();
                return;
            }
            
            ShowHUD();
            
            // Update ping
            if (pingText != null)
            {
                float ping = gameClient.Ping;
                pingText.text = $"Ping: {ping:F0}ms";
                
                // Color based on ping
                if (ping < 50)
                    pingText.color = Color.green;
                else if (ping < 100)
                    pingText.color = Color.yellow;
                else
                    pingText.color = Color.red;
            }
            
            // Update FPS
            frameCount++;
            fpsUpdateTimer += Time.deltaTime;
            
            if (fpsUpdateTimer >= 0.5f)
            {
                fps = frameCount / fpsUpdateTimer;
                frameCount = 0;
                fpsUpdateTimer = 0;
                
                if (fpsText != null)
                {
                    fpsText.text = $"FPS: {fps:F0}";
                    
                    // Color based on FPS
                    if (fps >= 50)
                        fpsText.color = Color.green;
                    else if (fps >= 30)
                        fpsText.color = Color.yellow;
                    else
                        fpsText.color = Color.red;
                }
            }
            
            // Update connection status
            if (connectionStatusText != null)
            {
                connectionStatusText.text = $"Status: {gameClient.State}";
                connectionStatusText.color = gameClient.IsConnected ? Color.green : Color.red;
            }
        }
        
        void OnConnected()
        {
            ShowHUD();
        }
        
        void OnDisconnected(string reason)
        {
            HideHUD();
        }
        
        void OnDisconnectClicked()
        {
            if (gameClient != null)
            {
                gameClient.Disconnect("User disconnected");
            }
        }
        
        void ShowHUD()
        {
            if (hudPanel != null && !hudPanel.activeSelf)
            {
                hudPanel.SetActive(true);
            }
        }
        
        void HideHUD()
        {
            if (hudPanel != null && hudPanel.activeSelf)
            {
                hudPanel.SetActive(false);
            }
        }
    }
}

