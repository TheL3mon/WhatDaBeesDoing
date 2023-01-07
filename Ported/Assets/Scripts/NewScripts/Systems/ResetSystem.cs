using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using UnityEngine.SceneManagement;

public partial class ResetSystem : SystemBase
{
    protected override void OnUpdate()
    {
        if (Input.GetKeyDown(KeyCode.W))
        {
            World.EntityManager.CompleteAllJobs();
            World.Dispose();
            DefaultWorldInitialization.Initialize("Default World");
            //var EM = Unity.Entities.World.DefaultGameObjectInjectionWorld.EntityManager;
            //EM.DestroyEntity(EM.UniversalQuery);
            SceneManager.LoadScene("CombatBeesECS", LoadSceneMode.Single);
        }
    }


}