using UnityEngine;

namespace CustomNetworking.Server
{
    /// <summary>
    /// Server launcher for headless mode and UI control
    /// </summary>
    public class ServerLauncher : MonoBehaviour
    {
        [SerializeField] private GameServer gameServer;
        [SerializeField] private bool autoStart = true;
        [SerializeField] private bool headlessMode = false;
        
        void Start()
        {
            // Check for headless mode from command line
            string[] args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-headless" || args[i] == "--headless")
                {
                    headlessMode = true;
                }
                
                if (args[i] == "-port" && i + 1 < args.Length)
                {
                    if (int.TryParse(args[i + 1], out int port))
                    {
                        // Set port via reflection or make it accessible
                        Debug.Log($"Server port set to: {port}");
                    }
                }
            }
            
            if (headlessMode)
            {
                Application.targetFrameRate = 60;
                Debug.Log("Running in headless mode");
            }
            
            if (autoStart)
            {
                gameServer.StartServer();
            }
        }
        
        void OnGUI()
        {
            if (headlessMode)
                return;
            
            // Simple debug UI
            GUILayout.BeginArea(new Rect(10, 10, 400, 100));
            GUILayout.Label(gameServer != null ? gameServer.GetStatus() : "No server");
            
            if (GUILayout.Button("Start Server"))
            {
                gameServer?.StartServer();
            }
            
            if (GUILayout.Button("Stop Server"))
            {
                gameServer?.StopServer();
            }
            
            GUILayout.EndArea();
        }
    }
}

