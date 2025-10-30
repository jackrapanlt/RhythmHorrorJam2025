using UnityEngine;

public class MoveLeft : MonoBehaviour
{
    public float Speed;
    
    void Update()
    {
        transform.Translate(Vector3.left*Speed* Time.deltaTime);
    }
}
