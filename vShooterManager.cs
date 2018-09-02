using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Invector;
using Invector.ItemManager;
using Invector.CharacterController;

//[vClassHeader("SHOOTER MANAGER", false, "", false, "shooterIcon")]
public class vShooterManager : vMonoBehaviour
{
    #region variables

    [System.Serializable]
    public class OnReloadWeapon : UnityEngine.Events.UnityEvent<vShooterWeapon> { }
    public delegate void TotalAmmoHandler(int ammoID, ref int ammo);

    [Header("- Float Values")]
    [Tooltip("min distance to aim")]
    public float minDistanceToAim = 1;
    public float checkAimRadius = 0.1f;    
    [Tooltip("smooth of the right hand when correcting the aim")]
    public float smoothHandRotation = 30f;
    [Tooltip("Limit the maxAngle for the right hand to correct the aim")]
    public float maxHandAngle = 60f;
    [Tooltip("Check true to make the character always aim and walk on strafe mode")]

    [Header("- Shooter Settings")]
    public bool alwaysAiming;
    [Tooltip("Check this to syinc the weapon aim to the camera aim")]
    public bool raycastAimTarget = true;   
    [Tooltip("Use the vAmmoDisplay to shot ammo count")]
    public bool useAmmoDisplay = true;
    [Tooltip("Move camera angle when shot using recoil properties of weapon")]
    public bool applyRecoilToCamera = true;
    [Tooltip("Check this to use IK on the left hand")]
    public bool useLeftIK = true;
    [Tooltip("Instead of adjust each weapon individually, make a single offset here for each character")]
    public Vector3 ikRotationOffset;
    [Tooltip("Instead of adjust each weapon individually, make a single offset here for each character")]
    public Vector3 ikPositionOffset;
    [Tooltip("Layer to aim down")]
    public LayerMask blockAimLayer = 1 << 0;
    [Tooltip("Layer to aim")]
    public LayerMask aimTargetLayer = 1 << 0;
    [Tooltip("Tags to the Aim ignore - tag this gameObject to avoid shot on yourself")]
    public List<string> ignoreTags;

    [Header("- LockOn Options")]
    [Tooltip("Allow the use of the LockOn or not")]
    public bool useLockOn = false;
    [Tooltip("Allow the use of the LockOn only with a Melee Weapon")]
    public bool useLockOnMeleeOnly = true;

    [Header("- HipFire Options")]
    [Tooltip("If enable, remember to change your weak attack input to other input - this allows shot without aim")]
    public bool hipfireShot = false;
    [Tooltip("Precision of the weapon when shooting using hipfire (without aiming)")]
    public float hipfireDispersion = 0.5f;

    [Header("- Camera Sway Settings")]
    [Tooltip("Camera Sway movement while aiming")]
    public float cameraMaxSwayAmount = 2f;
    [Tooltip("Camera Sway Speed while aiming")]
    public float cameraSwaySpeed = .5f;

    public vShooterWeapon rWeapon;
    [HideInInspector]
    public vAmmoManager ammoManager;
    public TotalAmmoHandler totalAmmoHandler;
   
    [HideInInspector]
    public OnReloadWeapon onReloadWeapon;
    [HideInInspector]
    public vAmmoDisplay ammoDisplay;
    [HideInInspector]
    public vThirdPersonCamera tpCamera;
    [HideInInspector]
    public bool showCheckAimGizmos;
    
    private Animator animator;
    private int totalAmmo;
    private bool usingThirdPersonController;    
    private float currentShotTime;
    private float hipfirePrecisionAngle;
    private float hipfirePrecision;
    #endregion

    void Start()
    {
        animator = GetComponent<Animator>();
        if (applyRecoilToCamera)
            tpCamera = FindObjectOfType<vThirdPersonCamera>();
        ammoManager = GetComponent<vAmmoManager>();
        usingThirdPersonController = GetComponent<vThirdPersonController>() != null;
        if (useAmmoDisplay)
            ammoDisplay = FindObjectOfType<vAmmoDisplay>();

        if (animator)
        {
            var _rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
            var weapon = _rightHand.GetComponentInChildren<vShooterWeapon>();
            if (weapon != null)
                SetRightWeapon(weapon.gameObject);
        }

        if (!ignoreTags.Contains(gameObject.tag))
            ignoreTags.Add(gameObject.tag);

        if (useAmmoDisplay && ammoDisplay)
            ammoDisplay.UpdateDisplay("");
    }

