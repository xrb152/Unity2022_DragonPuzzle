using UnityEngine;

namespace RescuePrincess.Gameplay
{
    public class CannonRangeTrigger : MonoBehaviour
    {
       private LevelObject levelObject;

        private void Awake()
        {
            levelObject = GetComponentInParent<LevelObject>();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (levelObject != null)
                levelObject.OnCannonRangeEnter(other);
            //Debug.Log("OnTriggerEnter2D: " + other.name);
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (levelObject != null)
                levelObject.OnCannonRangeExit(other);
        }
    }
} 