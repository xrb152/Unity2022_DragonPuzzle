using UnityEngine;

namespace RescuePrincess.Gameplay
{
    public class SelectionColliderListener : MonoBehaviour
    {
        private void OnTriggerEnter2D(Collider2D other)
        {
            var myBox = GetComponentInParent<ColorBox>();
            var otherBox = other.GetComponentInParent<ColorBox>();
            if (myBox != null && otherBox != null && myBox != otherBox)
            {
                myBox.OnSelectionColliderHit(otherBox);
            }
        }
    }
} 