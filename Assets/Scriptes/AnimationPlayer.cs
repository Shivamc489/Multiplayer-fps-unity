using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationPlayer : MonoBehaviour {

	Animator player;
    float h, v,sprint,shoot;

	void Start () {

		player = GetComponent<Animator> ();

	}

    private void Update()
    {
        
        v = Input.GetAxis("Vertical");
        h = Input.GetAxis("Horizontal");
        Sprinting();
        Shooting();
    }

    private void FixedUpdate()
    {
        if (PauseMenu.IsOn)
            return;
        v = Input.GetAxis("Vertical");
        h = Input.GetAxis("Horizontal");
        Sprinting();
        Shooting();
        if (!Input.GetKey(KeyCode.LeftControl))
        {
            player.SetFloat("crouch", 0f);
            player.SetFloat("walk", v);
            player.SetFloat("turn", h);
            player.SetFloat("run", sprint);
            player.SetFloat("shoot", shoot);
        }
        else
        {
            player.SetFloat("crouch", 1f);
            player.SetFloat("walk", v);
            player.SetFloat("turn", h);
        }

        if (Input.GetKeyDown(KeyCode.RightAlt))
        {
            int a = Random.Range(0,6);
            player.SetInteger("death", a);
        }

        if (Input.GetKeyDown(KeyCode.R))
        {
            player.SetFloat("reload", 0.2f);
        }
        else
        {
            player.SetFloat("reload", 0f);
        }
    }

    void Sprinting()
    {
        if(Input.GetKey(KeyCode.LeftShift)&&v>0)
        {
            sprint = 0.2f;
        }
        else if(Input.GetKey(KeyCode.LeftShift) && v < 0)
        {
            sprint = -0.2f;
        }
        else
        {
            sprint = 0f;
        }
    }

    void Shooting()
    {
        if (Input.GetKey(KeyCode.Mouse0))
        {
            shoot = 10.5f;
        }
        else
        {
            shoot = 0f;
        }
    }
}
