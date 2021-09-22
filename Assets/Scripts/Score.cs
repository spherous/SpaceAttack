using UnityEngine;

public class Score : MonoBehaviour
{
    public int current {get; private set;}
    public delegate void OnScoreChanged(int newScore);
    public OnScoreChanged onScoreChanged;
    Player player;

    private void Awake() => player = GameObject.FindObjectOfType<Player>();

    public void Add(int val)
    {
        if(player.isDying || player.isDead)
            return;
        
        current += val;
        onScoreChanged?.Invoke(current);
    }
}