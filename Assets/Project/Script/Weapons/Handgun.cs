using UnityEngine;
using UnityEngine.InputSystem;

public class Handgun : MonoBehaviour
{
    [Header("Shooting")]
    [SerializeField] private Transform bulletSpawnPos = null;
    [SerializeField] private GameObject bulletPrefab = null;

    [Header("Input")]
    [SerializeField] private InputActionReference shootInput = null;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource = null;
    [SerializeField] private AudioClip shotSound = null;

    private bool isHeld = false;

    private void Awake()
    {
        if(bulletSpawnPos == null)
            Debug.LogWarning("Handgun:: bulletSpawnPos not set!");
        if(bulletPrefab == null)
            Debug.LogWarning("Handgun:: bulletPrefab not set!");
        if(shootInput == null)
            Debug.LogWarning("Handgun:: shootinput not set!");
        if(audioSource == null)
            Debug.LogWarning("Handgun:: audioSource not set!");
        if(shotSound == null)
            Debug.LogWarning("Handgun:: shotSound not set!");
    }

    public void SetHeld(bool held)
    {
        isHeld = held;

        if (shootInput == null) return;

        if (isHeld)
        {
            shootInput.action.Enable();
            shootInput.action.performed += OnTryShoot;
        }
        else
        {
            shootInput.action.performed -= OnTryShoot;
            shootInput.action.Disable();
        }
    }

    private void OnDestroy()
    {
        if (shootInput != null)
        {
            shootInput.action.performed -= OnTryShoot;
            shootInput.action.Disable();
        }
    }

    private void OnTryShoot(InputAction.CallbackContext _)
    {
        if (!isHeld) return;
        Shoot();
    }

    private void Shoot()
    {
        PlayShootSound();
        SpawnBullet();
    }

    private void PlayShootSound()
    {
        if (!audioSource) return;
        audioSource.PlayOneShot(shotSound);
    }

    private void SpawnBullet()
    {
        if (!bulletSpawnPos || !bulletPrefab) return;
        Instantiate(bulletPrefab, bulletSpawnPos.position, bulletSpawnPos.rotation);
    }
}
