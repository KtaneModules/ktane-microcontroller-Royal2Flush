using UnityEngine;
using System.Collections;

public class buttonAnim : MonoBehaviour {

    public void press()
    {
        GetComponent<Animation>().Play();
    }

}
