using UnityEngine;
using UnityEngine.Networking;
public class WeaponManager : NetworkBehaviour {

    [SerializeField]
    private string weaponLayerName = "weapon";

    [SerializeField]
    private Transform weaponHolder;

    [SerializeField]
    private PlayerWeapon primaryWeapon;

    private PlayerWeapon currentWeapon;
    private WeaponGraphics currentGraphics;

    void Start()
    {
        EquipWeapon(primaryWeapon);
    }

    public PlayerWeapon GetCurrentWeapon()
    {
        return currentWeapon;
    }
    public WeaponGraphics GetCurrentGraphics()
    {
        return currentGraphics;
    }


    void EquipWeapon(PlayerWeapon weapon)
    {
        currentWeapon = weapon;

       GameObject weaponIns=(GameObject) Instantiate(weapon.graphics, weaponHolder.position, weaponHolder.rotation);
        weaponIns.transform.SetParent(weaponHolder);

        currentGraphics = weaponIns.GetComponent<WeaponGraphics>();
        if(currentGraphics==null)
        {
            Debug.LogError("No WeaponGraphics component on the weapon object: " + weaponIns.name);
        }

        if (isLocalPlayer)
        {
            Util.SetLayerRecursively(weaponIns, LayerMask.NameToLayer(weaponLayerName));
        }

    }
}
