using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OnePlayer : MonoBehaviour
{

    public Player state;

    //replication network ID
    int wasID = -1;

    //previous health value to determine if need to tint damage
    int wasHealth = -1;

    //cached renderer
    MeshRenderer _renderer;

    //posision to constantly walk to
    public Vector3 contPos = Vector3.zero;

    //emulation walking speed
    float speed = 1;

    /// <summary>
    /// Updating each unit state after receiving info from server
    /// </summary>
    /// <param name="state"></param>
    /// <returns></returns>
    public void FillState(Player state)
    {
        this.state = state;
        
        if (_renderer == null)
        {
            _renderer = GetComponentInChildren<MeshRenderer>();
        }

        if (state.health <= 0)
        {
            _renderer.material.color = Color.black;
            transform.localPosition = new Vector3(transform.localPosition.x, 0, transform.localPosition.z);
        }
        else if (state.team == 0)
        {
            _renderer.material.color = Color.red;
        }
        else
        {
             _renderer.material.color = Color.blue;
        }

        if (state.health > 0)
        {
            if (wasID < 0)
            {
                transform.localPosition = new Vector3((state.xPos - 50) / 2, 0.5f, (state.yPos - 50) / 2);
            }
            else
            {
                contPos = new Vector3((state.xPos - 50) / 2, 0.5f, (state.yPos - 50) / 2);

            }
        }

        wasID = state.unitID;
        if (state.health <= 0) wasID = -1;

    }

    //Basically just moving to the point
    private void Update()
    {
        //discard if we are dead or in no simluation
        if (wasID < 0) return;

        //we constantly moving
        var end = contPos - transform.localPosition;
        
        if (end.magnitude < speed * Time.deltaTime)
        {
            transform.localPosition = contPos;
        }
        else
        {
            transform.localPosition += end.normalized * Time.deltaTime * speed;
        }
    }
}
