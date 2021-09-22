using System;
using System.Collections;
using System.Collections.Generic;
using Extensions;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEngine.Camera;
using static UnityEngine.InputSystem.InputAction;

public class Player : MonoBehaviour, IHealth
{
    Camera cam;
    public GameObject obj {get => gameObject; set{}}
    [SerializeField] private LaserPooler laserPooler;
    [SerializeField] private ShotgunPooler shotgunPooler;
    [SerializeField] private RocketPooler rocketPooler;
    [SerializeField] private LaserBeamPooler laserBeamPooler;
    [SerializeField] private ParticleSystemPooler explosionPooler;
    [SerializeField] private ParticleSystemPooler sparkPooler;
    [SerializeField] private RingIndicator powerRing;
    [SerializeField] private AudioSource attackAudioSource;
    [SerializeField] private AudioSource loopingLaserBeamAudioSource;
    [SerializeField] public AudioSource enegineHumAudioSource;
    [SerializeField] public AudioSource damagedAudioSource;
    [SerializeField] private AudioSource resourceAlarmAudioSource;
    public float maxSpeed;
    public float deccelleration;
    public float accelleration;
    public float speed {get; private set;}
    public Vector2 movementInput {get; private set;} = new Vector2();
    private Vector2 lastMovementDir = new Vector2();
    private bool primaryFire = false;
    public float passiveFireDelay;
    private float defaultPassiveFireDelay;
    private float nextPassiveFireTime = 0;
    public Transform firePoint;
    public float damage;
    public bool canScreenWrap = true;

    public float maxHealth {get => _maxHP; set{}}
    [SerializeField] private float _maxHP;
    [ReadOnly, ShowInInspector] public float currentHealth {get => _hp; set{_hp = value;}}
    float _hp;
    public bool isDead {get => _isDead; set{}}
    bool _isDead;
    public bool isDying {get => _isDying; set{}}
    bool _isDying;
    public event OnHealthChanged onHealthChanged;

    EndGamePanel endGamePanel;
    [SerializeField] private Score score;
    PauseMenu pauseMenu;

    public delegate void OnResourceChanged(float val);
    public OnResourceChanged onResourceChanged;

    public float resource;

    public float moveResourceHit;
    public float attackResourceHit;

    public float tempPower {get; private set;}
    public float tempPowerupDuration;
    private float? tempFadeAtTime;
    public WeaponType weapon = WeaponType.None;

    public List<LaserBeam> existingLaserBeams = new List<LaserBeam>();

    public Dictionary<Powerups, int> powerupsDict = new Dictionary<Powerups, int>();

    public AudioClip blasterClip;
    public AudioClip shotgunClip;
    public AudioClip rocketClip;

    public AudioClip laserPowerOn;
    public AudioClip laserPowerOff;

    public List<AudioClip> damagedClips;
    private float lastDamagedClipPlayedAt;
    private ScreenWrapIndicator screenWrapIndicator;

    private void Awake()
    {
        cam = Camera.main;
        endGamePanel = GameObject.FindObjectOfType<EndGamePanel>();
        pauseMenu = GameObject.FindObjectOfType<PauseMenu>();
        screenWrapIndicator = GameObject.FindObjectOfType<ScreenWrapIndicator>();
        currentHealth = maxHealth;
        defaultPassiveFireDelay = passiveFireDelay;
    }

    private void Update()
    {
        if(primaryFire && Time.timeSinceLevelLoad >= nextPassiveFireTime)
        {
            PrimaryFire();
            if(weapon == WeaponType.LaserBeam && existingLaserBeams != null && existingLaserBeams.Count > 0)
                AdjustLaser();
        }
        
        if(tempFadeAtTime.HasValue && Time.timeSinceLevelLoad >= tempFadeAtTime.Value)
        {
            // play audio
            // turn off vfx
            powerRing.SetMode(RingMode.None);
            powerRing.Hide();
            tempPower = 0;
            tempFadeAtTime = null;
        }
    }

