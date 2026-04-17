using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class PreyAgent : Agent
{
    [SerializeField] private Transform _food;
    [SerializeField] private PredatorAgent _predatorAgent;

    [Header("Prey Agent Settings")]
    [SerializeField] private float _moveSpeed = 2f;
    [SerializeField] private float _rotationSpeed = 180f;

    [Header("Raycast Settings")]
    [SerializeField] private int _rayCount = 12;    // rays per sweep
    [SerializeField] private float _rayMaxDistance = 20f;  // world-units
    [SerializeField] [Range(1f, 360f)] private float _fieldOfView = 180f; // degrees, centered on forward
    
    // Tag encoding: 0 = nothing, 1 = food, 2 = predator, 3 = wall
    private static readonly string[] _tagOrder = { "Food", "Predator", "Wall" };

    // To change the agent's color based on events
    private Renderer _renderer;
    private Color _originalColor;


    // Tracking variables for debugging
    [HideInInspector] public int CurrentEpisode = 0;
    [HideInInspector] public float CumulativeReward = 0f;


    public override void Initialize()
    {
        _renderer = GetComponent<Renderer>();
        _originalColor = _renderer.material.color;

        CurrentEpisode = 0;
        CumulativeReward = 0f;

        Debug.Log("PreyAgent initialized.");
    }

    public override void OnEpisodeBegin()
    {
        CurrentEpisode++;
        CumulativeReward = 0f;

        _renderer.material.color = _originalColor;

        SpawnFood();
        RandomizeSpawn();

        Debug.Log("PreyAgent episode started.");
    }

    private void RandomizeSpawn()
    {
        float randomX = Random.Range(-8f, 8f);
        float randomZ = Random.Range(-8f, 8f);

        transform.localPosition = new Vector3(randomX, 0.375f, randomZ);
    }

    private void SpawnFood()
    {
        Vector3 spawnPos;

        float randomX = Random.Range(-9f, 9f);
        float randomZ = Random.Range(-9f, 9f);
        spawnPos = new Vector3(randomX, 0.5f, randomZ);

        for (int i = 0; i < 10; i++)
        {
            if (Vector3.Distance(spawnPos, _predatorAgent.transform.localPosition) >= 5f)
                break;

            randomX = Random.Range(-9f, 9f);
            randomZ = Random.Range(-9f, 9f);
            spawnPos = new Vector3(randomX, 0.5f, randomZ);
        }

        _food.localPosition = spawnPos;
        Debug.Log("Food spawned at: " + _food.localPosition);
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        float angleStep  = _rayCount > 1 ? _fieldOfView / (_rayCount - 1) : 0f;
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
        float move   = Mathf.Clamp(actionBuffers.ContinuousActions[0], 0, 1f);
        float rotate = Mathf.Clamp(actionBuffers.ContinuousActions[1], -1f, 1f);

        transform.localPosition += move * _moveSpeed * Time.deltaTime * transform.forward;
        transform.Rotate(Vector3.up, rotate * _rotationSpeed * Time.deltaTime);

        // Time penalty to encourage faster food collection
        AddReward(-0.5f / MaxStep);

        // Update cumulative reward after each step
        CumulativeReward = GetCumulativeReward();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Food"))
        {
            //Food collected
            FoodReached();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Wall"))
        {
            // Penalty for colliding with walls
            AddReward(-0.1f);
            _renderer.material.color = Color.red;
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        if(collision.gameObject.CompareTag("Wall"))
        {
            // Additional penalty for staying in contact with walls
            AddReward(-0.05f * Time.fixedDeltaTime);
        }
    }

    private void OnCollisionExit(Collision collision)
    {
        if (collision.gameObject.CompareTag("Wall"))
        {
            // Reset color when leaving wall collision
            _renderer.material.color = _originalColor;
        }
    }

    private void FoodReached()
    {
        SetReward(1f);
        _predatorAgent.SetReward(-1f);

        _renderer.material.color = Color.green;
        CumulativeReward = GetCumulativeReward();

        _predatorAgent.EndEpisode();
        EndEpisode();
    }

    private void OnDrawGizmosSelected()
    {
        float angleStep  = _rayCount > 1 ? _fieldOfView / (_rayCount - 1) : 0f;
        float startAngle = -_fieldOfView / 2f;
        for (int i = 0; i < _rayCount; i++)
        {
            float angle = startAngle + i * angleStep;
            Vector3 dir = Quaternion.Euler(0f, angle, 0f) * transform.forward;

            if (Physics.Raycast(transform.position, dir, out RaycastHit hit, _rayMaxDistance))
            {
                // Color by tag: green = food, red = predator, yellow = other hit
                if (hit.collider.CompareTag("Food")) Gizmos.color = Color.green;
                else if (hit.collider.CompareTag("Predator")) Gizmos.color = Color.red;
                else Gizmos.color = Color.yellow;

                Gizmos.DrawLine(transform.position, hit.point);
                Gizmos.DrawSphere(hit.point, 0.1f);
            }
            else
            {
                // No hit = grey line 
                Gizmos.color = Color.grey;
                Gizmos.DrawLine(transform.position, transform.position + dir * _rayMaxDistance);
            }
        }
    }

}
