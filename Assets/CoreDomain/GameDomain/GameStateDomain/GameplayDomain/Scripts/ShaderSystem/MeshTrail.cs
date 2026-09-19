using UnityEngine;
using System.Collections;

public class MeshTrail : MonoBehaviour
{

    [SerializeField] private int meshRate;
    [SerializeField] private Mesh trailMesh;

    public float activeTime = .5f;

    //isso aqui � pra mesh
    private bool isTrailActive;
    public float meshDestroyDelay = 3f;

    public float meshRefreshRate;
    public Transform positionToSpawn;

    //isso � pra shader

    public Material mat;
    public string shaderVarRef;
    //public float shaderVarRate = 0.1f;
    //public float shaderVarRefreshRate = 0.05f;
    public void StartTrail(){
        if(!isTrailActive)
            StartCoroutine(ActiveTrail(activeTime));
    }

    public IEnumerator ActiveTrail(float timeActive){
        
        isTrailActive = true;

        for(int i = 0; i < meshRate; i++){

        GameObject gObj = new GameObject();
        gObj.transform.SetLocalPositionAndRotation(positionToSpawn.position, positionToSpawn.rotation);

        MeshRenderer mr = gObj.AddComponent<MeshRenderer>();
        MeshFilter mf = gObj.AddComponent<MeshFilter>();
        
        mf.mesh = trailMesh;
        mr.material = mat;
        //StartCoroutine(AnimatedMaterialFloat(mr.material, 0, shaderVarRate, shaderVarRefreshRate));
        Destroy(gObj, meshDestroyDelay); 
         yield return new WaitForFixedUpdate();
        }

        isTrailActive = false;
        
    }

    IEnumerator AnimatedMaterialFloat(Material mat, float goal, float rate, float refreshRate)
    {
        float valueToAnimate = mat.GetFloat(shaderVarRef);

        while (valueToAnimate > goal)
        {
            valueToAnimate -= rate;
            mat.SetFloat(shaderVarRef, valueToAnimate);
            yield return new WaitForSeconds(refreshRate);
        }
    }
}
