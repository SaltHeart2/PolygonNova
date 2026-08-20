using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossRay : MonoBehaviour
{
    public float warningTime = 0.75f;
    public float attackTime = 0.2f;
    public float damage = 60f;

    bool hasDealtDamage;
    bool canDamage;

    SpriteRenderer sr;
    PolygonCollider2D col;

    Color warningColor = new Color(1f, 0f, 0f, 0.35f);
    Color attackColor = new Color(1f, 0f, 0f, 1f);


    void Awake()
    {
        // Obtener referencias de Collider y SpriteRenderer
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<PolygonCollider2D>();
    }

    // Orientar y posicionar el rayo
    public void Init(Vector2 direction, float length)
    {
        transform.right = direction;

        // Escala
        transform.localScale = new Vector3(
            length,
            transform.localScale.y,
            1f
        );

        // Rayo empiece en el Boss y extiende hacia afuera
        transform.position += (Vector3)(direction.normalized * length);

        // Iniciar la corrutina de rayos
        StartCoroutine(RayRoutine());
    }

    // Corrutina de rayos
    IEnumerator RayRoutine()
    {
        // Empezar con un fase de warning
        canDamage = false;
        hasDealtDamage = false;
        col.enabled = false;
        sr.color = warningColor;

        yield return new WaitForSeconds(warningTime);

        // Ahora empieza el ataque real
        canDamage = true;
        col.enabled = true;
        sr.color = attackColor;

        yield return new WaitForSeconds(attackTime);

        // Destruye el objeto al terminar
        Destroy(gameObject);
    }

    // Aplicar daño al jugador
    void OnTriggerEnter2D(Collider2D other)
    {
        if (!canDamage)
            return;
        if (hasDealtDamage)
            return;
        if (!other.CompareTag("Player"))
            return;

        other.GetComponent<Player>().TakeDamage(damage);
        hasDealtDamage = true;
    }
}