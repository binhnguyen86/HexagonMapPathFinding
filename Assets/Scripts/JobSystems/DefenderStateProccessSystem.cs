using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

public class DefenderStateProccessSystem : ComponentSystem
{
    EntityQuery query;
    protected override void OnCreate()
    {
        query = GetEntityQuery(typeof(ActorController), typeof(Defender));
    }

    protected override void OnUpdate()
    {
        Entities.With(query).ForEach((ActorController actor) =>
        {
            if ( actor == null || actor.IsDead )
            {
                return;
            }
            actor.UpdateAnim();
            actor.DamagedAnim();
        });
    }    
}