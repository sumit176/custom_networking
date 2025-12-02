using UnityEngine;
using UnityEngine.UI;
using TMPro;
using CustomNetworking.Client;

namespace CustomNetworking.UI
{
    public class DebugPanel : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameClient gameClient;
        [SerializeField] private GameObject debugPanel;
        
        [Header("Packet Simulation")]
        [SerializeField] private Toggle packetSimToggle;
        [SerializeField] private Slider packetLossSlider;
        [SerializeField] private Slider latencySlider;
        [SerializeField] private Slider jitterSlider;
        [SerializeField] private TextMeshProUGUI packetLossText;
        [SerializeField] private TextMeshProUGUI latencyText;
        [SerializeField] private TextMeshProUGUI jitterText;
        
        [Header("Statistics")]
        [SerializeField] private TextMeshProUGUI statsText;
        
        private bool isVisible = false;
        
        void Start()
        {
            if (packetSimToggle != null)
            {
                packetSimToggle.onValueChanged.AddListener(OnPacketSimToggled);
            }
            
            if (packetLossSlider != null)
            {
                packetLossSlider.onValueChanged.AddListener(OnPacketLossChanged);
                packetLossSlider.minValue = 0;
                packetLossSlider.maxValue = 50;
                packetLossSlider.value = 0;
            }
            
            if (latencySlider != null)
            {
                latencySlider.onValueChanged.AddListener(OnLatencyChanged);
                latencySlider.minValue = 0;
                latencySlider.maxValue = 500;
                latencySlider.value = 0;
            }
            
            if (jitterSlider != null)
            {
                jitterSlider.onValueChanged.AddListener(OnJitterChanged);
                jitterSlider.minValue = 0;
                jitterSlider.maxValue = 100;
                jitterSlider.value = 0;
            }
            
            if (debugPanel != null)
            {
                debugPanel.SetActive(false);
            }
        }
        
        void Update()
        {
            // Toggle debug panel with F3
            if (Input.GetKeyDown(KeyCode.F3))
            {
                isVisible = !isVisible;
                if (debugPanel != null)
                {
                    debugPanel.SetActive(isVisible);
                }
            }
            
            // Update statistics
            if (isVisible && statsText != null && gameClient != null)
            {
                UpdateStatistics();
            }
            
            // Update slider labels
            UpdateSliderLabels();
        }
        
        void OnPacketSimToggled(bool enabled)
        {
            if (gameClient != null)
            {
                float loss = packetLossSlider != null ? packetLossSlider.value : 0;
                float latency = latencySlider != null ? latencySlider.value : 0;
                float jitter = jitterSlider != null ? jitterSlider.value : 0;
                
                gameClient.SetPacketSimulation(enabled, loss, latency, jitter);
            }
        }
        
        void OnPacketLossChanged(float value)
        {
            UpdatePacketSimulation();
        }
        
        void OnLatencyChanged(float value)
        {
            UpdatePacketSimulation();
        }
        
        void OnJitterChanged(float value)
        {
            UpdatePacketSimulation();
        }
        
        void UpdatePacketSimulation()
        {
            if (gameClient != null && packetSimToggle != null && packetSimToggle.isOn)
            {
                float loss = packetLossSlider != null ? packetLossSlider.value : 0;
                float latency = latencySlider != null ? latencySlider.value : 0;
                float jitter = jitterSlider != null ? jitterSlider.value : 0;
                
                gameClient.SetPacketSimulation(true, loss, latency, jitter);
            }
        }
        
        void UpdateSliderLabels()
        {
            if (packetLossText != null && packetLossSlider != null)
            {
                packetLossText.text = $"Packet Loss: {packetLossSlider.value:F0}%";
            }
            
            if (latencyText != null && latencySlider != null)
            {
                latencyText.text = $"Latency: {latencySlider.value:F0}ms";
            }
            
            if (jitterText != null && jitterSlider != null)
            {
                jitterText.text = $"Jitter: {jitterSlider.value:F0}ms";
            }
        }
        
        void UpdateStatistics()
        {
            string stats = "=== Network Statistics ===\n\n";
            
            stats += $"Client State: {gameClient.State}\n";
            stats += $"Client ID: {gameClient.ClientId}\n";
            stats += $"Ping: {gameClient.Ping:F1}ms\n";
            stats += $"FPS: {1.0f / Time.deltaTime:F0}\n";
            
            // Get entity count through reflection
            var gameStateField = typeof(GameClient).GetField("gameState", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (gameStateField != null)
            {
                var gameState = gameStateField.GetValue(gameClient);
                var entitiesField = gameState.GetType().GetField("entities", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (entitiesField != null)
                {
                    var entities = entitiesField.GetValue(gameState) as System.Collections.IDictionary;
                    if (entities != null)
                    {
                        stats += $"Entity Count: {entities.Count}\n";
                    }
                }
            }
            
            stats += "\n=== Packet Simulation ===\n\n";
            stats += $"Enabled: {(packetSimToggle != null && packetSimToggle.isOn ? "Yes" : "No")}\n";
            if (packetSimToggle != null && packetSimToggle.isOn)
            {
                stats += $"Loss: {(packetLossSlider != null ? packetLossSlider.value : 0):F0}%\n";
                stats += $"Latency: {(latencySlider != null ? latencySlider.value : 0):F0}ms\n";
                stats += $"Jitter: {(jitterSlider != null ? jitterSlider.value : 0):F0}ms\n";
            }
            
            stats += "\n=== Controls ===\n\n";
            stats += "WASD: Move\n";
            stats += "Mouse: Aim\n";
            stats += "Click/Space: Shoot\n";
            stats += "F3: Toggle Debug Panel\n";
            
            statsText.text = stats;
        }
    }
}

