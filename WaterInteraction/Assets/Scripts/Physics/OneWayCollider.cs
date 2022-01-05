using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Based on the concept of:
//https://answers.unity.com/questions/362021/eficient-one-way-collider.html

public class OneWayCollider : MonoBehaviour
{
    List<Collider> _Colliders = new List<Collider>();
    // Start is called before the first frame update
    void Start()
    {
        Collider[] Colliders = GetComponents<Collider>();
        foreach (Collider col in Colliders)
        {
            if (!col.isTrigger) _Colliders.Add(col);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        IgnoreCollisionWith(other, true);
        //Debug.Log("TriggerEnter");
    }

    private void OnTriggerExit(Collider other)
    {
        IgnoreCollisionWith(other, false);
        //Debug.Log("TriggerExit");
    }

    void IgnoreCollisionWith(Collider other, bool ignore)
    {
        foreach (Collider col in _Colliders)
        {
            Physics.IgnoreCollision(col, other, ignore);
        }
    }
}
