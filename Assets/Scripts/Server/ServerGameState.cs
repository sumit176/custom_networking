using System.Collections.Generic;
using UnityEngine;
using CustomNetworking.Protocol;

namespace CustomNetworking.Server
{
    /// <summary>
    /// Server-side game state management for all entities
    /// </summary>
    public class ServerGameState
    {
        private Dictionary<uint, ServerEntity> entities;
        private uint nextEntityId;
        
        public ServerGameState()
        {
            entities = new Dictionary<uint, ServerEntity>();
            nextEntityId = 1;
        }
        
        public uint SpawnTank(string playerName, Vector3 position)
        {
            uint entityId = nextEntityId++;
            
            ServerEntity tank = new ServerEntity
            {
                EntityId = entityId,
                Type = EntityType.Tank,
                Position = position,
                Rotation = 0,
                Health = 100,
                MaxHealth = 100,
                OwnerId = entityId, // Tanks own themselves
                PlayerName = playerName,
                Velocity = Vector3.zero
            };
            
            entities[entityId] = tank;
            return entityId;
        }
        
        public uint SpawnProjectile(uint ownerId, Vector3 position, Vector3 velocity)
        {
            uint entityId = nextEntityId++;
            
            ServerEntity projectile = new ServerEntity
            {
                EntityId = entityId,
                Type = EntityType.Projectile,
                Position = position,
                Rotation = 0,
                Health = 1,
                MaxHealth = 1,
                OwnerId = ownerId,
                PlayerName = "",
                Velocity = velocity,
                LifeTime = 5.0f // Projectiles live for 5 seconds
            };
            
            entities[entityId] = projectile;
            return entityId;
        }
        
        public uint SpawnWall(Vector3 position, Vector3 scale)
        {
            uint entityId = nextEntityId++;
            
            ServerEntity wall = new ServerEntity
            {
                EntityId = entityId,
                Type = EntityType.Wall,
                Position = position,
                Rotation = 0,
                Health = 255,
                MaxHealth = 255,
                OwnerId = 0,
                PlayerName = "",
                Velocity = Vector3.zero,
                Scale = scale
            };
            
            entities[entityId] = wall;
            return entityId;
        }
        
        public void DespawnEntity(uint entityId)
        {
            entities.Remove(entityId);
        }
        
        /// <summary>
        /// Get entity by ID
        /// </summary>
        public ServerEntity GetEntity(uint entityId)
        {
            entities.TryGetValue(entityId, out ServerEntity entity);
            return entity;
        }
        
        public IEnumerable<ServerEntity> GetAllEntities()
        {
            return entities.Values;
        }
        
        /// <summary>
        /// Update entity physics (movement, lifetime) - does NOT update previous state
        /// </summary>
        public List<uint> UpdateEntitiesPhysics(float deltaTime)
        {
            List<uint> entitiesToRemove = new List<uint>();
            
            foreach (var entity in entities.Values)
            {
                // Update projectile lifetime
                if (entity.Type == EntityType.Projectile)
                {
                    entity.LifeTime -= deltaTime;
                    if (entity.LifeTime <= 0)
                    {
                        entitiesToRemove.Add(entity.EntityId);
                        continue;
                    }
                    
                    // Move projectile
                    entity.Position += entity.Velocity * deltaTime;
                }
            }
            
            return entitiesToRemove;
        }
        
        public void UpdatePreviousState()
        {
            foreach (var entity in entities.Values)
            {
                entity.PreviousPosition = entity.Position;
                entity.PreviousRotation = entity.Rotation;
                entity.PreviousHealth = entity.Health;
            }
        }
        
        public List<ServerEntity> GetChangedEntities()
        {
            List<ServerEntity> changed = new List<ServerEntity>();
            
            foreach (var entity in entities.Values)
            {
                if (entity.HasChanged())
                {
                    changed.Add(entity);
                }
            }
            
            return changed;
        }
        
        public void Clear()
        {
            entities.Clear();
            nextEntityId = 1;
        }
    }
    
    public class ServerEntity
    {
        public uint EntityId { get; set; }
        public EntityType Type { get; set; }
        public Vector3 Position { get; set; }
        public float Rotation { get; set; } // Y-axis rotation for top-down
        public byte Health { get; set; }
        public byte MaxHealth { get; set; }
        public uint OwnerId { get; set; }
        public string PlayerName { get; set; }
        public Vector3 Velocity { get; set; }
        public float LifeTime { get; set; }
        public Vector3 Scale { get; set; } = Vector3.one;
        
        // For delta tracking
        public Vector3 PreviousPosition { get; set; }
        public float PreviousRotation { get; set; }
        public byte PreviousHealth { get; set; }
        
        /// <summary>
        /// Check if entity has changed since last frame
        /// </summary>
        public bool HasChanged()
        {
            return Vector3.Distance(Position, PreviousPosition) > 0.001f ||
                   Mathf.Abs(Rotation - PreviousRotation) > 0.001f ||
                   Health != PreviousHealth;
        }
        
        public EntityState ToEntityState()
        {
            return new EntityState
            {
                EntityId = EntityId,
                Type = Type,
                PosX = Position.x,
                PosY = Position.y,
                PosZ = Position.z,
                RotY = Rotation,
                Health = Health,
                OwnerId = OwnerId,
                PlayerName = PlayerName,
                ScaleX = Scale.x,
                ScaleY = Scale.y,
                ScaleZ = Scale.z
            };
        }
        
        public EntityDelta ToDelta()
        {
            EntityDelta delta = new EntityDelta { EntityId = EntityId };
            
            if (Mathf.Abs(Position.x - PreviousPosition.x) > 0.001f)
                delta.PosX = Position.x;
            
            if (Mathf.Abs(Position.y - PreviousPosition.y) > 0.001f)
                delta.PosY = Position.y;
            
            if (Mathf.Abs(Position.z - PreviousPosition.z) > 0.001f)
                delta.PosZ = Position.z;
            
            if (Mathf.Abs(Rotation - PreviousRotation) > 0.001f)
                delta.RotY = Rotation;
            
            if (Health != PreviousHealth)
                delta.Health = Health;
            
            return delta;
        }
    }
}

