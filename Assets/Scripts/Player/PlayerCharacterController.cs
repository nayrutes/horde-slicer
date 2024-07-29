using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCharacterController : MonoBehaviour
{
    [SerializeField]
    private float _moveSpeed;


    private Vector2 _inputVec;
    public void OnMove(InputAction.CallbackContext ctx)
    {
        _inputVec = ctx.ReadValue<Vector2>();
    }

    private void Update()
    {
        Vector3 moveVec = new Vector3(_inputVec.x, 0, _inputVec.y).normalized;
        transform.position += moveVec * (Time.deltaTime * _moveSpeed);
    }
}
