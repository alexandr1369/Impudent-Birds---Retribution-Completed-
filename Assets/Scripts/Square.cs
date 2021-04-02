using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Pixelplacement;
using Pixelplacement.TweenSystem;

public class Square : MonoBehaviour
{
    // bird has just been spawned (square is reserved)
    private bool isTargeting = false;
    public bool IsTargeting { get => isTargeting; set => isTargeting = value; }

    [SerializeField]
    private new Renderer renderer; // main renderer

    [SerializeField]
    private AnimationCurve fadeInOutCurve; // just a curve for beautiful fades

    private Item item; // current item on square(if exists)

    private List<Material> materialsList; // list of all (4) materials
    private MaterialType materialType; // current type of material

    private bool shopState; // shop state (open/close)
    private bool isBeingReversed; // grid rejected building (y/n) -> reversing square mats back

    private void Awake()
    {
        // create {4} individual materials for square
        materialsList = new List<Material>();
        Array values = Enum.GetValues(typeof(MaterialType));
        for (int i = 0; i < values.Length; i++)
        {
            MaterialType materialType = (MaterialType)values.GetValue(i);
            Material material = Resources.Load<Material>($"Materials/Square {materialType} Material");
            materialsList.Add(material);
        }
    }
    private void Update()
    {
        // [bug with square 'default' alpha == .75f but .6f] utility
        if (!GridManager.instance.IsShopOpen() && shopState && materialType == MaterialType.Default)
        {
            shopState = false;
            if (renderer.material.HasProperty("BorderAlpha"))
                renderer.material.SetFloat("BorderAlpha", 0);
        }
    }

    // select the square (fade animations + action)
    public void Select()
    {
        // check for having an enemy upon a square
        Enemy enemy = item as Enemy;
        if (!enemy || enemy.IsSelected()) return;

        // clear the square (1)
        enemy.FreeSquare(false);

        // square cant be highlighted when shop is open
        if (GridManager.instance.IsShopOpen()) return;

        // set right material according to enemy type (mb add in future)
        EnemyType enemyType = enemy.GetEnemyType();
        if (enemyType == EnemyType.Bomb)
            SetMaterial(MaterialType.Fault);
        else
            SetMaterial(MaterialType.Selected);

        // clear the square (2)
        enemy = null;

        // highlight the square
        Material material = renderer.material;
        if(material.HasProperty("SquareAlpha") && material.HasProperty("UniqueAlpha"))
        {
            // set total alpha
            Tween.Value(.6f, .75f, (t) => material.SetFloat("SquareAlpha", t), .25f, 0, completeCallback: () => 
            {
                Tween.Value(.75f, .6f, (t) => material.SetFloat("SquareAlpha", t), .25f, 0);
            });

            // set unique alpha
            Tween.Value(0, 1f, (t) => material.SetFloat("UniqueAlpha", t), .25f, 0, completeCallback: () =>
            {
                Tween.Value(1f, 0, (t) => material.SetFloat("UniqueAlpha", t), .25f, 0, completeCallback: () => 
                {
                    // set right settings for default material on the end of fade select/fault material animation
                    if (material.HasProperty("DefaultBorderAlpha"))
                    {
                        SetMaterial(MaterialType.Default);
                        if(renderer)
                            material = renderer.material;

                        if (material.HasProperty("SquareAlpha") && material.HasProperty("BorderAlpha"))
                        {
                            material.SetFloat("SquareAlpha", shopState ? .75f : .6f);
                            material.SetFloat("BorderAlpha", shopState ? 1f : 0);
                        }
                    }
                });
            });
        }
    }
    // set current material default border color
    public void TrySetCurrentBorderColor(Color newColor)
    {
        if (renderer.material.HasProperty("MultiplyColor"))
            renderer.material.SetColor("MultiplyColor", newColor);
    }

