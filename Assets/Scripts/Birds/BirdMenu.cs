using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BirdMenu : MonoBehaviour
{
    [SerializeField]
    private Animator animator; // main animator

    [SerializeField]
    private ToggleBirdDanceButton btnTbd; // dance toggle button

    private void Start()
    {
        // get all dance trigger names
        string danceName = string.Empty;
        string[] danceNames = new string[] { "Dance1", "Dance2", "Dance3" };

        // toggle unique dance
        danceName = danceNames[Random.Range(0, danceNames.Length)];
        animator.SetTrigger(danceName);

        // bird dance toggle button utils
        btnTbd.SetCurrentDanceName(danceName);
    }
}