    private void FixedUpdate()
    {
        if(isDead || isDying)
            return;

        Move();
        ScreenWrap();
        LookAtMouse();

        int additionalProj = powerupsDict.ContainsKey(Powerups.Multishot) ? additionalProj = powerupsDict[Powerups.Multishot] : 0;
        float degreesBetweenProj = 120f / (float)(2 + additionalProj);
        for(int i = 0; i < existingLaserBeams.Count; i++)
        {
            LaserBeam beam = existingLaserBeams[i];
            Vector3 eulerRot = transform.rotation.eulerAngles + new Vector3(0, 0, (degreesBetweenProj * (i + 1)) - 60);
            beam.transform.SetPositionAndRotation(firePoint.position, Quaternion.Euler(eulerRot));
        }
    }
    
    public void InputInterrupt()
    {
        movementInput = Vector2.zero;
        lastMovementDir = Vector2.zero;
        primaryFire = false;
        loopingLaserBeamAudioSource.Stop();
        foreach(LaserBeam existingLaserBeam in existingLaserBeams)
            existingLaserBeam.Fade();
        existingLaserBeams.Clear();
    }

    private void ScreenWrap()
    {
        Vector3 screenPos = cam.WorldToScreenPoint(transform.position, MonoOrStereoscopicEye.Mono);
        Vector3 wrappedPos = screenPos.GetScreenWrapPosition(screenWrapIndicator);
        if(screenPos == wrappedPos)
            return;

        // screenWrapSource.Play(); // audio
        transform.position = cam.ScreenToWorldPoint(wrappedPos, MonoOrStereoscopicEye.Mono);
    }
    private void LookAtMouse()
    {
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Vector3 mouseWorldPos = cam.ScreenToWorldPoint((Vector3)mousePos + new Vector3(0, 0, 10), MonoOrStereoscopicEye.Mono);
        transform.rotation = Quaternion.LookRotation(transform.forward, (mouseWorldPos - transform.position).normalized);
    }

    public void AddPowerup(Powerups powerup)
    {
        if(powerupsDict.ContainsKey(powerup))
            powerupsDict[powerup]++;
        else
            powerupsDict.Add(powerup, 1);
    }

    private void Move()
    {
        Vector2 dir = new Vector2();

        float resourceMod = Mathf.Clamp(resource, -1, 1).Remap(-1, 1, .5f, 2);
        float tempResourceMod = tempPower.Remap(-1, 1, 0.01f, 1.99f);
        float scaledMaxSpeed = maxSpeed * resourceMod * tempResourceMod;

        if(movementInput.magnitude == 0 && speed > 0)
        {
            // deccellerate
            if(speed > 0)
                speed = Mathf.Clamp(speed - deccelleration, 0, scaledMaxSpeed);
            else if(speed == 0)
                lastMovementDir = Vector2.zero;
            
            dir = lastMovementDir;
        }
        else if(movementInput.magnitude > 0)
        {
            // accellerate
            if(speed < scaledMaxSpeed)
                speed = Mathf.Clamp(speed + accelleration, 0, scaledMaxSpeed);
            
            lastMovementDir = movementInput;
            dir = movementInput;
        }

        if(speed > 0 && dir.magnitude > 0)
        {
            Vector3 targetPos = transform.position + (Vector3)dir * speed * Time.fixedDeltaTime;
            Vector3 screenPos = cam.WorldToScreenPoint(targetPos);
            if(screenPos.x < 0 && screenWrapIndicator.leftLocked)
                return;
            else if(screenPos.x > Screen.width && screenWrapIndicator.rightLocked)
                return;
            else if(screenPos.y < 0 && screenWrapIndicator.topLocked)
                return;
            else if(screenPos.y > Screen.height && screenWrapIndicator.bottomLocked)
                return;
            transform.position = targetPos;
            UpdateResource(moveResourceHit);
        }
    }

    public void ActivatePower()
    {
        if(resource == 0)
            return;

        tempPower = Mathf.Clamp(resource, -1, 1);

        tempFadeAtTime = Time.timeSinceLevelLoad + tempPowerupDuration;

        // play audio
        powerRing.Show(tempPowerupDuration);
        powerRing.SetMode(resource < 0 ? RingMode.Power : RingMode.Speed, Mathf.Abs(tempPower));

        resource = 0;
        onResourceChanged?.Invoke(0);
    }

