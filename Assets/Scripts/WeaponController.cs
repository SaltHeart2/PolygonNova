using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class WeaponController : MonoBehaviour
{
    public enum WeaponType
    {
        NormalBullet,
        EnergyBall,
        DualSwords,
        Mines
    }

    public WeaponType currentWeapon = WeaponType.NormalBullet;
    public Transform firePoint;

    [Header("Bala normal")]
    public GameObject normalBulletPrefab;
    public float fireRate = 0.2f;

    private float lastFireTime;

    [Header("Energy Ball")]
    public float maxChargeTime = 3f;
    public float energyBallFireRate = 0.4f;

    float lastEnergyBallTime;

    private float currentCharge;
    private bool isCharging;

    [Header("Bola de energía")]
    public GameObject energyBallPrefab;

    public float minDamage = 20f;
    public float maxDamage = 400f;

    public float minSpeed = 10f;
    public float maxSpeed = 30f;

    public float minSize = 1f;
    public float maxSize = 3f;

    public Transform energyBallHoldPoint;

    private GameObject chargingEnergyBall;
    private Bullet chargingBullet;

    [Header("Minas")]
    public GameObject minePrefab;
    public float mineFireRate = 0.4f;
    public float mineLaunchForce = 20f;

    float lastMineTime;

    [Header("Espadas duales")]
    public Transform swordLeft;
    public Transform swordRight;

    public float swordRange = 2f;
    public float swordAttackDuration = 0.65f;
    public float swordCooldown = 0.8f;

    private bool isSwordAttacking;
    private float lastSwordTime;

    float angle;

    public Collider2D swordLeftCollider;
    public Collider2D swordRightCollider;


    void Start()
    {
        // Desactivar el collider de las espadas (no hacen daño si no realizamos un ataque)
        swordLeftCollider.enabled = false;
        swordRightCollider.enabled = false;
    }

    void Update()
    {
        // Seleccionar un comportamiento según el arma del jugador
        switch (currentWeapon)
        {
            case WeaponType.NormalBullet:
                HandleNormalBullet();
                break;

            case WeaponType.EnergyBall:
                HandleEnergyBallInput();
                break;

            case WeaponType.Mines:
                HandleMines();
                break;

            case WeaponType.DualSwords:
                HandleDualSwords();
                break;
        }
    }

    // Arma por defecto
    void HandleNormalBullet()
    {
        if (!Input.GetMouseButton(0)) return;
        if (EventSystem.current.IsPointerOverGameObject())
            return;
        if (Time.time - lastFireTime < fireRate)
            return;

        lastFireTime = Time.time; // Temporizador de disparo

        // Instanciar la bala para que tenga una posición y rotación correcta
        GameObject bullet = Instantiate(
            normalBulletPrefab,
            firePoint.position,
            firePoint.rotation
        );

        bullet.GetComponent<Bullet>().Init(firePoint.up); // Dispara desde el punto delantero del jugador

        // Reproducir el sonido
        MusicManager.Instance.PlaySFX(
            MusicManager.SFXType.PlayerNormalShot
        );
    }

    // Bola de energía
    void HandleEnergyBallInput()
    {
        // No hace nada si estamos en los paneles
        if (EventSystem.current.IsPointerOverGameObject())
            return;

        // Cuando se pulsa el botón izquierdo por primera vez
        if (Input.GetMouseButtonDown(0))
        {
            // No hace nada si no ha completado el cooldown
            if (Time.time - lastEnergyBallTime < energyBallFireRate)
                return;

            isCharging = true;
            currentCharge = 0f;

            // Instanciar la bola de energía para que tenga una posición y rotación correcta
            chargingEnergyBall = Instantiate(
                energyBallPrefab,
                energyBallHoldPoint.position,
                firePoint.rotation,
                energyBallHoldPoint
            );

            // Obtener referencias de RigidBody y el script Bullet
            chargingBullet = chargingEnergyBall.GetComponent<Bullet>();
            Rigidbody2D rb = chargingEnergyBall.GetComponent<Rigidbody2D>();
            rb.simulated = false; // Para que se queda pegada al jugador

            // Evitar que se mueva y no se destruye cuando cargamos
            chargingBullet.speed = 0f;
            chargingBullet.lifeTime = 0f;

            // Reproducir el sonido de carga
            MusicManager.Instance.PlaySFXLoop(
                MusicManager.SFXType.PlayerEnergyCharge
            );
        }

        // Cuando mantenemos el botón izquierdo, se carga la bola
        if (Input.GetMouseButton(0) && isCharging)
        {
            currentCharge += Time.deltaTime;
            currentCharge = Mathf.Clamp(currentCharge, 0f, maxChargeTime);

            float t = currentCharge / maxChargeTime;

            // Escalar
            float size = Mathf.Lerp(minSize, maxSize, t);
            chargingEnergyBall.transform.localScale = Vector3.one * size;

            // Calcular radios
            float ballRadius = size * 0.3f;
            float playerRadius = 0.5f;

            // Distancia total desde el centro del jugador
            float distance = playerRadius + ballRadius;

            // Posicionar la bola delante del jugador
            chargingEnergyBall.transform.position =
                transform.position + firePoint.up * distance;

        }

        // Cuando soltamos el botón izquierdo, se dispara
        if (Input.GetMouseButtonUp(0) && isCharging)
        {
            FireEnergyBall(currentCharge);
            isCharging = false;
        }
    }

    // Disparar la bola de energía
    void FireEnergyBall(float charge)
    {
        // Detener el sonido de carga y reproduce el sonido de disparo
        MusicManager.Instance.StopLoopSFX(
            MusicManager.SFXType.PlayerEnergyCharge
        );
        MusicManager.Instance.PlaySFX(
            MusicManager.SFXType.PlayerEnergyShot
        );

        float t = charge / maxChargeTime;

        chargingEnergyBall.transform.SetParent(null); // Separar la bola con el jugador

        // Vuelve la física de la bola
        Rigidbody2D rb = chargingEnergyBall.GetComponent<Rigidbody2D>();
        rb.simulated = true;

        // Calcular el daño de la bola y definir un lifetime
        chargingBullet.damage = Mathf.Lerp(minDamage, maxDamage, t);
        chargingBullet.speed = Mathf.Lerp(minSpeed, maxSpeed, t);
        chargingBullet.lifeTime = 2f;

        chargingBullet.Init(firePoint.up); // Para disparar a la dirección correcta

        lastEnergyBallTime = Time.time;

        chargingEnergyBall = null;
        chargingBullet = null;
    }

    // Cancelar el ataque cuando pausamos el juego
    public void CancelEnergyCharge()
    {
        if (!isCharging)
            return;

        isCharging = false;

        MusicManager.Instance.StopLoopSFX(
            MusicManager.SFXType.PlayerEnergyCharge
        );

        if (chargingEnergyBall != null)
            Destroy(chargingEnergyBall);

        chargingEnergyBall = null;
        chargingBullet = null;
        currentCharge = 0f;
    }

    // Minas
    void HandleMines()
    {
        if (!Input.GetMouseButton(0))
            return;
        if (EventSystem.current.IsPointerOverGameObject())
            return;
        if (Time.time - lastMineTime < mineFireRate)
            return;

        lastMineTime = Time.time;

        FireMine();
    }

    // Disparar las minas
    void FireMine()
    {
        // Se dispara por detrás del jugador
        Vector3 spawnPos = firePoint.position - firePoint.up * 0.6f;

        // Instanciar la mina
        GameObject mine = Instantiate(
            minePrefab,
            spawnPos,
            Quaternion.identity
        );

        // Referenciar el rigidbody y aplicar una fuerza de impulso hacia atrás
        Rigidbody2D rb = mine.GetComponent<Rigidbody2D>();
        rb.AddForce(-firePoint.up * mineLaunchForce, ForceMode2D.Impulse);

        // Definimos el lifetime de la mina
        Destroy(mine, 5f);

        // Reproducir el sonido de disparo
        MusicManager.Instance.PlaySFX(
            MusicManager.SFXType.PlayerMineDrop
        );
    }

    // Espadas duales
    void HandleDualSwords()
    {
        // Activar las espadas cuando seleccionamos este arma
        swordLeft.gameObject.SetActive(true);
        swordRight.gameObject.SetActive(true);

        if (isSwordAttacking)
            return;
        if (EventSystem.current.IsPointerOverGameObject())
            return;

        // Cuando pulsamos el botón izquierdo
        if (Input.GetMouseButtonDown(0))
        {
            // Comprobar el cooldown
            if (Time.time - lastSwordTime < swordCooldown)
                return;

            lastSwordTime = Time.time;

            StartCoroutine(DualSwordAttack()); // Ataque de las espadas
        }
    }

    // Corrutina de ataques de espadas
    IEnumerator DualSwordAttack()
    {
        // Reproducir el sonido de ataque
        MusicManager.Instance.PlaySFX(
            MusicManager.SFXType.PlayerDualSwords
        );

        isSwordAttacking = true;

        // Activar el collider de las espadas cuando atacamos
        swordLeftCollider.enabled = true;
        swordRightCollider.enabled = true;

        // Parámetro que controla el movimiento de las espadas
        float startAngle = -130f;
        float endAngle = 130f;
        float elapsed = 0f;

        // Se realiza un ataque de arco amplio
        // por lo cual tenemos que calcular la posición y rotación por cada frame
        while (elapsed < swordAttackDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / swordAttackDuration);

            float angle = Mathf.Lerp(startAngle, endAngle, t);

            // Cambiar la posición
            swordLeft.localPosition =
                Quaternion.Euler(0, 0, angle) * Vector3.up * swordRange;
            swordRight.localPosition =
                Quaternion.Euler(0, 0, -angle) * Vector3.up * swordRange;

            // Cambiar la rotación
            swordLeft.localRotation =
                Quaternion.Euler(0, 0, angle);
            swordRight.localRotation =
                Quaternion.Euler(0, 0, -angle);

            yield return null;
        }

        // Al terminar el ataque, desactiva el collider de las espadas
        swordLeftCollider.enabled = false;
        swordRightCollider.enabled = false;

        isSwordAttacking = false;
    }
}