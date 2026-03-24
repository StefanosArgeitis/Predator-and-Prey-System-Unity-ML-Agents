using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class PreyAgent : Agent
{
    [SerializeField] private Transform _food;

    [Header("Prey Agent Settings")]
    [SerializeField] private float _moveSpeed = 2f;
    [SerializeField] private float _rotationSpeed = 180f;

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
        Debug.Log("PreyAgent episode started.");
    }

    private void SpawnFood()
    {
        transform.localRotation = Quaternion.identity;
        transform.localPosition = new Vector3(0f, 0.5f, 0f);

        // Randomomizing food position around the agent (angle)
        float randomAngle = Random.Range(0f, 360f);
        Vector3 randomDirection = Quaternion.Euler(0f, randomAngle, 0f) * Vector3.forward;

        Debug.Log(randomAngle);
        Debug.Log(randomDirection);

        // Randomomizing food position around the agent (distance)
        float randomDistance = Random.Range(1f, 5f);

        Debug.Log(randomDistance);

        // Calculate the food position based on the random direction and distance
        Vector3 foodPosition = transform.localPosition + randomDirection * randomDistance;
        _food.localPosition = new Vector3(foodPosition.x, 0.5f, foodPosition.z);

        Debug.Log("Food spawned at: " + _food.localPosition);
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // Food Position
        float foodPosX_normalized = _food.localPosition.x / 10f;
        float foodPosZ_normalized = _food.localPosition.z / 10f;

        // Prey Agent Position
        float preyAgentPosX_normalized = transform.localPosition.x / 10f;
        float preyAgentPosZ_normalized = transform.localPosition.z / 10f;

        // Prey Agent Rotation (Y-axis)
        float preyAgentRotY_normalized = (transform.localEulerAngles.y / 360f) * 2f - 1f;

        // Add observations to the sensor
        sensor.AddObservation(foodPosX_normalized);
        sensor.AddObservation(foodPosZ_normalized);

        sensor.AddObservation(preyAgentPosX_normalized);
        sensor.AddObservation(preyAgentPosZ_normalized);
        sensor.AddObservation(preyAgentRotY_normalized);
    }

    public override void OnActionReceived(ActionBuffers actionBuffers)
    {
        // Discrete actions: [0] = Move Forward, [1] = Rotate Left, [2] = Rotate Right
        MoveAgent(actionBuffers.DiscreteActions);

        // Time penalty to encourage faster food collection
        AddReward(-2f / MaxStep);

        // Update cumulative reward after each step
        CumulativeReward = GetCumulativeReward();
    }

    public void MoveAgent(ActionSegment<int> discreteActions)
    {
        int moveAction = discreteActions[0];

        // Move Forward
        if (moveAction == 1)
        {
            transform.localPosition += transform.forward * _moveSpeed * Time.deltaTime;
        }
        // Rotate Left
        if (moveAction == 2)
        {
            transform.Rotate(Vector3.up, -_rotationSpeed * Time.deltaTime);
        }
        // Rotate Right
        if (moveAction == 3)
        {
            transform.Rotate(Vector3.up, _rotationSpeed * Time.deltaTime);
        }
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
            AddReward(-0.05f * Time.deltaTime);
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
        // Reward for collecting food
        AddReward(1f);
        _renderer.material.color = Color.green;
        CumulativeReward = GetCumulativeReward();

        //End the episode after collecting food
        EndEpisode();
    }

}
