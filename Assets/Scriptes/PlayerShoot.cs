using UnityEngine;
using UnityEngine.Networking;

[RequireComponent(typeof(WeaponManager))]
public class PlayerShoot : NetworkBehaviour {

    private const string PLAYER_TAG = "Player";
    
    [SerializeField]
    private Camera cam;


    [SerializeField]
    private LayerMask mask;

    public PlayerWeapon currentWeapon;
    private WeaponManager weaponManager;
	void Start () {
		if(cam==null)
        {
            Debug.LogError("PlayerShoot: No Camera Present");
            this.enabled = false;
        }

        weaponManager = GetComponent<WeaponManager>();
	}

    void Update()
    {
        currentWeapon = weaponManager.GetCurrentWeapon();

        if (PauseMenu.IsOn)
            return;

        if(currentWeapon.fireRate<=0f)
        {
            if (Input.GetButtonDown("Fire1"))
            {
                Shoot();
            }
        }
        else
        {
            if(Input.GetButtonDown("Fire1"))
            {
                InvokeRepeating("Shoot", 0f, 0.8125f/currentWeapon.fireRate);
            }
            else if (Input.GetButtonUp("Fire1"))
            {
                CancelInvoke("Shoot");
            }
        }

        
    }

    [Command]
    void CmdOnShoot()
    {
        RpcDoShootEffect();
    }

    [ClientRpc]
    void RpcDoShootEffect()
    {
        weaponManager.GetCurrentGraphics().muzzleFlash.Play();
    }

    [Command]
    void CmdonHit(Vector3 pos,Vector3 normal)
    {
        RpcDoHitEffect(pos, normal);
    }

    [ClientRpc]
    void RpcDoHitEffect(Vector3 pos,Vector3 normal)
    {
        GameObject hitEffect= Instantiate(weaponManager.GetCurrentGraphics().hitEffectPrefab, pos, Quaternion.LookRotation(normal));
        Destroy(hitEffect, 2f);
    }

    [Client]
    void Shoot()
    {
        if (!isLocalPlayer)
        {
            return;
        }

        CmdOnShoot();

        RaycastHit hit;
        if(Physics.Raycast(cam.transform.position, cam.transform.forward, out hit, currentWeapon.range,mask))
        {
            if(hit.collider.tag==PLAYER_TAG)
            {
                CmdPlayerShot(hit.collider.name, currentWeapon.damage);
            }

            CmdonHit(hit.point, hit.normal);
        }

    }

    [Command]
    void CmdPlayerShot(string playerID,int damage)
    {
        Debug.Log(playerID + " has been shot");

        Player player=GameManager.GetPlayer(playerID);
        player.RpcTakeDamage(damage);
    }
}
