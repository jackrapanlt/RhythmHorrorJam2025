using UnityEngine;

public class MissZone : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
       
        var m = other.GetComponentInParent<MonsterRhythm>();
        if (m != null)
        {
            Debug.Log("Miss");
        }
    }
}
