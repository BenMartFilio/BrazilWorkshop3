using System.Runtime.CompilerServices;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private Transform[] m_transforms;
    [SerializeField] private InputPlayerMovement m_inputManager;
    private Rigidbody2D rb;
    private int m_index;
    private int m_moveSpeed = 1;

    [SerializeField] private AudioEventDispatcher _AudioEventDispatcher;
    [SerializeField] private AudioType _MoveAudioType;

    private void OnEnable()
    {
        m_inputManager.OnMoveLeft += MoveToPreviousPosition;
        m_inputManager.OnMoveRight += MoveToNextPosition;
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        m_index = 1;
        UpdatePosition();
    }

    public void MoveToNextPosition()
    {
        _AudioEventDispatcher.PlayAudio(_MoveAudioType);
        m_index += m_moveSpeed;
        m_index = Mathf.Clamp(m_index, 0, m_transforms.Length - 1);
        UpdatePosition();
    }
    public void MoveToPreviousPosition()
    {
        _AudioEventDispatcher.PlayAudio(_MoveAudioType);
        m_index -= m_moveSpeed;
        m_index = Mathf.Clamp(m_index, 0, m_transforms.Length - 1);
        UpdatePosition();
    }
    public void MoveToDirection(int direction) //direction -1 ou 1
    {
        _AudioEventDispatcher.PlayAudio(_MoveAudioType);
        m_index += m_moveSpeed * direction;
        m_index = Mathf.Clamp(m_index, 0, m_transforms.Length - 1);
        UpdatePosition();
    }
    private void UpdatePosition()
    {
        transform.position = m_transforms[m_index].position;
        Quaternion actualRotation = transform.rotation;
        transform.rotation = actualRotation;
    }
}
