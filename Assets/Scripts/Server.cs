using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Server : MonoBehaviour
{
    #region SimulationParams
    public const int N = 100;
    public const int M = 100;

    public const int unitsMin = 150;
    public const int unitsMax = 250;

    public const int healthMin = 2;
    public const int healthMax = 5;

    public const float attackRange = 3;
    #endregion

    //last object simulation id
    public static int s_lastID = 0;

    [SerializeField]
    private GameState _gameState;
    // Start is called before the first frame update

    //connected client
    public Client client;

    //randomize actions
    public static System.Random rng = new System.Random();
    void Start()
    {
        InvokeRepeating("HandleGame", 0, 1f);
    }
    
    /// <summary>
    /// Handling game state
    /// </summary>
    public void HandleGame()
    {
        if (_gameState.state == States.NotStarted)
        {
            _gameState.InitField();
        }
        else
        {
            bool isEnd = _gameState.CheckEnd();
            
            if (isEnd)
            {
                _gameState.state = States.Finished;
            }
            else
            {
                //simulation step
                _gameState.SimulationStep();
            }
        }

        client.ReceiveState(_gameState);
    }

    #region ClientEvents
    public void StartPressed()
    {
        _gameState.state = States.NotStarted;
    }

    #endregion

}

[System.Serializable]
public class GameState
{
    //current game state
    public States state;

    //all current players
    public List<Player> players = new List<Player>();

    /// <summary>
    /// Checking if the field is occupied
    /// </summary>
    /// <param name="xTest"></param>
    /// <param name="yTest"></param>
    /// <returns></returns>
    public bool CheckPosition(byte xTest, byte yTest)
    {
        var check = players.Find(x => x.xPos == xTest && x.yPos == yTest && x.wasHealth > 0);
        
        if (check == null)
        {
            return false;
        }
        else
        {
            return true;
        }
    }

    /// <summary>
    /// Simulation is ended ?
    /// </summary>
    /// <returns></returns>
    public bool CheckEnd()
    {
        int redTeam = 0;
        int blueTeam = 0;
        foreach (var plr in players)
        {
            if (plr.team == 0 && plr.health > 0) redTeam++;
            else if (plr.team == 1 && plr.health > 0) blueTeam++;
        }

        if (redTeam == 0 || blueTeam == 0) return true;
        else return false;
    }

    public void InitField()
    {
        players.Clear();
        int unitsNum = Random.Range(Server.unitsMin, Server.unitsMax);

        for (int i = 0; i < unitsNum; i++)
        {
            var xR = Random.Range(0, Server.N);
            var yR = Random.Range(0, Server.M);

            if (!CheckPosition((byte)xR, (byte)yR))
            {
                //should be put grouped into the constructor instead
                //just for the simplicity
                Player player = new Player();
                
                player.unitID = Server.s_lastID++;
                player.xPos = (byte)xR;
                player.yPos = (byte)yR;
                player.health = (sbyte)Random.Range(Server.healthMin, Server.healthMax);
                player.wasHealth = player.health;
                player.team = (byte)Random.Range(0, 2);

                players.Add(player);
            }
            else
            {
                //going for another round
                //whem unitsMax / (N * M) ratio is low its good solution
                i--;
            }
        }

        state = States.InProgress;
    }

    public void SimulationStep()
    {

        foreach (var plr in players)
        {
            plr.acted = 0;
            plr.attackingID = -1;
            plr.wasHealth = plr.health;
        }

        //to make it more interesting we shuffle players
        players = players.OrderBy(a => Server.rng.Next()).ToList();

        //first we check units in range to continue attack
        for (int i = 0; i < players.Count; i++)
        {
            if (players[i].wasHealth <= 0 || players[i].acted > 0) continue;

            //trying to find who to attack
            byte opTeam = (byte)((players[i].team + 1) % 2);

            int opponent = -1;
            for (int j = i + 1; j < players.Count; j++)
            {
                if (players[j].wasHealth <= 0 || players[j].team != opTeam) continue;
                if (Mathf.Abs(players[i].xPos - players[j].xPos) > Server.attackRange) continue;
                if (Mathf.Abs(players[i].yPos - players[j].yPos) > Server.attackRange) continue;

                //checking attacking distance
                var dist = Mathf.Sqrt( (players[i].xPos - players[j].xPos) * (players[i].xPos - players[j].xPos) 
                    + (players[i].yPos - players[j].yPos) * (players[i].yPos - players[j].yPos));

                if (dist > Server.attackRange) continue;

                opponent = j;
                break;
            }

            if (opponent >= 0)
            {
                players[i].acted = 1;
                players[i].attackingID = players[opponent].unitID;
                players[opponent].health -= 1;

                //return strike is not acted opponent
                if (players[opponent].acted == 0)
                {
                    players[opponent].acted = 1;
                    players[opponent].attackingID = players[i].unitID;
                    players[i].health -= 1;
                }
            }
            else
            {
                //we move
                //we find our target with minimal distance
                opponent = -1;
                float min = 1e+10f;
                for (int j = 0; j < players.Count; j++)
                {
                    if (players[j].wasHealth <= 0 || players[j].team != opTeam) continue;

                    //checking attacking distance
                    var dist = Mathf.Sqrt((players[i].xPos - players[j].xPos) * (players[i].xPos - players[j].xPos)
                        + (players[i].yPos - players[j].yPos) * (players[i].yPos - players[j].yPos));

                    if (dist < min)
                    {
                        min = dist;
                        opponent = j;
                    }
                    
                }

                if (opponent < 0)
                {
                    //means the simulation will end in one turn
                    players[i].acted = 1;
                    
                }
                else
                {
                    //MANHATTAN distance algorithm
                    //no Dijkstra needed

                    //we check how to move
                    //its manhattan grid, so no complex algo is needed
                    int rand = 0;
                    if (players[i].xPos != players[opponent].xPos && players[i].yPos != players[opponent].yPos)
                    {
                        rand = Random.Range(0, 2) + 1;
                    }

                    if (players[i].xPos > players[opponent].xPos && !CheckPosition((byte)(players[i].xPos-1), players[i].yPos))
                    {
                        players[i].xPos--;
                    }
                    else if (players[i].xPos < players[opponent].xPos && !CheckPosition((byte)(players[i].xPos+1), players[i].yPos))
                    {
                        players[i].xPos++;
                    }
                    else
                    {
                        //its equal
                        //checking y
                        if (players[i].yPos > players[opponent].yPos && !CheckPosition(players[i].xPos, (byte)(players[i].yPos-1)))
                        {
                            players[i].yPos--;
                        }
                        else if (players[i].yPos < players[opponent].yPos && !CheckPosition(players[i].xPos, (byte)(players[i].yPos+1)))
                        {
                            players[i].yPos++;                           
                        }
                    }
                    players[i].acted = 1;

                }
            }
        }
    }
}

[System.Serializable]
public class Player
{
    //basically network object id
    public int unitID = -1;
    
    //encoding variables to minimize packet size sending over network
    public byte xPos = 0;
    public byte yPos = 0;
    
    //team 0 - red, 1 - blue
    public byte team = 0;
    
    //health is random between 2 and 5; 0 means player is dead
    public sbyte health;
    //cause all units act in the same turn and to avoid the situation when one kills another earlier
    public sbyte wasHealth;

    //was the player acted this frame
    public byte acted = 0;

    //were we attacking last turn, if yes, playerid
    public int attackingID = -1;
}

[System.Serializable]
public enum States
{
    NotStarted,
    InProgress,
    Paused,
    Finished
}