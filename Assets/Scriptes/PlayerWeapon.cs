using UnityEngine;

[System.Serializable]
public class PlayerWeapon {

    public string name = "Burst Gun";

    public int damage = 25;
    public float range = 100f;
    public int ammo = 24;

    public float fireRate = 0f;

    public GameObject graphics;
}
