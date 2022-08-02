using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraController : MonoBehaviour
{
    private Camera _cam;
    private Transform _target;
    private Vector3 _velocity = Vector3.zero;
    private float _minHeight;

    [SerializeField] private float _dampTime;
    

    private void Start()
    {
        _cam = GetComponent<Camera>();
        _target = GameObject.FindWithTag("Player").transform;
        _minHeight = transform.position.y;
    }

    void Update()
    {
        if(_target)
        {
            Vector3 point = _cam.WorldToViewportPoint(_target.position);
            Vector3 delta = _target.position - _cam.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, point.z));
            Vector3 destination = transform.position + delta;
            if(destination.y < _minHeight)
            {
                destination.y = _minHeight;
            }
            transform.position = Vector3.SmoothDamp(transform.position, destination, ref _velocity, _dampTime);
        }
    }
}
