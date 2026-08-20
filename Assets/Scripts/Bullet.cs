using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum BulletOwner
{
    Player,
    Enemy
}

public class Bullet : MonoBehaviour
{
    [Header("Propietario de la bala")]
    public BulletOwner owner;

    [Header("Estadísticas")]
    public float speed = 10f;
    public float damage = 10f;
    public float lifeTime = 1f;

    [Header("Efecto dot")]
    public bool applyDotEffect = false;
    public float dotDamagePerSecond = 10f;
    public float dotDuration = 3f;

    private Rigidbody2D rb;


    void Awake()
    {
        // Obtener referencias de RigidBody
        rb = GetComponent<Rigidbody2D>();
    }

    // Lanzar la bala hacia una dirección
    public void Init(Vector2 direction)
    {
        rb.velocity = direction.normalized * speed;

        // Destruye la bala cuando termina el lifetime
        if (lifeTime > 0f)
            Destroy(gameObject, lifeTime);
    }

    // Comportamiento de la bala cuando impacta algo
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Balas del jugador
        if (owner == BulletOwner.Player)
        {
            // Ignorar al jugador
            if (other.CompareTag("Player"))
                return;

            // Detectar enemigos y aplicar daño y dot
            Enemy enemy = other.GetComponent<Enemy>();
            if (enemy != null)
            {
                if (damage > 0f)
                    enemy.TakeDamage(damage);

                if (applyDotEffect)
                    enemy.ApplyDot(dotDamagePerSecond, dotDuration);

                Destroy(gameObject); // Destruye la bala cuando se golpea al enemigo
            }
        }

        // Balas del Boss
        else if (owner == BulletOwner.Enemy)
        {
            // Ignorar a enemigos
            if (other.CompareTag("Enemy"))
                return;

            // Detectar el jugador y aplicar daño
            Player player = other.GetComponent<Player>();
            if (player != null)
            {
                player.TakeDamage(damage);
                Destroy(gameObject); // Destruye la bala cuando se golpea al jugador
            }
        }
    }
}