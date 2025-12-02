using System.Collections.Generic;
using UnityEngine;

namespace CustomNetworking.Client
{
    public class ClientPrediction
    {
        private struct PredictedInput
        {
            public uint InputSequence;
            public Vector2 MoveInput;
            public Vector2 AimInput;
            public Vector3 PredictedPosition;
            public float PredictedRotation;
        }
        
        private Queue<PredictedInput> inputHistory;
        private const int MAX_INPUT_HISTORY = 60; // 3 seconds at 20Hz
        private const float MOVE_SPEED = 5.0f;
        
        public Vector3 PredictedPosition { get; private set; }
        public float PredictedRotation { get; private set; }
        
        public ClientPrediction()
        {
            inputHistory = new Queue<PredictedInput>();
        }
        
        public void PredictMovement(uint inputSequence, Vector2 moveInput, Vector2 aimInput, float deltaTime)
        {
            // Apply movement
            Vector3 moveDir = new Vector3(moveInput.x, 0, moveInput.y).normalized;
            PredictedPosition += moveDir * MOVE_SPEED * deltaTime;
            
            // Apply rotation
            if (aimInput.sqrMagnitude > 0.01f)
            {
                float angle = Mathf.Atan2(aimInput.x, aimInput.y) * Mathf.Rad2Deg;
                PredictedRotation = angle;
            }
            
            // Store in history
            PredictedInput predicted = new PredictedInput
            {
                InputSequence = inputSequence,
                MoveInput = moveInput,
                AimInput = aimInput,
                PredictedPosition = PredictedPosition,
                PredictedRotation = PredictedRotation
            };
            
            inputHistory.Enqueue(predicted);
            
            // Limit history size
            while (inputHistory.Count > MAX_INPUT_HISTORY)
            {
                inputHistory.Dequeue();
            }
        }
        
        public void Reconcile(Vector3 serverPosition, float serverRotation, uint lastProcessedInput)
        {
            // Check if prediction was correct
            float positionError = Vector3.Distance(PredictedPosition, serverPosition);
            
            if (positionError > 0.5f) // Large error - probably teleported
            {
                // Snap immediately for large errors
                PredictedPosition = serverPosition;
                PredictedRotation = serverRotation;
            }
            else if (positionError > 0.01f) // Small errors - smooth correction
            {
                float blendFactor = Mathf.Clamp(positionError * 2f, 0.1f, 0.5f);
                
                PredictedPosition = Vector3.Lerp(PredictedPosition, serverPosition, blendFactor);
                PredictedRotation = Mathf.LerpAngle(PredictedRotation, serverRotation, blendFactor);
            }
        }
        
        public void Initialize(Vector3 position, float rotation)
        {
            PredictedPosition = position;
            PredictedRotation = rotation;
            inputHistory.Clear();
        }
        
        public void Clear()
        {
            inputHistory.Clear();
            PredictedPosition = Vector3.zero;
            PredictedRotation = 0;
        }
    }
}

