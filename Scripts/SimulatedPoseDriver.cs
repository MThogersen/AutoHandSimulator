using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.XR.Interaction;

public class SimulatedPoseDriver : BasePoseProvider
{
    [System.Obsolete]
    public override bool TryGetPoseFromProvider(out Pose output)
    {
        output = new Pose(transform.position, transform.rotation);
        return true;
    }
}
