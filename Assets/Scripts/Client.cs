using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Client : MonoBehaviour
{
    public Button btnStart;

    public GameObject playerPrefab;

    public Transform fieldContainer;


    //connected server
    public Server server;

    //Adding listener to restart silmulation
    private void Start()
    {
        btnStart.onClick.AddListener(PressStart);
    }

    /// <summary>
    /// Receive server state
    /// </summary>
    public void ReceiveState(GameState state)
    {
        if (state.state == States.Finished)
        {
            btnStart.gameObject.SetActive(true);
        }
        else
        {
            btnStart.gameObject.SetActive(false);
        }

        //
        foreach (var plr in state.players)
        {
            SpawnAndSet(plr);
        }
    }

    /// <summary>
    /// Finding unit with ID, if no just spawn it
    /// units are put in object pool initially
    /// </summary>
    public void SpawnAndSet(Player plr)
    {
        bool check = false;
        OnePlayer player = null;

        for (int i = 0; i < fieldContainer.childCount; i++)
        {
            var onePlayer = fieldContainer.GetChild(i).GetComponent<OnePlayer>();
            if (onePlayer.state.unitID == plr.unitID)
            {
                check = true;
                player = onePlayer;
                break;
            }
        }

        if (!check)
        {
            var op = (GameObject)Instantiate(playerPrefab, fieldContainer);
            player = op.GetComponent<OnePlayer>();
        }

        player.FillState(plr);

    }


    /// <summary>
    /// Simulation starts
    /// </summary>
    public void PressStart()
    {
        ClearAll();

        server.StartPressed();
    }

    /// <summary>
    /// Clear Field
    /// </summary>
    public void ClearAll()
    {
        for (int i = fieldContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(fieldContainer.GetChild(i).gameObject);
        }
    }


}
