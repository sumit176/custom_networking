using UnityEngine;
using CustomNetworking.Protocol;

namespace CustomNetworking.Client
{
    public class ClientProjectile : ClientEntity
    {
        public Vector3 Velocity { get; set; }
        
        public ClientProjectile(uint entityId) : base(entityId, EntityType.Projectile)
        {
        }
        
        public override void CreateGameObject()
        {
            if (GameObject != null)
                return;
            
            // Debug.Log($"Create Projectile: Pos={Position}, Vel={Velocity}");
            var gobj = Resources.Load<GameObject>("TankProjectile");
            GameObject = Object.Instantiate(gobj);
            GameObject.name = $"Projectile_{EntityId}";
        }
        
        public override void Update(float deltaTime)
        {
            // use extrapolation instead of interpolation
            if (Velocity != Vector3.zero)
            {
                Position += Velocity * deltaTime;
            }
            
            UpdateGameObject();
        }
    }
}

