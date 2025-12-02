using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace CustomNetworking.Testing
{
    public class StressTestManager : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private string serverAddress = "127.0.0.1";
        [SerializeField] private int serverPort = 7777;
        [SerializeField] private int maxDummyClients = 50;
        
        [Header("UI")]
        [SerializeField] private GameObject stressTestPanel;
        [SerializeField] private Slider clientCountSlider;
        [SerializeField] private TextMeshProUGUI clientCountText;
        [SerializeField] private Button startButton;
        [SerializeField] private Button stopButton;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private TextMeshProUGUI statisticsText;
        
        private List<DummyClient> dummyClients;
        private bool isRunning;
        private int targetClientCount;
        private float statsUpdateTimer;
        
        void Start()
        {
            dummyClients = new List<DummyClient>();
            isRunning = false;
            
            if (clientCountSlider != null)
            {
                clientCountSlider.minValue = 1;
                clientCountSlider.maxValue = maxDummyClients;
                clientCountSlider.value = 10;
                clientCountSlider.onValueChanged.AddListener(OnClientCountChanged);
            }
            
            if (startButton != null)
            {
                startButton.onClick.AddListener(OnStartClicked);
            }
            
            if (stopButton != null)
            {
                stopButton.onClick.AddListener(OnStopClicked);
                stopButton.interactable = false;
            }
            
            if (stressTestPanel != null)
            {
                stressTestPanel.SetActive(false);
            }
            
            UpdateClientCountText();
        }
        
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.F4))
            {
                if (stressTestPanel != null)
                {
                    stressTestPanel.SetActive(!stressTestPanel.activeSelf);
                }
            }
            
            if (isRunning)
            {
                foreach (var client in dummyClients)
                {
                    // Update clients
                    client.Update(Time.deltaTime);
                }
                
                statsUpdateTimer += Time.deltaTime;
                if (statsUpdateTimer >= 0.5f)
                {
                    statsUpdateTimer = 0;
                    UpdateStatistics();
                }
            }
        }
        
        void OnDestroy()
        {
            StopAllClients();
        }
        
        void OnClientCountChanged(float value)
        {
            targetClientCount = (int)value;
            UpdateClientCountText();
        }
        
        void UpdateClientCountText()
        {
            if (clientCountText != null)
            {
                int current = dummyClients.Count;
                clientCountText.text = $"Dummy Clients: {current} / {targetClientCount}";
            }
        }
        
        void OnStartClicked()
        {
            if (isRunning)
                return;
            
            targetClientCount = clientCountSlider != null ? (int)clientCountSlider.value : 10;
            StartStressTest();
        }
        
        void OnStopClicked()
        {
            StopStressTest();
        }
        
        void StartStressTest()
        {
            if (isRunning)
                return;
            
            isRunning = true;
            
            if (startButton != null)
                startButton.interactable = false;
            if (stopButton != null)
                stopButton.interactable = true;
            if (clientCountSlider != null)
                clientCountSlider.interactable = false;
            
            // Start spawning clients
            StartCoroutine(SpawnClientsGradually());
            
            UpdateStatus($"Starting stress test with {targetClientCount} clients...", Color.yellow);
            Debug.Log($"Starting stress test with {targetClientCount} dummy clients");
        }
        
        void StopStressTest()
        {
            if (!isRunning)
                return;
            
            isRunning = false;
            
            StopAllCoroutines();
            StopAllClients();
            
            if (startButton != null)
                startButton.interactable = true;
            if (stopButton != null)
                stopButton.interactable = false;
            if (clientCountSlider != null)
                clientCountSlider.interactable = true;
            
            UpdateStatus("Stress test stopped", Color.white);
            Debug.Log("Stress test stopped");
        }
        
        System.Collections.IEnumerator SpawnClientsGradually()
        {
            int spawned = 0;
            
            while (spawned < targetClientCount && isRunning)
            {
                string name = $"Bot{spawned + 1}";
                DummyClient client = new DummyClient(name, spawned);
                
                if (client.Connect(serverAddress, serverPort))
                {
                    dummyClients.Add(client);
                    spawned++;
                    
                    UpdateClientCountText();
                    UpdateStatus($"Spawning clients... {spawned}/{targetClientCount}", Color.yellow);
                    yield return new WaitForSeconds(0.05f);
                }
                else
                {
                    Debug.LogWarning($"Failed to connect dummy client {name}");
                    yield return new WaitForSeconds(0.1f);
                }
            }
            
            UpdateStatus($"Stress test running with {dummyClients.Count} clients", Color.green);
        }
        
        void StopAllClients()
        {
            foreach (var client in dummyClients)
            {
                client.Disconnect();
            }
            dummyClients.Clear();
            UpdateClientCountText();
        }
        
        void UpdateStatus(string message, Color color)
        {
            if (statusText != null)
            {
                statusText.text = message;
                statusText.color = color;
            }
        }
        
        void UpdateStatistics()
        {
            if (statisticsText == null)
                return;
            
            int connectedCount = 0;
            float totalRtt = 0;
            float minRtt = float.MaxValue;
            float maxRtt = 0;
            
            foreach (var client in dummyClients)
            {
                if (client.IsConnected)
                {
                    connectedCount++;
                    float rtt = client.Rtt;
                    totalRtt += rtt;
                    
                    if (rtt < minRtt) minRtt = rtt;
                    if (rtt > maxRtt) maxRtt = rtt;
                }
            }
            
            float avgRtt = connectedCount > 0 ? totalRtt / connectedCount : 0;
            
            string stats = "=== Stress Test Statistics ===\n\n";
            stats += $"Target Clients: {targetClientCount}\n";
            stats += $"Total Clients: {dummyClients.Count}\n";
            stats += $"Connected: {connectedCount}\n";
            stats += $"Disconnected: {dummyClients.Count - connectedCount}\n";
            stats += $"\n=== Network Stats ===\n\n";
            stats += $"Avg RTT: {avgRtt:F1}ms\n";
            stats += $"Min RTT: {(minRtt < float.MaxValue ? minRtt : 0):F1}ms\n";
            stats += $"Max RTT: {maxRtt:F1}ms\n";
            stats += $"\n=== Server Load ===\n\n";
            stats += $"FPS: {1.0f / Time.deltaTime:F0}\n";
            stats += $"Frame Time: {Time.deltaTime * 1000:F1}ms\n";
            
            statisticsText.text = stats;
        }
    }
}