    public void ActivatePower(CallbackContext context)
    {
        if(!context.performed)
            return;

        if(isDying || isDying || pauseMenu.paused)
            return;
        
        ActivatePower();
    }

    public void Vertical(CallbackContext context)
    {
        if(isDead || isDying || pauseMenu.paused)
            return;

        movementInput = new Vector2(movementInput.x, context.ReadValue<float>());
    }
    public void Horizontal(CallbackContext context)
    {
        if(isDead || isDying || pauseMenu.paused)
            return;

        movementInput = new Vector2(context.ReadValue<float>(), movementInput.y);
    }

    public void Shoot(CallbackContext context)
    {
        if(isDead || isDying || pauseMenu.paused)
            return;

        if(context.performed)
            PrimaryFire();
        else if(context.canceled)
        {
            primaryFire = false;
            if(weapon == WeaponType.LaserBeam && existingLaserBeams != null && existingLaserBeams.Count > 0)
            {
                // turn off laser
                attackAudioSource.PlayOneShot(laserPowerOff);
                loopingLaserBeamAudioSource.Stop();
                foreach(LaserBeam existingLaserBeam in existingLaserBeams)
                    existingLaserBeam.Fade();
                existingLaserBeams.Clear();
            }
        }
        else if(context.started)
        {
            primaryFire = true;
            if(weapon == WeaponType.LaserBeam && existingLaserBeams.Count == 0)
            {
                // turn on laser
                attackAudioSource.PlayOneShot(laserPowerOn);
                loopingLaserBeamAudioSource.Play();
                LaserBeam();
                TickLaserBeamResource();
            }
        }
    }

    private void PrimaryFire()
    {
        switch(weapon)
        {
            case WeaponType.Blaster:
                BasterFire();   
                break;
            case WeaponType.Grenade:
                GrenadeFire();
                break;
            case WeaponType.LaserBeam:
                TickLaserBeamResource();
                break;
            case WeaponType.Rocket:
                RocketFire();
                break;
            case WeaponType.Shotgun:
                ShotgunFire();
                break;
            default:
                return;
        }
        
    }

    private void TickLaserBeamResource()
    {
        UpdateResource(-(attackResourceHit / 2.75f));
    }

    private void AdjustLaser()
    {
        float clampedResource = Mathf.Clamp(resource, -1, 1);
        nextPassiveFireTime = Time.timeSinceLevelLoad + 0.1f;

        float attackSpeedPowerupMod = 1;
        if(powerupsDict.ContainsKey(Powerups.FireRate))
        {
            for(int i = 0; i < powerupsDict[Powerups.FireRate]; i++)
                attackSpeedPowerupMod *= 0.75f;
        }

        foreach(LaserBeam existingLaserBeam in existingLaserBeams)
        {
            existingLaserBeam.damageRate = existingLaserBeam.defaultDamageRate * attackSpeedPowerupMod;

            float speedMod = clampedResource.Remap(-1, 1, 1.75f, 0.75f);
            float tempPowerSpeedMod = tempPower.Remap(-1, 1, 1.75f, 0.25f);
            float projSpeedPowerupMod = 1;
            if(powerupsDict.ContainsKey(Powerups.ProjectileSpeed))
                projSpeedPowerupMod += powerupsDict[Powerups.ProjectileSpeed] * .1f;

            float caliberMod = clampedResource.Remap(-1, 1, 1.5f, 0.5f);
            float tempPowerCaliberMod = tempPower.Remap(-1, 1, 1.5f, 0.5f);
            float caliberPowerupMod = 1;
            if(powerupsDict.ContainsKey(Powerups.Caliber))
                caliberPowerupMod += powerupsDict[Powerups.Caliber] * 0.1f;

            existingLaserBeam.SetLength(3.5f * speedMod * tempPowerSpeedMod * projSpeedPowerupMod);
            existingLaserBeam.SetWidth(2.5f * caliberMod * tempPowerCaliberMod * caliberPowerupMod);

            float attackMod = clampedResource.Remap(-1, 1, 2.5f, 0.5f);
            float tempPowerAttackMod = tempPower.Remap(-1, 1, 1.9f, .1f);
            existingLaserBeam.damage = (damage / 10) * attackMod * tempPowerAttackMod * caliberPowerupMod;
        }
    }

