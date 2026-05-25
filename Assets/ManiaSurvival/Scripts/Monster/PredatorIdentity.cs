using UnityEngine;

public enum PredatorArchetype
{
    None,
    ShadowStalker,
    HellBrute,
    CycloneReaver,
    StoneMaw,
    RootHorror
}

public class PredatorIdentity : MonoBehaviour
{
    public PredatorArchetype archetype = PredatorArchetype.None;
}
