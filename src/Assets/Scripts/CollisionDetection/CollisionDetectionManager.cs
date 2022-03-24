using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace CollisionDetection
{
    public class CollisionDetectionManager : MonoBehaviour
    {
        [SerializeField] private Camera vrCamera;
        private readonly List<CollisionData> _collisionList = new List<CollisionData>();

        private void Awake()
        {
            if(!vrCamera)
            {
                vrCamera = Camera.main;
                if (!vrCamera)
                    vrCamera = FindObjectOfType<Camera>();
            }
            var boxCollider = vrCamera.gameObject.AddComponent<BoxCollider>();
            boxCollider.size = new Vector3(.5f, 1, .5f);
            boxCollider.center = new Vector3(0, -.5f, 0);
            var collisionDetection = vrCamera.gameObject.AddComponent<CollisionDetection>();
            collisionDetection.CollisionDetectionManager = this;
            vrCamera.gameObject.AddComponent<Rigidbody>();
        }

        public void Collision(CollisionData collisionData)
        {
            var lastValue = _collisionList.LastOrDefault();

            if (lastValue != null && (collisionData.CollisionTime - lastValue.CollisionTime).Seconds < 1  && lastValue.CollidedGameObjectName == collisionData.CollidedGameObjectName)
            {
                _collisionList[_collisionList.Count - 1].Count++;
            }
            else
            {
                _collisionList.Add(collisionData);
            }
        }
    
        private DateTime _localDate = DateTime.Now;

        /// <summary>
        /// Writes a csv-file with the current time in its name.
        /// On Android files are saved under /storage/emulated/0/Android/data/<packagename>/files
        /// on Windows under %userprofile%\AppData\Local\Packages\<productname>\LocalState
        /// </summary>
        private void WriteCsv()
        {
            string path = Application.persistentDataPath;
            string fileName = "Collisions_" + _localDate.ToString("HH_mm_ss");
            string fullFileName = path + "/" + fileName + ".csv";

            TextWriter writer = new StreamWriter(fullFileName, false);
        
            int totalCollisions = _collisionList.Sum(collision => collision.Count);
            writer.WriteLine("Total Collisions detected: " + totalCollisions);

            foreach (var collision in _collisionList)
            {
                writer.WriteLine(collision.CollisionTime + ", Object: " + collision.CollidedGameObjectName + " x" + collision.Count);
            }

            writer.Close();
        }

        private void OnDestroy()
        {
            WriteCsv();
            Debug.Log("File written to: " + Application.persistentDataPath);
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (!pauseStatus) return;
            WriteCsv();
            Debug.Log("File written to: " + Application.persistentDataPath);
        }
    }

    public class CollisionData
    {
        public DateTime CollisionTime;
        public string CollidedGameObjectName;
        public int Count;
    }
}