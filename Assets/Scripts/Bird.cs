using Unity.VisualScripting;
using UnityEngine;

public class Bird : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] float PositionLerpSpeed = 5f;
    [SerializeField] float PredictionMultiplier = 1f;
    [SerializeField] float MaxDistance = 5f;

    Vector2 _serverPos;
    Vector2 _serverVel;

    void Update()
    {
        if (_serverPos == null || _serverVel == null) return;

        Vector2 predictedPos = _serverPos + _serverVel * PredictionMultiplier * Time.deltaTime;

        Vector2 delta = new Vector2(transform.position.x, transform.position.z) - _serverPos;
        if (delta.sqrMagnitude > MaxDistance*MaxDistance)
        {
            transform.position = new Vector3(_serverPos.x, transform.position.y, _serverPos.y);
            return;
        }

        Vector3 newPos = Vector3.Lerp(
            transform.position,
            new Vector3(predictedPos.x, transform.position.y, predictedPos.y),
            Time.deltaTime * PositionLerpSpeed
        );

        transform.position = newPos;
    }

    public void UpdateFromServer(Vector2 pos, Vector2 vel)
    {
        _serverPos = pos;
        _serverVel = vel;
    }
}
