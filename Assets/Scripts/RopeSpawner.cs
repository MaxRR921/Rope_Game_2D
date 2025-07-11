using UnityEngine;

public class RopeSpawner : MonoBehaviour
{
    public RopeGPU ropePrefab;        // assign your prefab here
    public Transform[] pinTargets;    // optional objects the rope should pin to

    void Start()
    {
        // example: spawn three ropes in a row
        for (int i = 0; i < 3; i++)
        {
            var rope = Instantiate(ropePrefab,
                                    new Vector3(i * 0.4f, 0, 0),
                                    Quaternion.identity);

            // customise per instance
            rope.thickness = 0.02f + 0.01f * i;
            rope.ropeColor = Color.Lerp(Color.white, Color.red, i / 2f);

            // pin the first point to a target, if you want
            if (pinTargets.Length > i && pinTargets[i] != null)
                rope.pinnedObjs.Add(pinTargets[i].gameObject);
        }
    }
}
