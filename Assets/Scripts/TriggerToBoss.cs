using UnityEngine;

public class TriggerToBoss : MonoBehaviour
{
  
    [SerializeField] private GameObject targetToActivate;

    [SerializeField] private string playerName = "#Player";
    [SerializeField] private string playerTag = "Player";

    private bool triggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (triggered) return;

      
        if (other.gameObject.name == playerName || other.CompareTag(playerTag))
        {
            triggered = true;

            if (targetToActivate != null)
            {
                targetToActivate.SetActive(true);
               
            }
            else
            {
                Debug.LogWarning("[TriggerToBoss] targetToActivate  Inspector");
            }
        }
    }
}
