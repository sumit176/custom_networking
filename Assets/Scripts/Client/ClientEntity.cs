using UnityEngine;
using CustomNetworking.Protocol;

namespace CustomNetworking.Client
{
    public abstract class ClientEntity
    {
        public uint EntityId { get; set; }
        public EntityType Type { get; set; }
        public GameObject GameObject { get; set; }
        
        // Current interpolated state
        public Vector3 Position { get; set; }
        public float Rotation { get; set; }
        public byte Health { get; set; }
        public uint OwnerId { get; set; }
        public string PlayerName { get; set; }
        public Vector3 ServerPosition => toPosition;
        
        // Interpolation data
        protected Vector3 fromPosition;
        protected Vector3 toPosition;
        protected float fromRotation;
        protected float toRotation;
        protected float interpolationTime;
        protected float interpolationDuration = 0.15f; // 150ms
        
        protected ClientEntity(uint entityId, EntityType type)
        {
            EntityId = entityId;
            Type = type;
        }
        
        public virtual void UpdateFromState(EntityState state)
        {
            // interpolation from current position
            fromPosition = Position;
            fromRotation = Rotation;
            
            // new target
            toPosition = new Vector3(state.PosX, state.PosY, state.PosZ);
            toRotation = state.RotY;
            
            Health = state.Health;
            OwnerId = state.OwnerId;
            PlayerName = state.PlayerName;
            
            interpolationTime = 0;
        }
        
        public virtual void ApplyDelta(EntityDelta delta)
        {
            fromPosition = Position;
            fromRotation = Rotation;
            
            if (delta.PosX.HasValue || delta.PosY.HasValue || delta.PosZ.HasValue)
            {
                toPosition = new Vector3(
                    delta.PosX ?? toPosition.x,
                    delta.PosY ?? toPosition.y,
                    delta.PosZ ?? toPosition.z
                );
            }
            
            if (delta.RotY.HasValue)
                toRotation = delta.RotY.Value;
            
            if (delta.Health.HasValue)
                Health = delta.Health.Value;
            
            interpolationTime = 0;
        }
        
        public virtual void Update(float deltaTime)
        {
            if (interpolationTime < interpolationDuration)
            {
                interpolationTime += deltaTime;
                float t = interpolationTime / interpolationDuration;
                
                Position = Vector3.Lerp(fromPosition, toPosition, t);
                Rotation = Mathf.LerpAngle(fromRotation, toRotation, t);
            }
            else
            {
                Position = toPosition;
                Rotation = toRotation;
            }
            
            UpdateGameObject();
        }
        
        public virtual void UpdateGameObject()
        {
            if (GameObject != null)
            {
                GameObject.transform.position = Position;
                GameObject.transform.rotation = Quaternion.Euler(0, Rotation, 0);
            }
        }
        
        public abstract void CreateGameObject();
        
        public virtual void Destroy()
        {
            if (GameObject != null)
            {
                Object.Destroy(GameObject);
                GameObject = null;
            }
        }
    }
}

