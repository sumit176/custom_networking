using UnityEngine;
using CustomNetworking.Protocol;

namespace CustomNetworking.Server
{
    /// <summary>
    /// Server-side physics and collision detection
    /// </summary>
    public static class ServerPhysics
    {
        public const float TANK_RADIUS = 1f;
        public const float PROJECTILE_RADIUS = 0.15f;
        public const float TANK_SPEED = 5.0f;
        
        public static void MoveTank(ServerEntity tank, Vector2 moveInput, float deltaTime, ServerGameState gameState)
        {
            if (tank.Type != EntityType.Tank)
                return;
            
            if (moveInput.sqrMagnitude < 0.001f)
                return;
            
            Vector3 moveDir = new Vector3(moveInput.x, 0, moveInput.y).normalized;
            Vector3 newPosition = tank.Position + moveDir * TANK_SPEED * deltaTime;
            
            // Check collision with walls
            foreach (var entity in gameState.GetAllEntities())
            {
                if (entity.Type == EntityType.Wall)
                {
                    if (CheckTankWallCollision(newPosition, entity))
                    {
                        // Collision detected, don't move
                        return;
                    }
                }
            }
            
            // Check collision with other tanks
            foreach (var entity in gameState.GetAllEntities())
            {
                if (entity.Type == EntityType.Tank && entity.EntityId != tank.EntityId)
                {
                    if (Vector3.Distance(newPosition, entity.Position) < TANK_RADIUS * 2)
                    {
                        // Collision with another tank, don't move
                        return;
                    }
                }
            }
            
            tank.Position = newPosition;
        }
        
        public static void RotateTank(ServerEntity tank, Vector2 aimInput)
        {
            if (tank.Type != EntityType.Tank)
                return;
            
            if (aimInput.sqrMagnitude > 0.01f)
            {
                float angle = Mathf.Atan2(aimInput.x, aimInput.y) * Mathf.Rad2Deg;
                tank.Rotation = angle;
                // Debug.Log($"Rotate: aimInput=({aimInput.x}, {aimInput.y}), angle={angle}°");
            }
        }
        
        /// <summary>
        /// Check projectile collisions with entities
        /// Returns the entity ID hit, or 0 if no hit
        /// </summary>
        public static uint CheckProjectileCollisions(ServerEntity projectile, ServerGameState gameState)
        {
            if (projectile.Type != EntityType.Projectile)
                return 0;
            
            // Check collision with walls
            foreach (var entity in gameState.GetAllEntities())
            {
                if (entity.Type == EntityType.Wall)
                {
                    if (CheckProjectileWallCollision(projectile.Position, entity))
                    {
                        return entity.EntityId;
                    }
                }
            }
            
            // Check collision with tanks (but not the owner)
            foreach (var entity in gameState.GetAllEntities())
            {
                if (entity.Type == EntityType.Tank && entity.EntityId != projectile.OwnerId)
                {
                    if (Vector3.Distance(projectile.Position, entity.Position) < TANK_RADIUS + PROJECTILE_RADIUS)
                    {
                        return entity.EntityId;
                    }
                }
            }
            
            return 0;
        }
        
        /// <summary>
        /// Check if tank collides with wall (AABB vs Circle)
        /// </summary>
        private static bool CheckTankWallCollision(Vector3 tankPos, ServerEntity wall)
        {
            // Wall is AABB, tank is circle
            // For top-down game, only check XZ plane
            Vector3 wallMin = wall.Position - wall.Scale * 0.5f;
            Vector3 wallMax = wall.Position + wall.Scale * 0.5f;
            
            // Find closest point on AABB to circle center (XZ plane only)
            float closestX = Mathf.Clamp(tankPos.x, wallMin.x, wallMax.x);
            float closestZ = Mathf.Clamp(tankPos.z, wallMin.z, wallMax.z);
            
            // Calculate 2D distance in XZ plane only
            float dx = tankPos.x - closestX;
            float dz = tankPos.z - closestZ;
            float distanceSquared = dx * dx + dz * dz;
            
            return distanceSquared <= TANK_RADIUS * TANK_RADIUS;
        }
        
        private static bool CheckProjectileWallCollision(Vector3 projPos, ServerEntity wall)
        {
            // For top-down game, only check XZ plane
            Vector3 wallMin = wall.Position - wall.Scale * 0.5f;
            Vector3 wallMax = wall.Position + wall.Scale * 0.5f;
            
            // Find closest point on AABB to circle center (XZ plane only)
            float closestX = Mathf.Clamp(projPos.x, wallMin.x, wallMax.x);
            float closestZ = Mathf.Clamp(projPos.z, wallMin.z, wallMax.z);
            
            // Calculate 2D distance in XZ plane only
            float dx = projPos.x - closestX;
            float dz = projPos.z - closestZ;
            float distanceSquared = dx * dx + dz * dz;
            
            return distanceSquared <= PROJECTILE_RADIUS * PROJECTILE_RADIUS;
        }
        
        /// <summary>
        /// Get spawn position that doesn't collide with anything
        /// </summary>
        public static Vector3 GetSafeSpawnPosition(ServerGameState gameState, System.Random random)
        {
            const int MAX_ATTEMPTS = 20;
            const float SPAWN_AREA_SIZE = 20f;
            
            for (int i = 0; i < MAX_ATTEMPTS; i++)
            {
                float x = (float)(random.NextDouble() * 2.0 - 1.0) * SPAWN_AREA_SIZE;
                float z = (float)(random.NextDouble() * 2.0 - 1.0) * SPAWN_AREA_SIZE;
                Vector3 position = new Vector3(x, 0, z);
                
                bool collision = false;
                
                // Check against all entities
                foreach (var entity in gameState.GetAllEntities())
                {
                    if (entity.Type == EntityType.Wall)
                    {
                        if (CheckTankWallCollision(position, entity))
                        {
                            collision = true;
                            break;
                        }
                    }
                    else if (entity.Type == EntityType.Tank)
                    {
                        if (Vector3.Distance(position, entity.Position) < TANK_RADIUS * 3)
                        {
                            collision = true;
                            break;
                        }
                    }
                }
                
                if (!collision)
                    return position;
            }
            return Vector3.zero;
        }
    }
}

