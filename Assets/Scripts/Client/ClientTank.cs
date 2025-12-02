using UnityEngine;
using CustomNetworking.Protocol;
using TMPro;

namespace CustomNetworking.Client
{
    public class ClientTank : ClientEntity
    {
        private GameObject nameTagObject;
        private TextMesh nameTagText;
        private TextMeshPro healthText;
        private Transform turret;
        
        // Smooth movement
        private Vector3 smoothVelocity;
        private float smoothRotationVelocity;
        private const float positionSmoothTime = 0.1f; 
        private const float rotationSmoothTime = 0.1f; 
        
        public ClientTank(uint entityId) : base(entityId, EntityType.Tank)
        {
        }
        
        public override void CreateGameObject()
        {
            if (GameObject != null)
                return;
            
            var rObject = Resources.Load<GameObject>($"Tank");
            GameObject tankGo = GameObject.Instantiate(rObject);
            tankGo.name = $"Tank_{EntityId}";
            GameObject = tankGo;
            turret = tankGo.transform.Find("Rotor");
            MapNameTag();
            MapHealthBar();
        }
        
        private void MapNameTag()
        {
            nameTagObject = GameObject.transform.Find("NameText").gameObject;
            nameTagObject.GetComponent<TextMeshPro>().text = PlayerName;
            nameTagObject.AddComponent<Billboard>();
        }
        
        private void MapHealthBar()
        {
            var healthBarObject = GameObject.transform.Find("HealthText").gameObject;
            healthText = healthBarObject.GetComponent<TextMeshPro>();
            healthText.text = $"health: 100";
            
            // health bar should face camera
            healthBarObject.AddComponent<Billboard>();
        }

        public override void UpdateFromState(EntityState state)
        {
            base.UpdateFromState(state);
            if (healthText != null)
            {
                healthText.text = $"Health: {Health}";
            }
        }

        public override void ApplyDelta(EntityDelta delta)
        {
            base.ApplyDelta(delta);
            if (healthText != null)
            {
                healthText.text = $"Health: {Health}";
            }
        }

        public override void UpdateGameObject()
        {
            if (GameObject != null)
            {
                // Smooth position using SmoothDamp for natural deceleration
                Vector3 currentPos = GameObject.transform.position;
                Vector3 smoothedPos = Vector3.SmoothDamp(
                    currentPos, 
                    Position, 
                    ref smoothVelocity, 
                    positionSmoothTime
                );
                GameObject.transform.position = smoothedPos;
                
                float currentRotation = turret.rotation.eulerAngles.y;
                float smoothedRotation = Mathf.SmoothDampAngle(
                    currentRotation,
                    Rotation,
                    ref smoothRotationVelocity,
                    rotationSmoothTime
                );
                turret.rotation = Quaternion.Euler(0, smoothedRotation, 0);
            }
        }
    }
    
    public class Billboard : MonoBehaviour
    {
        void LateUpdate()
        {
            if (Camera.main != null)
            {
                transform.rotation = Camera.main.transform.rotation;
            }
        }
    }
}

