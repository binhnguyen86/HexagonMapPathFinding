using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public class HexNodeColorProccessSystem : ComponentSystem
{
    EntityQuery query;
    protected override void OnCreate()
    {
        query = GetEntityQuery(typeof(SpriteRenderer));
    }

    protected override void OnUpdate()
    {
#if UNITY_EDITOR
        Entities.With(query).ForEach((SpriteRenderer sprite) =>
        {
            sprite.color = Color.white;
            if (HexagonMapGenerator.Instance.CurrentPath.Contains(sprite.transform.position) )
            {
                sprite.color = Color.red;
            }
        });
#endif
    }

}