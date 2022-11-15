using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Flags]
public enum CollisionLayers
{
    Selection = 1 << 0,
    Ground = 1 <<1
}
