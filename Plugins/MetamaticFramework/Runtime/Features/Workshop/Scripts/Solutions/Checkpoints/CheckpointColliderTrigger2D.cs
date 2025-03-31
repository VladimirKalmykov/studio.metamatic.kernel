using System;
using UnityEngine;

namespace Metamatic.Framework.Workshop
{
    /// <summary>
    /// Triggers checkpoint when player enters the collider (Collider must be trigger).
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class CheckpointColliderTrigger2D: CheckpointTrigger
    {
        private void OnEnable()
        {
            GetComponent<Collider2D>().isTrigger = true;
            GetComponent<Collider2D>().enabled = true;
        }

        private void OnDisable()
        {
            GetComponent<Collider2D>().enabled = false;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (PlayerSystem.IsPlayer(other.gameObject, out var entity))
            {
                Checkpoint.Trigger(entity);
            }
        }
    }
}