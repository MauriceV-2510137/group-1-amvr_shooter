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

    //??
    public void SetBulletSpawnLocalPosition(Vector3 localPosition)
    {
        if (bulletSpawnPos != null)
        {
            bulletSpawnPos.localPosition = localPosition;
        }
    }


    public bool TryGetMuzzleRay(out Ray ray)
    {
        if (bulletSpawnPos == null)
        {
            ray = default;
            return false;
        }

        ray = new Ray(bulletSpawnPos.position, bulletSpawnPos.forward);
        return true;
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
        ActivateMenuButtonAlongMuzzle(); //??
        SpawnBullet();
    }

    private void ActivateMenuButtonAlongMuzzle() //??
    {
        if (!TryGetMuzzleRay(out Ray ray))
        {
            return;
        }

        if (Physics.Raycast(ray, out RaycastHit hit, 20f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide))
        {
            VRShootableButton shootableButton = hit.collider.GetComponentInParent<VRShootableButton>();
            if (shootableButton != null)
            {
                shootableButton.Activate();
            }
        }
    }

    private void PlayShootSound()
    {
        if (!audioSource || !shotSound) return;
        audioSource.PlayOneShot(shotSound);
    }

    private void SpawnBullet()
    {
        if (!bulletSpawnPos || !bulletPrefab) return;
        Instantiate(bulletPrefab, bulletSpawnPos.position, bulletSpawnPos.rotation);
    }
}
