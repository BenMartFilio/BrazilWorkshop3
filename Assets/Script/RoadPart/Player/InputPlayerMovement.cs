using System;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class InputPlayerMovement : MonoBehaviour
{
    private Vector2 startPos;
    private float minDistance = 50f;
    public event Action OnMoveLeft;
    public event Action OnMoveRight;
    public event Action OnTapScreen;

    private void OnEnable()
    {
        EnhancedTouchSupport.Enable();
        Touch.onFingerDown += OnFingerDown;
        Touch.onFingerUp += OnFingerUp;
    }

    private void OnDisable()
    {
        Touch.onFingerDown -= OnFingerDown;
        Touch.onFingerUp -= OnFingerUp;
        EnhancedTouchSupport.Disable();
    }

    private void OnFingerDown(Finger finger)
    {
        startPos = finger.screenPosition;
    }

    private void OnFingerUp(Finger finger)
    {
        Vector2 endPos = finger.screenPosition;
        Vector2 delta = endPos - startPos;

        if (delta.magnitude < minDistance)
        {
            TapScreen();
            return;
        }

        if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
        {
            bool right = delta.x > 0;

            if (right) MoveRight();
            else MoveLeft();

        }
        else
        {
            Debug.Log(delta.y > 0 ? "Swipe Up" : "Swipe Down");
        }
    }


    public void MoveLeft()
    {
        OnMoveLeft?.Invoke();
    }
    public void MoveRight()
    {
        OnMoveRight?.Invoke();
    }
    public void TapScreen()
    {
        OnTapScreen?.Invoke();
    }
}