    public void LaserBeam()
    {
        float clampedResource = Mathf.Clamp(resource, -1, 1);
        nextPassiveFireTime = Time.timeSinceLevelLoad + 0.1f;

        Vector3 mouseWorldPos = cam.ScreenToWorldPoint((Vector3)Mouse.current.position.ReadValue() + Vector3.forward * 10, MonoOrStereoscopicEye.Mono);

        int additionalProj = powerupsDict.ContainsKey(Powerups.Multishot) ? additionalProj = powerupsDict[Powerups.Multishot] : 0;
        float degreesBetweenProj = 120f / (float)(2 + additionalProj);

        for(int i = 0; i < 1 + additionalProj; i++)
        {
            LaserBeam newLaserBeam = laserBeamPooler.pool.Get();
            existingLaserBeams.Add(newLaserBeam);
            Vector3 eulerRot = transform.rotation.eulerAngles + new Vector3(0, 0, (degreesBetweenProj * (i + 1)) - 60);
            newLaserBeam.transform.SetPositionAndRotation(firePoint.position, Quaternion.Euler(eulerRot));

            float attackSpeedPowerupMod = 1;
            if(powerupsDict.ContainsKey(Powerups.FireRate))
            {
                for(int j = 0; j < powerupsDict[Powerups.FireRate]; j++)
                    attackSpeedPowerupMod *= 0.75f;
            }
            newLaserBeam.damageRate = newLaserBeam.defaultDamageRate * attackSpeedPowerupMod;
            
            float speedMod = clampedResource.Remap(-1, 1, 1.75f, 0.75f);
            float tempPowerSpeedMod = tempPower.Remap(-1, 1, 1.75f, 0.25f);
            float projSpeedPowerupMod = 1;
            if(powerupsDict.ContainsKey(Powerups.ProjectileSpeed))
                projSpeedPowerupMod += powerupsDict[Powerups.ProjectileSpeed] * .1f;

            float caliberMod = clampedResource.Remap(-1, 1, 1.5f, 0.5f);
            float tempPowerCaliberMod = tempPower.Remap(-1, 1, 1.5f, 0.5f);
            float caliberPowerupMod = 1;
            if(powerupsDict.ContainsKey(Powerups.Caliber))
                caliberPowerupMod += powerupsDict[Powerups.Caliber] * 0.1f;

            float attackMod = clampedResource.Remap(-1, 1, 2.5f, 0.5f);
            float tempPowerAttackMod = tempPower.Remap(-1, 1, 1.9f, .1f);
            
            newLaserBeam.Fire(transform, firePoint, (damage / 10) * attackMod * tempPowerAttackMod * caliberPowerupMod,  3.5f * speedMod * tempPowerSpeedMod * projSpeedPowerupMod, 2.5f * caliberMod * tempPowerCaliberMod * caliberPowerupMod);
        }
    }

    public void GrenadeFire()
    {
        UpdateResource(-attackResourceHit);
    }

