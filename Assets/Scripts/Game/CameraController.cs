using UnityEngine;
using CustomNetworking.Client;

namespace CustomNetworking.Game
{
    public class CameraController : MonoBehaviour
    {
        [SerializeField] private PlayerController playerController;
        [SerializeField] private float height = 15f;
        [SerializeField] private float distance = 10f;
        [SerializeField] private float angle = 45f;
        [SerializeField] private float smoothSpeed = 5f;
        
        private Vector3 offset;
        
        void Start()
        {
            offset = new Vector3(0, height, -distance);
            transform.rotation = Quaternion.Euler(angle, 0, 0);
        }
        
        void LateUpdate()
        {
            if (playerController == null)
                return;
            
            ClientTank localTank = playerController.GetLocalPlayerTank();
            if (localTank != null)
            {
                Vector3 targetPosition = localTank.Position + offset;
                transform.position = Vector3.Lerp(transform.position, targetPosition, smoothSpeed * Time.deltaTime);
            }
        }
    }
}

