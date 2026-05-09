using UnityEngine;

public class GoalTrigger : MonoBehaviour
{
    [Tooltip("1 = Player1 scores, 2 = Player2 scores")]
    public int scoringPlayerId;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Ball"))
            GameManager.Instance?.RegisterGoal(scoringPlayerId);
    }
}