    public void RocketFire()
    {
        UpdateResource(-(attackResourceHit * 3));
        float clampedResource = Mathf.Clamp(resource, -1, 1);

        float fireDelayMod = clampedResource.Remap(-1, 1, .5f, 2f);
        float tempPowerFireDelayMod = tempPower.Remap(-1, 1, .5f, 2f);
        float attackSpeedPowerupMod = 1;
        if(powerupsDict.ContainsKey(Powerups.FireRate))
        {
            for(int i = 0; i < powerupsDict[Powerups.FireRate]; i++)
                attackSpeedPowerupMod *= 0.75f;
        }
        passiveFireDelay = defaultPassiveFireDelay * fireDelayMod * tempPowerFireDelayMod * attackSpeedPowerupMod;
        nextPassiveFireTime = Time.timeSinceLevelLoad + passiveFireDelay;

        Vector3 mouseWorldPos = cam.ScreenToWorldPoint((Vector3)Mouse.current.position.ReadValue() + Vector3.forward * 10, MonoOrStereoscopicEye.Mono);
        int additionalProj = powerupsDict.ContainsKey(Powerups.Multishot) ? additionalProj = powerupsDict[Powerups.Multishot] : 0;
        float degreesBetweenProj = 120f / (float)(2 + additionalProj);
        bool audioPlayed = false;
        for(int i = 0; i < 1 + additionalProj; i++)
        {
            Rocket rocket = rocketPooler.pool.Get();
            Vector3 eulerRot = transform.rotation.eulerAngles + new Vector3(0, 0, (degreesBetweenProj * (i + 1)) - 60);
            rocket.transform.SetPositionAndRotation(firePoint.position, Quaternion.Euler(eulerRot));

            float speedMod = clampedResource.Remap(-1, 1, 1.75f, 0.75f);
            float tempPowerSpeedMod = tempPower.Remap(-1, 1, 1.75f, 0.25f);
            float projSpeedPowerupMod = 1;
            if(powerupsDict.ContainsKey(Powerups.ProjectileSpeed))
                projSpeedPowerupMod += powerupsDict[Powerups.ProjectileSpeed] * .1f;
            rocket.speed = rocket.defaultSpeed * speedMod * tempPowerSpeedMod * projSpeedPowerupMod;

            float caliberMod = clampedResource.Remap(-1, 1, 1.5f, 0.5f);
            float tempPowerCaliberMod = tempPower.Remap(-1, 1, 1.5f, 0.5f);
            float caliberPowerupMod = 1;
            if(powerupsDict.ContainsKey(Powerups.Caliber))
                caliberPowerupMod += powerupsDict[Powerups.Caliber] * 0.1f;
            rocket.SetScale(caliberMod * tempPowerCaliberMod * caliberPowerupMod);

            float attackMod = clampedResource.Remap(-1, 1, 2.5f, 0.5f);
            float tempPowerAttackMod = tempPower.Remap(-1, 1, 1.9f, .1f);
            rocket.Fire(transform, damage * attackMod * tempPowerAttackMod * caliberPowerupMod, true);
            if(!audioPlayed)
            {
                attackAudioSource.PlayOneShot(rocketClip);
                audioPlayed = true;
            }
        }
    }

    public void ShotgunFire()
    {
        UpdateResource(-(attackResourceHit * 2));
        float clampedResource = Mathf.Clamp(resource, -1, 1);

        float fireDelayMod = clampedResource.Remap(-1, 1, .5f, 2f);
        float tempPowerFireDelayMod = tempPower.Remap(-1, 1, .5f, 2f);
        float attackSpeedPowerupMod = 1;
        if(powerupsDict.ContainsKey(Powerups.FireRate))
        {
            for(int i = 0; i < powerupsDict[Powerups.FireRate]; i++)
                attackSpeedPowerupMod *= 0.75f;
        }
        passiveFireDelay = defaultPassiveFireDelay * fireDelayMod * tempPowerFireDelayMod * attackSpeedPowerupMod;
        nextPassiveFireTime = Time.timeSinceLevelLoad + passiveFireDelay;
        int additionalProj = powerupsDict.ContainsKey(Powerups.Multishot) ? additionalProj = powerupsDict[Powerups.Multishot] : 0;
        if(gameObject.activeSelf)
        {
            StartCoroutine(ShotgunBurst(
                new UnitBurst(
                    time: 0.0f,
                    count: 1,
                    cycles: 6 + additionalProj,
                    interval: 0.01f
                ), 
                clampedResource
            ));
        }
    }

