using UnityEngine;

public class Util{

    public static void SetLayerRecursively(GameObject obj,int newLayer)
    {
        if (obj == null)
            return;
        obj.layer=newLayer;
        foreach (Transform child in obj.transform)
        {
            if (child == null)
            {
                continue;
            }
            else
            {
                SetLayerRecursively(child.gameObject, newLayer);
            }
        }
    }
	
}
