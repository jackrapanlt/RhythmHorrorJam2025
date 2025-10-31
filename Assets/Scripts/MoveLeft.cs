using UnityEngine;

public class MoveMultipleObjectsLeft : MonoBehaviour
{
    [System.Serializable]
    public class MovingObject
    {
        public GameObject target; 
        public float speed = 5f;  
    }

    public MovingObject[] objects;

    void Update()
    {
        foreach (var obj in objects)
        {
            if (obj.target != null)
            {
                
                obj.target.transform.Translate(Vector3.left * obj.speed * Time.deltaTime);
            }
        }
    }
}