    IEnumerator ShotgunBurst(UnitBurst burst, float clampedResource)
    {
        Vector3 mouseWorldPos = cam.ScreenToWorldPoint((Vector3)Mouse.current.position.ReadValue() + Vector3.forward * 10, MonoOrStereoscopicEye.Mono);
        bool audioPlayed = false;
        for(int i = 0; i < burst.cycles; i++)
        {
            if(!gameObject.activeSelf)
                break;
            for(int j = 0; j < burst.count; j++)
            {
                if(!gameObject.activeSelf)
                    break;
                LaserProjectile proj = shotgunPooler.pool.Get();
                Vector3 eulerRot = transform.rotation.eulerAngles + new Vector3(0, 0, UnityEngine.Random.Range(-15f * (1f - 2), 15f * (1f - 2)));
                proj.transform.SetPositionAndRotation(firePoint.position, Quaternion.Euler(eulerRot));

                float speedMod = clampedResource.Remap(-1, 1, 1.75f, 0.75f);
                float tempPowerSpeedMod = tempPower.Remap(-1, 1, 1.75f, 0.25f);
                float projSpeedPowerupMod = 1;
                if(powerupsDict.ContainsKey(Powerups.ProjectileSpeed))
                    projSpeedPowerupMod += powerupsDict[Powerups.ProjectileSpeed] * .1f;
                proj.speed = proj.defaultSpeed * speedMod * tempPowerSpeedMod * projSpeedPowerupMod;

                float caliberMod = clampedResource.Remap(-1, 1, 2f, 0.333f);
                float tempPowerCaliberMod = tempPower.Remap(-1, 1, 1.5f, 0.5f);
                float caliberPowerupMod = 1;
                if(powerupsDict.ContainsKey(Powerups.Caliber))
                    caliberPowerupMod += powerupsDict[Powerups.Caliber] * 0.1f;
                proj.trail.widthMultiplier = proj.defaultWidthMulti * caliberMod * tempPowerCaliberMod * caliberPowerupMod;
                proj.col.size = new Vector2(proj.trail.widthMultiplier, proj.col.size.y);

                float attackMod = clampedResource.Remap(-1, 1, 2.5f, 0.5f);
                float tempPowerAttackMod = tempPower.Remap(-1, 1, 1.9f, .1f);
                proj.Fire(transform, damage / 6 * attackMod * tempPowerAttackMod * caliberPowerupMod, true);
                if(!audioPlayed)
                {
                    attackAudioSource.PlayOneShot(shotgunClip);
                    audioPlayed = true;
                }
            }
            yield return new WaitForSeconds(burst.interval);
        }
    }

    public void BasterFire()
    {
        UpdateResource(-attackResourceHit);
        float clampedResource = Mathf.Clamp(resource, -1, 1);

        float fireDelayMod = clampedResource.Remap(-1, 1, .5f, 2f);
        float tempPowerFireDelayMod = tempPower.Remap(-1, 1, .5f, 2f);
        float attackSpeedPowerupMod = 1;
        if(powerupsDict.ContainsKey(Powerups.FireRate))
        {
            for(int i = 0; i < powerupsDict[Powerups.FireRate]; i++)
                attackSpeedPowerupMod *= 0.75f;
        }
        passiveFireDelay = defaultPassiveFireDelay * fireDelayMod * tempPowerFireDelayMod * attackSpeedPowerupMod;
        nextPassiveFireTime = Time.timeSinceLevelLoad + passiveFireDelay;
        Vector3 mouseWorldPos = cam.ScreenToWorldPoint((Vector3)Mouse.current.position.ReadValue() + Vector3.forward * 10, MonoOrStereoscopicEye.Mono);

        int additionalProj = powerupsDict.ContainsKey(Powerups.Multishot) ? additionalProj = powerupsDict[Powerups.Multishot] : 0;
        float degreesBetweenProj = 120f / (float)(2 + additionalProj);
        bool audioPlayed = false;
        for(int i = 0; i < 1 + additionalProj; i++)
        {
            LaserProjectile proj = laserPooler.pool.Get();
            Vector3 eulerRot = transform.rotation.eulerAngles + new Vector3(0, 0, (degreesBetweenProj * (i + 1)) - 60);
            proj.transform.SetPositionAndRotation(firePoint.position, Quaternion.Euler(eulerRot));

            float speedMod = clampedResource.Remap(-1, 1, 1.75f, 0.75f);
            float tempPowerSpeedMod = tempPower.Remap(-1, 1, 1.75f, 0.25f);
            float projSpeedPowerupMod = 1;
            if(powerupsDict.ContainsKey(Powerups.ProjectileSpeed))
                projSpeedPowerupMod += powerupsDict[Powerups.ProjectileSpeed] * .1f;
            proj.speed = proj.defaultSpeed * speedMod * tempPowerSpeedMod * projSpeedPowerupMod;

            float caliberMod = clampedResource.Remap(-1, 1, 2.5f, 0.333f);
            float tempPowerCaliberMod = tempPower.Remap(-1, 1, 1.9f, 0.1f);
            float caliberPowerupMod = 1;
            if(powerupsDict.ContainsKey(Powerups.Caliber))
                caliberPowerupMod += powerupsDict[Powerups.Caliber] * 0.1f;
            proj.trail.widthMultiplier = proj.defaultWidthMulti * caliberMod * tempPowerCaliberMod * caliberPowerupMod;
            proj.col.size = new Vector2(proj.trail.widthMultiplier, proj.col.size.y);

            float attackMod = clampedResource.Remap(-1, 1, 3f, 0.5f);
            float tempPowerAttackMod = tempPower.Remap(-1, 1, 1.9f, .1f);
            proj.Fire(transform, damage * attackMod * tempPowerAttackMod * caliberPowerupMod, true);
            
            if(!audioPlayed)
            {
                attackAudioSource.PlayOneShot(blasterClip);
                audioPlayed = true;
            }
        }
    }

