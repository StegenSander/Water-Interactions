using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ThrowObject : MonoBehaviour
{
    [SerializeField] GameObject _ObjectToThrow;
    [SerializeField] float _ThrowForce = 1;
    GameObject _BallParent;

    private void Start()
    {
        _BallParent = new GameObject();
        _BallParent.name = "BallParent";
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0) && !Helpers.IsPointerOverUIObject())
        {
            Throw();
        }
    }

    void Throw()
    {
        if (!_ObjectToThrow) return;

        GameObject obj = Instantiate(_ObjectToThrow, Camera.main.transform.position, _ObjectToThrow.transform.rotation, _BallParent.transform);

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        obj.GetComponent<Rigidbody>().AddForce(ray.direction * _ThrowForce, ForceMode.Impulse);
    }

    public void SetObjectToThrow(GameObject obj)
    {
        _ObjectToThrow = obj;
    }
}