    // check square for building availabillity and set material (build or fault)
    public bool TryBuild()
    {
        bool state = !(item as Booster);

        if (state)
        {
            SetMaterial(MaterialType.Build);
            //if (CameraController.instance.CurrentZoom == 1)
            //    if (renderer.material.HasProperty("SquareAlpha"))
            //        renderer.material.SetFloat("SquareAlpha", 0);
        }
        else
            SetMaterial(MaterialType.Fault);

        return state;
    }
    public bool TryBuild(BoosterType boosterType)
    {
        bool state = !(item as Booster);

        if (state)
        {
            SetMaterial(MaterialType.Build);
            //if (CameraController.instance.CurrentZoom == 1 && boosterType != BoosterType.Minefield)
            //    if (renderer.material.HasProperty("SquareAlpha"))
            //        renderer.material.SetFloat("SquareAlpha", 0);
        }
        else
            SetMaterial(MaterialType.Fault);

        return state;
    }
    // clear square after 'trying to build'
    public void ClearBuild()
    {
        Material material;
        if (materialType == MaterialType.Fault)
        {
            SetMaterial(MaterialType.Build);
            material = renderer.material;
            if (material.HasProperty("SquareAlpha"))
                material.SetFloat("SquareAlpha", 0);
        }
        else
        {
            SetMaterial(MaterialType.Default);
            material = renderer.material;
            if (material.HasProperty("BorderAlpha") && material.HasProperty("SquareAlpha"))
            {
                material.SetFloat("BorderAlpha", 1);
                material.SetFloat("SquareAlpha", .75f);
            }
        }
    }
    // inform about square reversing (when a booster can't be build on another one or barrier)
    public void ReverseBuild() => isBeingReversed = true;
    // reset squares materials with animation after booster timer has expired
    public void ResetSquare()
    {
        if(materialType == MaterialType.Build)
        {
            SetMaterial(MaterialType.Default);
            Material material = renderer.material;
            if (material.HasProperty("BorderAlpha") && material.HasProperty("SquareAlpha"))
            {
                if (GridManager.instance.IsShopOpen())
                {
                    material.SetFloat("BorderAlpha", 1);
                    material.SetFloat("SquareAlpha", .75f);
                }
                else
                {
                    material.SetFloat("BorderAlpha", 0);
                    material.SetFloat("SquareAlpha", 0f);
                    Tween.Value(0, .65f, (float t) => { material.SetFloat("SquareAlpha", t); }, .15f, 0, fadeInOutCurve);
                }
            }
        }
    }
    // turn on/off shop state material(visual only)
    public void ToggleShopState(bool state)
    {
        // get current material
        Material material = renderer.material;

        // make material transition 
        switch (materialType)
        {
            case MaterialType.Default:
            {
                if (material.HasProperty("BorderAlpha") && material.HasProperty("SquareAlpha"))
                {
                    Tween.Value(state ? 0 : 1f, state ? 1f : 0, (float t) => { material.SetFloat("BorderAlpha", t); }, .15f, 0, fadeInOutCurve, obeyTimescale: false);
                    Tween.Value(state ? .6f : .75f, state ? .75f : .6f, (float t) => { material.SetFloat("SquareAlpha", t); }, .15f, 0, fadeInOutCurve, obeyTimescale: false);
                }
            } break;
            case MaterialType.Build:
            {
                if (/*isBeingReversed*/true)
                {
                    if (material.HasProperty("UniqueAlpha") && material.HasProperty("SquareAlpha") && material.GetFloat("SquareAlpha") > 0)
                    {
                        Tween.Value(.75f, .6f, (float t) => { material.SetFloat("SquareAlpha", t); }, .15f, 0, fadeInOutCurve, obeyTimescale: false);
                        Tween.Value(1f, 0, (float t) => { material.SetFloat("UniqueAlpha", t); }, .15f, 0, fadeInOutCurve, obeyTimescale: false, completeCallback: () =>
                        {
                            SetMaterial(MaterialType.Default);
                            isBeingReversed = false;
                        });
                    }
                }
                else
                {
                    if (!state && material.HasProperty("SquareAlpha") && material.GetFloat("SquareAlpha") > 0)
                    {
                        Tween.Value(.75f, 0, (float t) => { material.SetFloat("SquareAlpha", t); }, .15f, 0, fadeInOutCurve, obeyTimescale: false);
                    }
                }
            } break;
            case MaterialType.Selected:
            {
                // get shop state
                shopState = state;

                // toggle unique alpha
                if (material.HasProperty("UniqueAlpha"))
                    Tween.Value(state ? 0 : 1f, state ? 1f : 0, (float t) => { material.SetFloat("UniqueAlpha", t); }, .15f, 0, fadeInOutCurve);

                // show default highlight when shop is openning
                if (state && material.HasProperty("DefaultBorderAlpha"))
                    Tween.Value(0, 1f, (float t) => { material.SetFloat("DefaultBorderAlpha", t); }, .15f, 0, fadeInOutCurve);
            } break;
            case MaterialType.Fault:
            {
                // get shop state
                shopState = state;

                if (isBeingReversed)
                {
                    // hiding unique alpha
                    if (material.HasProperty("UniqueAlpha"))
                        Tween.Value(1f, 0, (float t) => { material.SetFloat("UniqueAlpha", t); }, .15f, 0, fadeInOutCurve);

                    // hiding square after trying to build booster on wrong square with 'fault' material
                    if (material.HasProperty("SquareAlpha"))
                    {
                        Tween.Value(.75f, 0, (float t) => { material.SetFloat("SquareAlpha", t); }, .15f, 0, fadeInOutCurve, completeCallback: () =>
                        {
                            SetMaterial(MaterialType.Build);
                            material = renderer.material;
                            if (material.HasProperty("SquareAlpha"))
                            {
                                material.SetFloat("SquareAlpha", 0);
                                isBeingReversed = false;
                            }
                        });
                    }
                }
                else
                {
                    // toggle unique alpha
                    if (material.HasProperty("UniqueAlpha"))
                        Tween.Value(state ? 0 : 1f, state ? 1f : 0, (float t) => { material.SetFloat("UniqueAlpha", t); }, .15f, 0, fadeInOutCurve);

                    // show default highlight when shop is openning
                    if (state && material.HasProperty("DefaultBorderAlpha"))
                        Tween.Value(0, 1f, (float t) => { material.SetFloat("DefaultBorderAlpha", t); }, .15f, 0, fadeInOutCurve);

                    // hiding square after trying to build booster on wrong square with 'fault' material
                    if (!state && material.HasProperty("SquareAlpha"))
                        Tween.Value(.75f, 0, (float t) => { material.SetFloat("SquareAlpha", t); }, .15f, 0, fadeInOutCurve);
                }
            } break;
        } 
    }

    // get current item
    public Item GetItem() => item;
    // set new item(link)
    public void SetItem(Item item)
    {
        Enemy enemy = this.item as Enemy;
        if(enemy)
            enemy.FreeSquare(true);

        this.item = item;
    }
    public void SetBarrier(Item item)
    {
        this.item = item;

        SetMaterial(MaterialType.Build);
        Material material = renderer.material;
        if(material.HasProperty("UniqueAlpha") && material.HasProperty("SquareAlpha"))
        {
            material.SetFloat("UniqueAlpha", 0);
            material.SetFloat("SquareAlpha", 0);
        }
    }

    // just switching between materials
    private void SetMaterial(MaterialType materialType)
    {
        if (!renderer) return;
        renderer.material = materialsList.Find((t) => t.name == $"Square {materialType} Material"); ;
        this.materialType = materialType;
    }
    private IEnumerator SetMaterial(MaterialType materialType, float delay)
    {
        yield return new WaitForSeconds(delay);
        renderer.material = materialsList.Find((t) => t.name == $"Square {materialType} Material"); ;
        this.materialType = materialType;
    }
}
public enum MaterialType
{
    Default = 0,
    Build = 1,
    Selected = 2,
    Fault = 3
}