    public void HealToFull()
    {
        currentHealth = maxHealth;
        onHealthChanged?.Invoke(this, currentHealth);
    }

    public void TakeDamage(float amount, Transform damgedBy)
    {
        if(isDead || isDying)
            return;
        
        amount = Mathf.Abs(amount);

        float oldHP = currentHealth;
        currentHealth = Mathf.Clamp(currentHealth - amount, 0, maxHealth);

        if(currentHealth != oldHP)
        {
            if(damgedBy != transform)
            {
                AudioClip chosenClip = damagedClips.ChooseRandom();
                if(Time.timeSinceLevelLoad >= lastDamagedClipPlayedAt + chosenClip.length)
                {
                    damagedAudioSource.PlayOneShot(chosenClip);
                    lastDamagedClipPlayedAt = Time.timeSinceLevelLoad;
                }
                PooledParticleSystem sparks = sparkPooler.pool.Get();
                sparks.transform.position = transform.position;
                sparks.Play();
            }
            
            onHealthChanged?.Invoke(this, currentHealth);
        }

        if(currentHealth == 0)
            Die();
    }

    public void Die()
    {
        if(primaryFire)
            primaryFire = false;
        
        _isDead = true;

        // play destroyed vfx and audio
        PooledParticleSystem explosion = explosionPooler.pool.Get();
        explosion.transform.position = transform.position;
        explosion.SetSize(1);
        explosion.Play();
        enegineHumAudioSource.Stop();
        
        if(existingLaserBeams != null && existingLaserBeams.Count > 0)
        {
            foreach(LaserBeam existingLaserBeam in existingLaserBeams)
                existingLaserBeam.Fade();
            existingLaserBeams.Clear();
        }

        gameObject.SetActive(false);
        // open endgame panel
        endGamePanel.GameOver(score.current);
    }

    private void UpdateResource(float val)
    {
        resource += val;
        if(resource < 0 && val < 0)
        {
            float difference = resource + 1;
            if(difference < 0)
            {
                TakeDamage(Mathf.Abs(difference) * (maxHealth * 0.6f), transform);
                if(!resourceAlarmAudioSource.isPlaying)
                {
                    resourceAlarmAudioSource.pitch = 0.85f;
                    resourceAlarmAudioSource.Play();
                }
            }
            else if(resourceAlarmAudioSource.isPlaying)
                resourceAlarmAudioSource.Stop();
        }
        else if(resource > 0 && val > 0)
        {
            float difference = resource - 1;
            if(difference > 0)
            {
                TakeDamage(difference * (maxHealth * 0.01f), transform);
                if(!resourceAlarmAudioSource.isPlaying)
                {
                    resourceAlarmAudioSource.pitch = 1.1f;
                    resourceAlarmAudioSource.Play();
                }
            }
            else if(resourceAlarmAudioSource.isPlaying)
                resourceAlarmAudioSource.Stop();
        }
        if(resource > -1 && resource < 1 && resourceAlarmAudioSource.isPlaying)
            resourceAlarmAudioSource.Stop();

        onResourceChanged?.Invoke(resource);
    }
}