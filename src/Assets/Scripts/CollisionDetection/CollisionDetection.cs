using System;
using UnityEngine;

namespace CollisionDetection
{
    public class CollisionDetection : MonoBehaviour
    {
        public CollisionDetectionManager CollisionDetectionManager;
        private void OnCollisionEnter(Collision collision)
        {
            if (!collision.gameObject.CompareTag("Environment")) return;
        
            CollisionDetectionManager.Collision(new CollisionData
                {CollisionTime = DateTime.Now, CollidedGameObjectName = collision.gameObject.name, Count = 1});
        }
    }
}