    public void SetRightWeapon(GameObject weapon)
    {
        if (weapon != null)
        {
            var w = weapon.GetComponent<vShooterWeapon>();
            rWeapon = w;
            if (rWeapon)
            {
                rWeapon.ignoreTags = ignoreTags;
                rWeapon.hitLayer = aimTargetLayer;
                rWeapon.root = transform;
                rWeapon.onDestroy.AddListener(OnDestroyWeapon);
                if (usingThirdPersonController)
                {
                    if (useAmmoDisplay && !ammoDisplay) ammoDisplay = FindObjectOfType<vAmmoDisplay>();
                    if (useAmmoDisplay && ammoDisplay) ammoDisplay.Show();
                    UpdateTotalAmmo();
                }
                currentShotTime = 0;
            }
        }
    }

    public void OnDestroyWeapon(GameObject otherGameObject)
    {
        if (usingThirdPersonController)
        {
            if (useAmmoDisplay && !ammoDisplay) ammoDisplay = FindObjectOfType<vAmmoDisplay>();
            if (useAmmoDisplay && (ammoDisplay && (rWeapon == null || rWeapon.gameObject.Equals(otherGameObject))))
            {
                ammoDisplay.UpdateDisplay("");
                ammoDisplay.Hide();
            }
        }
        currentShotTime = 0;
    }

    public int GetMoveSetID()
    {
        int id = 0;
        if (rWeapon) id = (int)rWeapon.moveSetID;
        return id;
    }

    public int GetUpperBodyID()
    {
        int id = 0;
        if (rWeapon) id = (int)rWeapon.upperBodyID;
        return id;
    }

    public int GetAttackID()
    {
        int id = 0;
        if (rWeapon) id = (int)rWeapon.attackID;
        return id;
    }

    public int GetEquipID()
    {
        int id = 0;
        if (rWeapon) id = (int)rWeapon.equipID;
        return id;
    }

    public int GetReloadID()
    {
        int id = 0;
        if (rWeapon) id = (int)rWeapon.reloadID;
        return id;
    }

    public virtual bool WeaponHasAmmo()
    {
        return totalAmmo > 0;
    }
   
    public bool isShooting
    {
        get { return currentShotTime > 0; }
    }

    public void ReloadWeapon(bool ignoreAmmo = false)
    {
        if (rWeapon == null) return;
        UpdateTotalAmmo();
        if (!ignoreAmmo && (rWeapon.ammoCount >= rWeapon.clipSize || !WeaponHasAmmo())) return;
        onReloadWeapon.Invoke(rWeapon);
        var needAmmo = rWeapon.clipSize - rWeapon.ammoCount;
        if (!ignoreAmmo && WeaponAmmo().count < needAmmo)
            needAmmo = WeaponAmmo().count;

        rWeapon.AddAmmo(needAmmo);
        if(!ignoreAmmo)
           WeaponAmmo().count -= needAmmo;
        if (animator)
        {
            animator.SetInteger("ReloadID", GetReloadID());
            animator.SetTrigger("Reload");
        }

        rWeapon.ReloadEffect();
        UpdateTotalAmmo();
    }

    public vAmmo WeaponAmmo()
    {
        if (rWeapon == null) return null;

        var ammo = new vAmmo();
        if (ammoManager && ammoManager.ammos != null && ammoManager.ammos.Count > 0)
        {
            ammo = ammoManager.ammos.Find(a => a.ammoID == rWeapon.ammoID);
        }

        return ammo;
    }

