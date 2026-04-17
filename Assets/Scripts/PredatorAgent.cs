using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class PredatorAgent : Agent
{
    [Header("Agent Reference")]
    [SerializeField] private PreyAgent _preyAgent;

    [Header("Predator Agent Settings")]
    [SerializeField] private float _moveSpeed = 2.5f;
    [SerializeField] private float _rotationSpeed = 180f;

    [Header("Raycast Settings")]
    [SerializeField] private int _rayCount = 12;
    [SerializeField] private float _rayMaxDistance = 20f;
    [SerializeField] [Range(1f, 360f)] private float _fieldOfView = 180f;

    // Tag encoding: 0 = nothing, 1 = prey, 2 = wall
    private static readonly string[] _tagOrder = { "Prey", "Wall" };

    private Renderer _renderer;
    private Color _originalColor;

    [HideInInspector] public int CurrentEpisode = 0;
    [HideInInspector] public float CumulativeReward = 0f;

    public override void Initialize()
    {
        _renderer = GetComponent<Renderer>();
        _originalColor = _renderer.material.color;

        CurrentEpisode = 0;
        CumulativeReward = 0f;

        Debug.Log("PredatorAgent initialized.");
    }

    public override void OnEpisodeBegin()
    {
        CurrentEpisode++;
        CumulativeReward = 0f;

        _renderer.material.color = _originalColor;

        RandomizeSpawn();

        Debug.Log("PredatorAgent episode started.");
    }

    private void RandomizeSpawn()
    {
        float randomX = Random.Range(-8f, 8f);
        float randomZ = Random.Range(-8f, 8f);
        Vector3 spawnPos = new Vector3(randomX, 0.375f, randomZ);

        for (int i = 0; i < 10; i++)
        {
            if (Vector3.Distance(spawnPos, _preyAgent.transform.localPosition) >= 5f)
                break;

            randomX = Random.Range(-8f, 8f);
            randomZ = Random.Range(-8f, 8f);
            spawnPos = new Vector3(randomX, 0.375f, randomZ);
        }

        transform.localPosition = spawnPos;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        float angleStep = _rayCount > 1 ? _fieldOfView / (_rayCount - 1) : 0f;
        float startAngle = -_fieldOfView / 2f;

        for (int i = 0; i < _rayCount; i++)
        {
            float angle = startAngle + i * angleStep;
            Vector3 dir = Quaternion.Euler(0f, angle, 0f) * transform.forward;

            float normalizedDistance = 1f;
            int hitTagIndex = -1;

            if (Physics.Raycast(transform.position, dir, out RaycastHit hit, _rayMaxDistance))
            {
                normalizedDistance = hit.distance / _rayMaxDistance;
                for (int t = 0; t < _tagOrder.Length; t++)
                {
                    if (hit.collider.CompareTag(_tagOrder[t]))
                    {
                        hitTagIndex = t;
                        break;
                    }
                }
            }

            sensor.AddObservation(normalizedDistance);
            for (int t = 0; t < _tagOrder.Length; t++)
                sensor.AddObservation(hitTagIndex == t ? 1f : 0f);
        }

        sensor.AddObservation(transform.localPosition.x / 10f);
        sensor.AddObservation(transform.localPosition.z / 10f);
        sensor.AddObservation(transform.localEulerAngles.y / 360f * 2f - 1f);
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        float move = Mathf.Clamp(actionBuffers.ContinuousActions[0], 0f, 1f);
        float rotate = Mathf.Clamp(actionBuffers.ContinuousActions[1], -1f, 1f);

        transform.localPosition += move * _moveSpeed * Time.deltaTime * transform.forward;
        transform.Rotate(Vector3.up, rotate * _rotationSpeed * Time.deltaTime);

        // Time penalty to encourage faster catches
        AddReward(-0.5f / MaxStep);

        CumulativeReward = GetCumulativeReward();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Prey"))
        {
            PreyCaught();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Wall"))
        {
            AddReward(-0.1f);
            _renderer.material.color = Color.red;
        }

    }

    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag("Wall"))
        {
            AddReward(-0.05f * Time.fixedDeltaTime);
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Wall"))
        {
            _renderer.material.color = _originalColor;
        }
    }

    private void PreyCaught()
    {
        // Predator wins, prey loses
        SetReward(1f);
        _preyAgent.SetReward(-1f);

        _renderer.material.color = Color.green;
        CumulativeReward = GetCumulativeReward();

        _preyAgent.EndEpisode();
        EndEpisode();
    }

    private void OnDrawGizmosSelected()
    {
        float angleStep = _rayCount > 1 ? _fieldOfView / (_rayCount - 1) : 0f;
        float startAngle = -_fieldOfView / 2f;

        for (int i = 0; i < _rayCount; i++)
        {
            float angle = startAngle + i * angleStep;
            Vector3 dir = Quaternion.Euler(0f, angle, 0f) * transform.forward;

            if (Physics.Raycast(transform.position, dir, out RaycastHit hit, _rayMaxDistance))
            {
                if (hit.collider.CompareTag("Prey"))    Gizmos.color = Color.red;
                else                                    Gizmos.color = Color.yellow;

                Gizmos.DrawLine(transform.position, hit.point);
                Gizmos.DrawSphere(hit.point, 0.1f);
            }
            else
            {
                Gizmos.color = Color.grey;
                Gizmos.DrawLine(transform.position, transform.position + dir * _rayMaxDistance);
            }
        }
    }
}