using System.Collections.Generic;
using UnityEngine;
using CustomNetworking.Protocol;

namespace CustomNetworking.Client
{
    /// <summary>
    /// Client-side game state management
    /// </summary>
    public class ClientGameState
    {
        private Dictionary<uint, ClientEntity> entities;
        private Queue<SnapshotMessage> snapshotBuffer;
        private const int SNAPSHOT_BUFFER_SIZE = 5;
        
        public uint LocalPlayerId { get; set; }
        
        public ClientGameState()
        {
            entities = new Dictionary<uint, ClientEntity>();
            snapshotBuffer = new Queue<SnapshotMessage>();
        }
        
        public void ProcessSnapshot(SnapshotMessage snapshot)
        {
            // Add to buffer for interpolation
            snapshotBuffer.Enqueue(snapshot);
            
            // Limit buffer size
            while (snapshotBuffer.Count > SNAPSHOT_BUFFER_SIZE)
            {
                snapshotBuffer.Dequeue();
            }
            
            // Apply snapshot
            ApplySnapshot(snapshot);
        }
        
        private void ApplySnapshot(SnapshotMessage snapshot)
        {
            HashSet<uint> snapshotEntityIds = new HashSet<uint>();
            
            foreach (var state in snapshot.Entities)
            {
                snapshotEntityIds.Add(state.EntityId);
                
                if (entities.TryGetValue(state.EntityId, out ClientEntity entity))
                {
                    entity.UpdateFromState(state);
                }
                else
                {
                    SpawnEntity(state);
                }
            }
            
            // Remove entities not in snapshot
            // Exception: static entities (walls) - they're only sent once on connection
            List<uint> toRemove = new List<uint>();
            foreach (var entityId in entities.Keys)
            {
                if (!snapshotEntityIds.Contains(entityId))
                {
                    if (entities[entityId].Type == EntityType.Wall)
                        continue;
                        
                    toRemove.Add(entityId);
                }
            }
            
            foreach (uint entityId in toRemove)
            {
                DespawnEntity(entityId);
            }
        }
        
        public void ProcessDeltaUpdate(DeltaUpdateMessage delta)
        {
            foreach (var entityDelta in delta.Deltas)
            {
                if (entities.TryGetValue(entityDelta.EntityId, out ClientEntity entity))
                {
                    entity.ApplyDelta(entityDelta);
                }
            }
        }
        
        private void SpawnEntity(EntityState state)
        {
            ClientEntity entity = null;
            
            switch (state.Type)
            {
                case EntityType.Tank:
                    entity = new ClientTank(state.EntityId);
                    break;
                
                case EntityType.Projectile:
                    entity = new ClientProjectile(state.EntityId);
                    break;
                
                case EntityType.Wall:
                    ClientWall wall = new ClientWall(state.EntityId);
                    wall.SetScale(new Vector3(state.ScaleX, state.ScaleY, state.ScaleZ));
                    entity = wall;
                    break;
            }
            
            if (entity != null)
            {
                entity.UpdateFromState(state);
                entity.CreateGameObject();
                entities[state.EntityId] = entity;
                
                Debug.Log($"Spawned entity: {state.Type} (ID: {state.EntityId})");
            }
        }
        
        /// <summary>
        /// Spawn entity from spawn message
        /// </summary>
        public void SpawnEntity(EntitySpawnMessage message)
        {
            if (entities.ContainsKey(message.EntityId))
                return;
            
            ClientEntity entity = null;
            
            switch (message.EntityType)
            {
                case EntityType.Tank:
                    entity = new ClientTank(message.EntityId);
                    break;
                
                case EntityType.Projectile:
                    entity = new ClientProjectile(message.EntityId);
                    break;
                
                case EntityType.Wall:
                    entity = new ClientWall(message.EntityId);
                    break;
            }
            
            if (entity != null)
            {
                entity.Position = new Vector3(message.PosX, message.PosY, message.PosZ);
                entity.Rotation = message.RotY;
                entity.OwnerId = message.OwnerId;
                entity.PlayerName = message.PlayerName;
                
                entity.CreateGameObject();
                entities[message.EntityId] = entity;
                
                Debug.Log($"Spawned entity: {message.EntityType} (ID: {message.EntityId})");
            }
        }
        
        public void SpawnProjectile(ProjectileSpawnMessage message)
        {
            if (entities.ContainsKey(message.ProjectileId))
                return;
            
            ClientProjectile projectile = new ClientProjectile(message.ProjectileId);
            Vector3 serverSpawnPos = new Vector3(message.PosX, message.PosY, message.PosZ);
            projectile.Velocity = new Vector3(message.VelX, message.VelY, message.VelZ);
            projectile.OwnerId = message.OwnerId;
            
            ClientEntity ownerTank = GetEntity(message.OwnerId);
            if (ownerTank != null && ownerTank.EntityId == LocalPlayerId)
            {
                Vector3 spawnDirection = projectile.Velocity.normalized;
                float spawnOffset = 0.7f; // Distance in front of tank
                
                projectile.Position = ownerTank.Position + spawnDirection * spawnOffset;
            }
            else
            {
                // For other players, use server position as-is
                projectile.Position = serverSpawnPos;
                Debug.Log($"REMOTE Projectile: serverSpawnPos={serverSpawnPos}");
            }
            
            projectile.CreateGameObject();
            entities[message.ProjectileId] = projectile;
        }
        
        public void DespawnEntity(uint entityId)
        {
            if (entities.TryGetValue(entityId, out ClientEntity entity))
            {
                entity.Destroy();
                entities.Remove(entityId);
                Debug.Log($"Despawned entity ID: {entityId}");
            }
        }
        
        public void Update(float deltaTime)
        {
            foreach (var entity in entities.Values)
            {
                // Skip local player if using prediction
                if (entity.EntityId == LocalPlayerId && entity.Type == EntityType.Tank)
                {
                    continue;
                }
                
                entity.Update(deltaTime);
            }
        }
        
        public ClientEntity GetEntity(uint entityId)
        {
            entities.TryGetValue(entityId, out ClientEntity entity);
            return entity;
        }
        
        public ClientTank GetLocalPlayerTank()
        {
            if (LocalPlayerId != 0 && entities.TryGetValue(LocalPlayerId, out ClientEntity entity))
            {
                return entity as ClientTank;
            }
            return null;
        }
        
        public void Clear()
        {
            foreach (var entity in entities.Values)
            {
                entity.Destroy();
            }
            entities.Clear();
            snapshotBuffer.Clear();
        }
    }
}

