using UnityEngine.UI;
using UnityEngine;

public class WorldConstructDebug : MonoBehaviour {
    public RawImage rwImage_worldConstruct;

    public void Update() {
        RenderTexture rt_worldConstruct = WorldConstructPass.resultRenderTexture;
        if(rt_worldConstruct != null ) {
            rwImage_worldConstruct.texture = rt_worldConstruct; 
        }
    }
   
}
