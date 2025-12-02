using UnityEngine;
using CustomNetworking.Client;

namespace CustomNetworking.Game
{
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private GameClient gameClient;
        [SerializeField] private bool useMouseAim = true;
        
        private Vector2 moveInput;
        private Vector2 aimInput;
        private bool shootInput;
        
        void Update()
        {
            if (gameClient == null || !gameClient.IsConnected)
                return;
            
            // Get movement input
            moveInput.x = Input.GetAxisRaw("Horizontal");
            moveInput.y = Input.GetAxisRaw("Vertical");
            
            if (moveInput.sqrMagnitude > 1)
            {
                moveInput.Normalize();
            }
            
            // Get aim input
            if (useMouseAim)
            {
                // Mouse aim - convert screen position to world direction
                if (Camera.main != null)
                {
                    Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                    Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
                    float rayDistance;
                    
                    if (groundPlane.Raycast(ray, out rayDistance))
                    {
                        Vector3 worldPoint = ray.GetPoint(rayDistance);
                        ClientTank localTank = gameClient?.GetLocalPlayerTank();
                        
                        if (localTank != null)
                        {
                            Vector3 aimDir = (worldPoint - localTank.Position).normalized;
                            aimInput.x = aimDir.x;
                            aimInput.y = aimDir.z;
                        }
                    }
                }
            }
            
            // Get shoot input
            if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
            {
                shootInput = true;
            }
            
            gameClient.SetInput(moveInput, aimInput, shootInput);
            
            shootInput = false;
        }
        
        public ClientTank GetLocalPlayerTank()
        {
            return gameClient?.GetLocalPlayerTank();
        }
    }
}

