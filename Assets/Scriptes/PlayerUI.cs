using UnityEngine;

public class PlayerUI : MonoBehaviour
{

    [SerializeField]
    GameObject pauseMenu;

    //[SerializeField]
    //RectTransform staminaFill;

    //private UnityStandardAssets.Characters.FirstPerson.RigidbodyFirstPersonController controller;

    //public void SetController(UnityStandardAssets.Characters.FirstPerson.RigidbodyFirstPersonController _controller)
    //{
    //    controller = _controller;
    //}

    private void Start()
    {
        PauseMenu.IsOn = false;
    }

    void Update()
    {
       // SetStaminaAmount(controller.GetStaminaAmount());
       if(Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePauseMenu();
        }
    }

    void TogglePauseMenu()
    {
        pauseMenu.SetActive(!pauseMenu.activeSelf);
        PauseMenu.IsOn = pauseMenu.activeSelf;
    }

    //void SetStaminaAmount(float amount)
    //{
    //    staminaFill.localScale = new Vector3(amount, 1f, 1f);
    //}

}
