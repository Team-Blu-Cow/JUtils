using UnityEngine;
using UnityEngine.InputSystem;
using System.Threading.Tasks;
using blu;

// had to change this due to a conflict with JUtils
public class MainTestScript : MonoBehaviour
{
    private bool _paused;

    #region Level Loading

    public void LoadLevel(string in_scene)
    {
        if (Random.value < 0.5f)
        {
            App.GetModule<SceneModule>().SwitchScene(in_scene, TransitionType.LRSweep);
            App.GetModule<SceneModule>().SwitchScene(in_scene, TransitionType.LRSweep);
        }
        else
        {
            App.GetModule<SceneModule>().SwitchScene(in_scene, TransitionType.Fade);
        }
    }

    public void LoadLevel(InputAction.CallbackContext context)
    {
        if (Random.value < 0.5f)
        {
            App.GetModule<SceneModule>().SwitchScene("Level02", TransitionType.LRSweep, LoadingBarType.BottomRightRadial);
        }
        else
        {
            App.GetModule<SceneModule>().SwitchScene("Level02", TransitionType.Fade, LoadingBarType.BottomRightRadial);
        }
    }

    #endregion Level Loading

    private void PauseGame(InputAction.CallbackContext context)
    {
        _paused = !_paused;

        if (_paused)
        {
            Time.timeScale = 0f;
            App.CanvasManager.AddCanvas(Instantiate(Resources.Load<GameObject>("prefabs/PauseCanvas")));
        }
        else
        {
            Time.timeScale = 1f;
            App.CanvasManager.RemoveCanvasContainer("PauseCanvas(Clone)");
        }
    }

    private void OnDestroy()
    {
        App.GetModule<blu.InputModule>().PlayerController.Move.Direction.started -= LoadLevel;
        App.GetModule<blu.InputModule>().SystemController.UI.Pause.performed -= PauseGame;
    }

    // Start is called before the first frame update
    private void Start()
    {
        App.GetModule<blu.InputModule>().PlayerController.Move.Direction.started += LoadLevel;
        //App.ModuleManager.GetModule<blu.InputModule>().SystemController.UI.Pause.performed += InputDebug;
        App.GetModule<blu.InputModule>().SystemController.UI.Pause.performed += PauseGame;

        if (App.GetModule<IOModule>().SaveSlots[0] != null)
        {
            App.GetModule<IOModule>().LoadSave(App.GetModule<IOModule>().SaveSlots[0]);
        }
        else
        {
            App.GetModule<IOModule>().CreateNewSave(0, true);
        }
    }

    // Update is called once per frame
    private void Update()
    {
        if (Keyboard.current.oKey.wasPressedThisFrame)
        {
            App.RemoveModule<SettingsModule>();
        }

        if (Keyboard.current.pKey.wasPressedThisFrame)
        {
            App.AddModule<SettingsModule>();
        }
    }
}