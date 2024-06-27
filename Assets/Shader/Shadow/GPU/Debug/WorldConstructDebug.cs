using UnityEngine.UI;
using UnityEngine;

public class WorldConstructDebug : MonoBehaviour {
    public RawImage rwImage_worldConstruct;
    public RawImage rwImage_shadowConstruct;

    public void Update() {
        RenderTexture rt_worldConstruct = WorldConstructPass.resultWorldPositionRenderTexture;
        if(rt_worldConstruct != null) {
            rwImage_worldConstruct.texture = rt_worldConstruct; 
        }

        RenderTexture rt_shadowConstruct = WorldConstructPass.resultShadowRenderTexture;
        if(rt_shadowConstruct != null) {
            rwImage_shadowConstruct.texture = rt_shadowConstruct; 
        }
    }
   
}
