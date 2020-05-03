
using UnityEngine;

public class LogTester01 : MonoBehaviour
{
    [Button("default log normal")]
    public void DefaultLogNormal()
    {
        // w
        Debug.Log("default log normal");
    }

    [Button("default log warnning")]
    public void DefaultLogWarning()
    {
        //wa
        Debug.LogWarning("default log warning wa");
    }

    [Button("default log error")]
    public void DefaultLogError()
    {
        // wawa
        Debug.LogError("default log error wwa");
    }

}


public class TempWaClass
{

    
}
