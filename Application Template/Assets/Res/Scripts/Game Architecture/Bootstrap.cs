using System;
using UnityEngine;

public class Bootstrap : MonoBehaviour
{
    public class BootstrapException : ApplicationException
    {
        public BootstrapException()
        {
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void loadApplication()
    {
        GameObject app = GameObject.Instantiate(new GameObject());
        if (app == null)
        {
            throw new BootstrapException();
        }

        blu.App component = app.AddComponent<blu.App>();
        if (component == null)
        {
            throw new BootstrapException();
        }

        DontDestroyOnLoad(app);
    }
}