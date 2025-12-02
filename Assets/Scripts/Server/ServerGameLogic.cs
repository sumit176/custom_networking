using UnityEngine;
using System.Collections.Generic;
using CustomNetworking.Protocol;

namespace CustomNetworking.Server
{
    /// <summary>
    /// Server-side game logic: damage, death, respawn, shooting
    /// </summary>
    public class ServerGameLogic
    {
        public const byte PROJECTILE_DAMAGE = 25;
        public const float SHOOT_COOLDOWN = 0.5f; // 500ms between shots
        public const float PROJECTILE_SPEED = 15.0f;
        
        private Dictionary<uint, float> shootCooldowns;

        public ServerGameLogic()
        {
            shootCooldowns = new Dictionary<uint, float>();
            new System.Random();
        }
        
        public void UpdateCooldowns(float deltaTime)
        {
            List<uint> toRemove = new List<uint>();
            
            // Create a copy of keys to avoid modifying collection during enumeration
            List<uint> keys = new List<uint>(shootCooldowns.Keys);
            
            foreach (uint key in keys)
            {
                float newValue = shootCooldowns[key] - deltaTime;
                shootCooldowns[key] = newValue;
                
                if (newValue <= 0)
                {
                    toRemove.Add(key);
                }
            }
            
            foreach (uint id in toRemove)
            {
                shootCooldowns.Remove(id);
            }
        }
        
        /// <summary>
        /// Try to shoot projectile from tank
        /// Returns projectile ID if shot, 0 if on cooldown
        /// </summary>
        public uint TryShoot(ServerEntity tank, ServerGameState gameState)
        {
            if (tank.Type != EntityType.Tank)
                return 0;
            
            // Check cooldown
            if (!shootCooldowns.TryAdd(tank.EntityId, SHOOT_COOLDOWN))
                return 0;

            // Calculate projectile spawn position and velocity
            float angleRad = tank.Rotation * Mathf.Deg2Rad;
            Vector3 direction = new Vector3(Mathf.Sin(angleRad), 0, Mathf.Cos(angleRad));
            Vector3 spawnPos = tank.Position + direction * 0.7f; // Spawn in front of tank
            Vector3 velocity = direction * PROJECTILE_SPEED;
            
            // Debug.Log($"Shoot: tankPos={tank.Position}, tankRot={tank.Rotation}°, direction={direction}, spawnPos={spawnPos}, velocity={velocity}");
            
            // Spawn projectile
            uint projectileId = gameState.SpawnProjectile(tank.EntityId, spawnPos, velocity);
            
            return projectileId;
        }
        
        private bool ApplyDamage(ServerEntity target, byte damage, out byte newHealth)
        {
            if (target.Health <= damage)
            {
                target.Health = 0;
                newHealth = 0;
                return true;
            }

            target.Health -= damage;
            newHealth = target.Health;
            return false;
        }
        
        /// <summary>
        /// Check and process all projectile collisions
        /// Returns list of events (damage, death, etc.)
        /// </summary>
        public List<GameEvent> ProcessProjectileCollisions(ServerGameState gameState)
        {
            List<GameEvent> events = new List<GameEvent>();
            List<uint> projectilesToRemove = new List<uint>();
            
            foreach (var entity in gameState.GetAllEntities())
            {
                if (entity.Type == EntityType.Projectile)
                {
                    uint hitEntityId = ServerPhysics.CheckProjectileCollisions(entity, gameState);
                    
                    if (hitEntityId != 0)
                    {
                        projectilesToRemove.Add(entity.EntityId);
                        
                        ServerEntity hitEntity = gameState.GetEntity(hitEntityId);
                        
                        if (hitEntity != null && hitEntity.Type == EntityType.Tank)
                        {
                            // Apply damage to tank
                            bool died = ApplyDamage(hitEntity, PROJECTILE_DAMAGE, out byte newHealth);
                            
                            events.Add(new DamageEvent
                            {
                                TargetId = hitEntityId,
                                SourceId = entity.OwnerId,
                                Damage = PROJECTILE_DAMAGE,
                                NewHealth = newHealth
                            });
                            
                            if (died)
                            {
                                events.Add(new DeathEvent
                                {
                                    PlayerId = hitEntityId,
                                    KillerId = entity.OwnerId
                                });
                            }
                        }
                    }
                }
            }
            
            // Remove projectiles that hit something
            foreach (uint projectileId in projectilesToRemove)
            {
                gameState.DespawnEntity(projectileId);
                events.Add(new DespawnEvent { EntityId = projectileId });
            }
            
            return events;
        }
    }
    
    // Game event types
    public abstract class GameEvent { }
    
    public class DamageEvent : GameEvent
    {
        public uint TargetId;
        public uint SourceId;
        public byte Damage;
        public byte NewHealth;
    }
    
    public class DeathEvent : GameEvent
    {
        public uint PlayerId;
        public uint KillerId;
    }
    
    public class DespawnEvent : GameEvent
    {
        public uint EntityId;
    }
    
    public class SpawnEvent : GameEvent
    {
        public uint EntityId;
        public EntityType Type;
        public Vector3 Position;
    }
}

