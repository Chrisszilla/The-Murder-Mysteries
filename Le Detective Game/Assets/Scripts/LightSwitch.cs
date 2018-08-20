using UnityEngine;

public class LightSwitch : MonoBehaviour {

    public Light[] lights;
    public GameObject[] affectedDecals;

    public void SwitchLights()
    {
        foreach (Light light in lights)
        {
            light.enabled = !light.enabled;
        }
        foreach (GameObject decal in affectedDecals)
        {
            decal.SetActive(!lights[0].enabled);
        }
    }
}
