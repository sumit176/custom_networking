using UnityEngine;
using CustomNetworking.Protocol;

namespace CustomNetworking.Client
{
    public class ClientWall : ClientEntity
    {
        public Vector3 Scale { get; set; }
        
        public ClientWall(uint entityId) : base(entityId, EntityType.Wall)
        {
            Scale = Vector3.one;
        }
        
        public override void CreateGameObject()
        {
            if (GameObject != null)
                return;
            
            var wObj = Resources.Load("Wall") as GameObject;
            GameObject = Object.Instantiate(wObj, Position, Quaternion.identity);
            GameObject.name = $"Wall_{EntityId}";
            GameObject.transform.localScale = Scale;
        }
        
        public void SetScale(Vector3 scale)
        {
            Scale = scale;
            if (GameObject != null)
            {
                GameObject.transform.localScale = scale;
            }
        }
        
        public override void UpdateFromState(EntityState state)
        {
            Position = new Vector3(state.PosX, state.PosY, state.PosZ);
            Rotation = state.RotY;
            Health = state.Health;
            OwnerId = state.OwnerId;
            PlayerName = state.PlayerName;
            
            if (GameObject != null)
            {
                GameObject.transform.position = Position;
                GameObject.transform.rotation = Quaternion.Euler(0, Rotation, 0);
            }
        }

        
        public override void Update(float deltaTime)
        {
            //No need to update
        }
    }
}