    public void UpdateTotalAmmo()
    {
        var ammoCount = 0;
        var ammo = WeaponAmmo();
        if (ammo != null) ammoCount += ammo.count;
        if (totalAmmoHandler != null) totalAmmoHandler(rWeapon.ammoID, ref ammoCount);
        totalAmmo = ammoCount;
        if (useAmmoDisplay && !ammoDisplay) ammoDisplay = FindObjectOfType<vAmmoDisplay>();

        if (useAmmoDisplay && ammoDisplay)
            ammoDisplay.UpdateDisplay(string.Format("{0} / {1}", rWeapon.ammoCount, totalAmmo), rWeapon.ammoID);
    }

    public virtual void Shoot(Vector3 aimPosition, bool applyHipfirePrecision = false)
    {
        if (isShooting) return;
        if (rWeapon.ammoCount > 0)
        {
            var _aimPos = applyHipfirePrecision ? aimPosition + HipFirePrecision(aimPosition) : aimPosition;
            rWeapon.ShootEffect(_aimPos, transform);

            if (applyRecoilToCamera)
            {
                var recoilHorizontal = Random.Range(rWeapon.recoilLeft, rWeapon.recoilRight);
                var recoilUp = Random.Range(0, rWeapon.recoilUp);
                StartCoroutine(Recoil(recoilHorizontal, recoilUp));
            }           
           
            if (useAmmoDisplay && !ammoDisplay) ammoDisplay = FindObjectOfType<vAmmoDisplay>();
            if (useAmmoDisplay && ammoDisplay)
                ammoDisplay.UpdateDisplay(string.Format("{0} / {1}", rWeapon.ammoCount, totalAmmo), rWeapon.ammoID);
        }
        else
        {  
            rWeapon.EmptyClipEffect();          
        }
        currentShotTime = rWeapon.shootFrequency;
    }

    IEnumerator Recoil(float horizontal, float up)
    {
        yield return new WaitForSeconds(0.02f);
        if (animator) animator.SetTrigger("Shoot");
        if (tpCamera != null) tpCamera.RotateCamera(horizontal, up);
    }   

    protected virtual Vector3 HipFirePrecision(Vector3 _aimPosition)
    {
        hipfirePrecisionAngle = UnityEngine.Random.Range(-1000, 1000);
        hipfirePrecision = Random.Range(-hipfireDispersion, hipfireDispersion);
        var dir = (Quaternion.AngleAxis(hipfirePrecisionAngle, _aimPosition - rWeapon.muzzle.position) * (Vector3.up)).normalized * hipfirePrecision;        
        return dir;
    }   

    public void CameraSway()
    {
        float bx = (Mathf.PerlinNoise(0, Time.time * cameraSwaySpeed) - 0.5f);
        float by = (Mathf.PerlinNoise(0, (Time.time * cameraSwaySpeed) + 100)) - 0.5f;

        var swayAmount = cameraMaxSwayAmount * (1f - rWeapon.precision);
        if (swayAmount == 0) return;

        bx *= swayAmount;
        by *= swayAmount;

        float tx = (Mathf.PerlinNoise(0, Time.time * cameraSwaySpeed) - 0.5f);
        float ty = ((Mathf.PerlinNoise(0, (Time.time * cameraSwaySpeed) + 100)) - 0.5f);

        tx *= -(swayAmount * 0.25f);
        ty *= (swayAmount * 0.25f);

        vThirdPersonCamera.instance.offsetMouse.x = bx + tx;
        vThirdPersonCamera.instance.offsetMouse.y = by + ty;
    }

    public void UpdateShotTime()
    {
        if (currentShotTime > 0) currentShotTime -= Time.deltaTime;
    }


}

namespace Invector
{
    [System.Serializable]
    public class vAmmo
    {
        [Tooltip("Use the same name of your weapon")]
        public string weaponName;
        [Tooltip("Ammo ID - make sure your AmmoManager and ItemListData use the same ID")]
        public int ammoID;
        [Tooltip("Don't need to setup if you're using a Inventory System")]
        public int count;
        public vAmmo()
        {

        }
        public vAmmo(string weaponName, int ammoID, int count)
        {
            this.weaponName = weaponName;
            this.ammoID = ammoID;
            this.count = count;
        }
        public vAmmo(vAmmo ammo)
        {
            this.weaponName = ammo.weaponName;
            this.ammoID = ammo.ammoID;
            this.count = ammo.count;
        }
    }
}
