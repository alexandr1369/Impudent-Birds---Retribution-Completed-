using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cleaner : MonoBehaviour
{
    [SerializeField]
    private Animator animator;

    [SerializeField]
    private float idleCooldown;
    private float currentIdleCooldown = 0;

    private void Update()
    {
        if (currentIdleCooldown <= 0)
        {
            animator.SetTrigger("Idle");
            currentIdleCooldown = idleCooldown;
        }
        else
            currentIdleCooldown -= Time.deltaTime;
    }
    private void Sweep()
    {
        if (GameManager.instance.IsTutorial)
        {
            List<string> sweepSoundsNames = new List<string> { "Cleaner Sweep 1", "Cleaner Sweep 2" };
            AudioClip sweepSound = Resources.Load<AudioClip>("Sounds/" + sweepSoundsNames[Random.Range(0, sweepSoundsNames.Count)]);
            SoundManager.instance.PlaySingle(sweepSound);
        }
    }
}
