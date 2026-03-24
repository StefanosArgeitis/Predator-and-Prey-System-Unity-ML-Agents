using UnityEngine;

public class GUI_PreyAgent : MonoBehaviour
{
    [SerializeField] private PreyAgent preyAgent;

    private GUIStyle _defaultStyle = new GUIStyle();
    private GUIStyle _positiveStyle = new GUIStyle();
    private GUIStyle _negativeStyle = new GUIStyle();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _defaultStyle.fontSize = 20;
        _defaultStyle.normal.textColor = Color.white;

        _positiveStyle.fontSize = 20;
        _positiveStyle.normal.textColor = Color.green;

        _negativeStyle.fontSize = 20;
        _negativeStyle.normal.textColor = Color.red;
    }

    private void OnGUI()
    {
        string debugEpisode = "Episode: " + preyAgent.CurrentEpisode + " - Step: " + preyAgent.StepCount;
        string debugReward = "Cumulative Reward: " + preyAgent.CumulativeReward.ToString("F2");

        GUIStyle rewardStyle = preyAgent.CumulativeReward > 0 ? _positiveStyle : _negativeStyle;

        GUI.Label(new Rect(10, 10, 300, 30), debugEpisode, _defaultStyle);
        GUI.Label(new Rect(10, 40, 300, 30), debugReward, rewardStyle);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
