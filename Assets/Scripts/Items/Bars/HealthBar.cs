using TMPro;
using System;
using UnityEngine;
using Pixelplacement;

public class HealthBar : MonoBehaviour
{
    private bool isEnding; // ending state

    public Action onHealthEnded; // // delegate for 'health ended' event

    [SerializeField]
    private TextMeshProUGUI valueText; // value(amount) text of health
    private TextMeshProUGUI valueUIText; // value(amount) text of health (UI)

    [SerializeField]
    private UnityEngine.UI.Image heartIcon; // heart icon image
    private Material heartIconMaterial; // heart icon material

    private int health; // total amount of health

    private void Update()
    {
        transform.LookAt(transform.position + Camera.main.transform.forward);
    }

    // set health with fade in animation
    public void SetHealth(int health)
    {
        // set health amount
        this.health = health;
        valueText.text = this.health.ToString();

        // start init
        heartIconMaterial = Instantiate<Material>(Resources.Load<Material>($"Materials/Booster Health Icon Material"));
        heartIcon.material = heartIconMaterial;

        // fade in animation
        Color vertexColor = valueText.color;
        Tween.Value(0, 1, (float t) => { valueText.color = new Color(vertexColor.r, vertexColor.g, vertexColor.b, t); }, .15f, 0);

        heartIconMaterial = heartIcon.material;
        if (heartIconMaterial.HasProperty("ColorAlpha"))
            Tween.Value(0, 1, (float t) => { heartIconMaterial.SetColor("ColorAlpha", new Color(1, 1, 1, t)); }, .15f, 0);
    }
    // get damage (return current health)
    public void GetDamage(int damage)
    {
        // check for damage
        if (damage < 0) return;

        // get damage
        health -= damage;
        if (health <= 0)
        {
            // set health to 0
            health = 0;

            // fade in animation
            Color vertexColor = valueText.color;
            Tween.Value(1, 0, (float t) => { valueText.color = new Color(vertexColor.r, vertexColor.g, vertexColor.b, t); }, .15f, 0);
            if (heartIconMaterial.HasProperty("ColorAlpha"))
                Tween.Value(1, 0, (float t) => { heartIconMaterial.SetColor("ColorAlpha", new Color(1, 1, 1, t)); }, .15f, 0, completeCallback: () => onHealthEnded());
        }

        // set modified health value
        valueText.text = valueUIText.text = health.ToString();
    }
    // set current 2D version of booster's health bar
    public void SetReferenceToUI(TextMeshProUGUI value)
    {
        valueUIText = value;
        valueUIText.text = health.ToString();
    }
}
