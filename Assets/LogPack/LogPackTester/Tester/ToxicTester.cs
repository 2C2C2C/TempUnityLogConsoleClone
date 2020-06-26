using UnityEngine;

public class ToxicTester : MonoBehaviour
{

    [Button("try exec public method")]
    public void ExecPublicMethod()
    {
        Debug.Log("try exec public method wa");
    }

    [Button("try exec protected method")]
    protected void ExecProtectedMethod()
    {
        Debug.Log("try exec protected method wa");
    }


    [Button("try exec private method")]
    private void ExecPrivateMethod()
    {
        Debug.Log("try exec private method wa");
    }

    [Button("try exec public static method")]
    public static void ExecPublicStaticMethod()
    {
        Debug.Log("try exec public static method wa");
    }

}
