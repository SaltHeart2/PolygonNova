using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossLaser : MonoBehaviour
{
    public float damagePerSecond = 180f;

    public SpriteRenderer sr;
    public BoxCollider2D col;

    public Color warningColor = new Color(1f, 0f, 0f, 0.35f);
    public Color firingColor = new Color(1f, 0f, 0f, 1f);


    void Awake()
    {
        // Obtener referencias de Collider y SpriteRenderer
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<BoxCollider2D>();
    }

    // Fase de warning
    public void StartWarning()
    {
        gameObject.SetActive(true);
        sr.color = warningColor;
        col.enabled = false;
    }

    // Fase de ataque real
    public void StartFiring()
    {
        sr.color = firingColor;
        col.enabled = true;
    }

    // Desactivar el láser
    public void ResetLaser()
    {
        col.enabled = false;
        gameObject.SetActive(false);
    }

    // Aplicar daño contínuo cuando solapa con el jugador
    void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        Player player = other.GetComponent<Player>();
        if (player != null)
        {
            player.TakeDamage(damagePerSecond * Time.deltaTime);
        }
    }
}