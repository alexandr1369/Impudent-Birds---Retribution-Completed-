using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Pixelplacement;

public class LifesTrackController : MonoBehaviour
{
    // start state
    private bool isStarted = false;

    // lifes track animator
    [SerializeField]
    private Animator animator;

    // lifes track material (visual moving shader)
    [SerializeField]
    private Material lifesTrackMaterial;

    // 3d lifes models
    [SerializeField]
    private GameObject lifePrefab;
    private List<Transform> lifes;

    // spawn utils
    [SerializeField]
    private Transform spawnPlace;
    [SerializeField]
    private Transform fallPlace;
    [SerializeField]
    private List<Transform> places;
    [SerializeField]
    private float startDuration = 5f;
    private bool isMoving = false;

    private void Start()
    {
        lifes = new List<Transform>();
    }
    // appearing action
    public void StartLifesTrack()
    {
        // check for already started one
        if (isStarted) return;

        // toggle start state
        isStarted = true;

        // play appearing animation
        animator.SetTrigger("Appearing");

        // set visual track moving
        if (lifesTrackMaterial.HasProperty("TrackPosition"))
        {
            lifesTrackMaterial.SetFloat("TrackPosition", 0);
            float endValue = 2f;
            Tween.Value(0, endValue,
            (float t) =>
            {
                // check for life (heart) spawning
                if(t > lifes.Count * endValue / 5f)
                    SpawnAndMoveLife();

                // move track visually
                lifesTrackMaterial.SetFloat("TrackPosition", t);
            }, startDuration, 0f,
            startCallback: () => isMoving = true,
            completeCallback: () =>
            {
                // set default position
                lifesTrackMaterial.SetFloat("TrackPosition", 0);

                // toggle moving state
                isMoving = false;

                // check for lifes amount update
                if (GameManager.instance.LifesAmount != lifes.Count)
                {
                    if (GameManager.instance.LifesAmount < lifes.Count)
                        MoveLifesTrack();
                    else
                        AddLife();
                }
            });
        }

        // spawn and animate lifes (hearts) moving to their reserved places
    }
    // lifes adding action
    public IEnumerator AddLife()
    {
        while (isMoving)
            yield return new WaitForSeconds(.1f);

        // check for maximum amount of lifes
        if (lifes.Count < GameManager.instance.StartLifesAmount)
        {
            // get life's position
            Vector3 lifesPosition = places[places.Count - lifes.Count - 1].position;

            // spawn life at nearest heart place and save to lifes's list
            Transform newLife = Instantiate(lifePrefab, lifesPosition, Quaternion.identity).transform;
            newLife.name = lifePrefab.name;
            lifes.Add(newLife);
        }

    }
    // lifes removing action
    public void MoveLifesTrack()
    {
        // check for current moving
        if (isMoving) return;

        // move lifes track forward (one place forward)
        float endValue = 2f / 5f;
        Tween.Value(0, endValue, (float t) => lifesTrackMaterial.SetFloat("TrackPosition", t), startDuration / 5f, 0f,
        startCallback: () => isMoving = true,
        completeCallback: () =>
        {
            // set default position
            lifesTrackMaterial.SetFloat("TrackPosition", 0);

            // toggle moving state
            isMoving = false;

            // check for lifes amount update
            if(GameManager.instance.LifesAmount != lifes.Count)
            {
                if (GameManager.instance.LifesAmount < lifes.Count)
                    MoveLifesTrack();
                else
                    AddLife();
            }
        }, obeyTimescale: false);

        // move all lifes to next place (first added will fall with kinematic off)
        for(int i = 0; i < lifes.Count; i++)
        {
            Transform life = lifes[i];
            if (!life) return;
            if (i == 0)
            {
                Tween.Position(life, fallPlace.position, startDuration / 5f, 0f,
                completeCallback: () =>
                {
                    // turn kinematic off
                    life.GetComponent<Rigidbody>().isKinematic = false;

                    // remove from list
                    lifes.Remove(life);
                    Destroy(life.gameObject, 3f);
                }, obeyTimescale: false);
            }
            else
            {
                Tween.Position(life, places[places.Count - i].position, startDuration / 5f, 0f, obeyTimescale: false);
            }
        }
    }

    // start lifes track utility
    private void SpawnAndMoveLife()
    {
        // spawn new life at spawn place and save to lifes's list
        Transform newLife = Instantiate(lifePrefab, spawnPlace.position, Quaternion.identity).transform;
        newLife.name = lifePrefab.name;
        lifes.Add(newLife);

        // get place to move
        Vector3 endValue = places[places.Count - lifes.Count].position;

        // animate moving current new life to it's place
        float oneUnitMoveDuration = startDuration / 5f;
        Tween.Position(newLife, endValue, (places.Count - lifes.Count + 1) * oneUnitMoveDuration, 0);
    }
}
