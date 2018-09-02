using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Invector;
using Invector.CharacterController;
using Invector.IK;

[vClassHeader("SHOOTER/MELEE INPUT", iconName = "inputIcon")]
public class vShooterMeleeInput : vMeleeCombatInput
{
    #region Shooter Inputs

    [vEditorToolbar("Inputs")]
    [Header("Shooter Inputs")]
    public GenericInput aimInput = new GenericInput("Mouse1", false, "LT", true, "LT", false);
    public GenericInput shotInput = new GenericInput("Mouse0", false, "RT", true, "RT", false);
    public GenericInput reloadInput = new GenericInput("R", "LB", "LB");
    public GenericInput switchCameraSideInput = new GenericInput("Tab", "RightStickClick", "RightStickClick");
    public GenericInput switchScopeViewInput = new GenericInput("Z", "RB", "RB");

    #endregion

    #region Shooter Variables       

    [HideInInspector]
    public vShooterManager shooterManager;
    [HideInInspector]
    public bool blockAim;
    [HideInInspector]
    public bool isAiming;
    [HideInInspector]
    public bool canEquip;
    [HideInInspector]
    public bool isReloading;
    [HideInInspector]
    public bool isEquipping;
    [HideInInspector]
    public Transform leftHand, rightHand, rightUpperArm;
    [HideInInspector]
    public Vector3 aimPosition;
    protected int onlyArmsLayer;
    protected bool allowAttack;
    protected bool aimConditions;
    protected bool isUsingScopeView;
    protected bool isCameraRightSwitched;
    protected float onlyArmsLayerWeight;
    protected float lIKWeight;
    protected float rightRotationWeight;
    protected float aimWeight;
    protected float aimTimming;
    protected float lastAimDistance;
    protected Quaternion handRotation, upperArmRotation;
    protected vIKSolver leftIK;

    protected vHeadTrack headTrack;
    private vControlAimCanvas _controlAimCanvas;
    private GameObject aimAngleReference;
    public vControlAimCanvas controlAimCanvas
    {
        get
        {
            if (!_controlAimCanvas)
                _controlAimCanvas = FindObjectOfType<vControlAimCanvas>();
            return _controlAimCanvas;
        }
    }

    [HideInInspector]
    public bool lockShooterInput;

    public void SetLockShooterInput(bool value)
    {
        lockShooterInput = value;
    }
    #endregion

    protected override void Start()
    {
        shooterManager = GetComponent<vShooterManager>();

        base.Start();

        leftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
        rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
        rightUpperArm = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
        onlyArmsLayer = animator.GetLayerIndex("OnlyArms");
        aimAngleReference = new GameObject("aimAngleReference");
        aimAngleReference.transform.rotation = transform.rotation;
        var chest = animator.GetBoneTransform(HumanBodyBones.Chest);
        aimAngleReference.transform.SetParent(chest);
        aimAngleReference.transform.localPosition = Vector3.zero;

        headTrack = GetComponent<vHeadTrack>();

        if (!controlAimCanvas)
            Debug.LogWarning("Missing the AimCanvas, drag and drop the prefab to this scene in order to Aim", gameObject);
    }

    protected override void LateUpdate()
    {
        if ((!updateIK && animator.updateMode == AnimatorUpdateMode.AnimatePhysics)) return;
        base.LateUpdate();
        UpdateAimBehaviour();
    }

    #region Shooter Inputs    

    protected override void InputHandle()
    {
        if (cc == null || lockInput)
            return;

        #region MeleeInput

        if (MeleeAttackConditions && !isAiming && !isReloading && !lockInputByItemManager && !lockMeleeInput)
        {
            MeleeWeakAttackInput();
            MeleeStrongAttackInput();
            BlockingInput();
        }
        else
            isBlocking = false;

        #endregion

        #region BasicInput

        if (!isAttacking)
        {
            if (!cc.lockMovement && !cc.ragdolled)
            {
                MoveCharacter();
                SprintInput();
                CrouchInput();
                StrafeInput();
                JumpInput();
                RollInput();
            }

            UpdateMeleeAnimations();
        }
        else
            cc.input = Vector2.zero;

        #endregion

        #region ShooterInput

        if (lockShooterInput)
        {
            isAiming = false;
        }
        else
        {
            if (shooterManager == null || shooterManager.rWeapon == null || !shooterManager.rWeapon.gameObject.activeInHierarchy || lockInputByItemManager)
            {
                isAiming = false;
                if (controlAimCanvas != null)
                {
                    controlAimCanvas.SetActiveAim(false);
                    controlAimCanvas.SetActiveScopeCamera(false);
                }

            }
            else
            {
                AimInput();
                ReloadInput();
                SwitchCameraSide();
                SwitchScopeViewInput();
            }
        }
        onUpdateInput.Invoke(this);
        #endregion
    }

    public override bool lockInventory
    {
        get
        {
            return base.lockInventory || isReloading;
        }
    }

    protected virtual void AimInput()
    {
        if (blockAim)
        {
            isAiming = false;
            return;
        }
        if (cc.locomotionType == vThirdPersonMotor.LocomotionType.OnlyFree)
        {
            Debug.LogWarning("Shooter behaviour needs to be OnlyStrafe or Free with Strafe. \n Please change the Locomotion Type.");
            return;
        }

        if(shooterManager.hipfireShot)
        {
            if (aimTimming > 0)
                aimTimming -= Time.deltaTime;           
        }        

        if (!shooterManager || shooterManager.rWeapon == null)
        {
            if (controlAimCanvas)
            {
                controlAimCanvas.SetActiveAim(false);
                controlAimCanvas.SetActiveScopeCamera(false);
            }
            isAiming = false;
            return;
        }

        if (!cc.isRolling)
            isAiming = !isReloading && (aimInput.GetButton() || (shooterManager.alwaysAiming)) && !cc.actions && !cc.customAction || (cc.actions && cc.isJumping);

        if (headTrack)
            headTrack.awaysFollowCamera = isAiming;

        if (cc.locomotionType == vThirdPersonMotor.LocomotionType.FreeWithStrafe)
        {
            if ((isAiming || aimTimming > 0) && !cc.isStrafing)
            {
                cc.Strafe();
            }
            else if ((!isAiming && aimTimming <= 0) && cc.isStrafing)
            {
                cc.Strafe();
            }
        }

        if (controlAimCanvas)
        {
            if ((isAiming || aimTimming > 0) && !controlAimCanvas.isAimActive)
                controlAimCanvas.SetActiveAim(true);
            if ((!isAiming && aimTimming <= 0) && controlAimCanvas.isAimActive)
                controlAimCanvas.SetActiveAim(false);
        }

        shooterManager.rWeapon.SetActiveAim(isAiming && aimConditions);
        shooterManager.rWeapon.SetActiveScope(isAiming && isUsingScopeView);
    }

    protected virtual void ShotInput()
    {
        if (!shooterManager || shooterManager.rWeapon == null) return;

        if ((isAiming && !shooterManager.hipfireShot || shooterManager.hipfireShot) && !shooterManager.isShooting && aimConditions && !isReloading && !isAttacking)
        {
            if (shooterManager.rWeapon.automaticWeapon ? shotInput.GetButton() : shotInput.GetButtonDown())
            {
                if (shooterManager.hipfireShot) aimTimming = 3f;
                shooterManager.Shoot(aimPosition, !isAiming);
            }
            else if (shotInput.GetButtonDown())
            {
                if (allowAttack == false)
                {
                    if (shooterManager.hipfireShot) aimTimming = 1f;
                    shooterManager.Shoot(aimPosition, !isAiming);
                    allowAttack = true;
                }
            }
            else allowAttack = false;
        }
        shooterManager.UpdateShotTime();
    }

    protected virtual void ReloadInput()
    {
        if (!shooterManager || shooterManager.rWeapon == null) return;
        if (reloadInput.GetButtonDown() && !cc.actions && !cc.ragdolled)
            shooterManager.ReloadWeapon();
    }

    protected virtual void SwitchCameraSide()
    {
        if (tpCamera == null) return;
        if (switchCameraSideInput.GetButtonDown())
        {
            isCameraRightSwitched = !isCameraRightSwitched;
            tpCamera.SwitchRight(isCameraRightSwitched);
        }
    }

    protected virtual void SwitchScopeViewInput()
    {
        if (!shooterManager || shooterManager.rWeapon == null) return;
        if (isAiming && aimConditions && switchScopeViewInput.GetButtonDown())
        {
            if (controlAimCanvas && shooterManager.rWeapon.scopeTarget)
            {
                isUsingScopeView = !isUsingScopeView;
                controlAimCanvas.SetActiveScopeCamera(isUsingScopeView, shooterManager.rWeapon.useUI);
            }
        }
        else if (controlAimCanvas && !isAiming || controlAimCanvas && !aimConditions)
        {
            isUsingScopeView = false;
            controlAimCanvas.SetActiveScopeCamera(false);
        }
    }

    protected override void BlockingInput()
    {
        if (shooterManager == null || shooterManager.rWeapon == null)
            base.BlockingInput();
    }

    protected override void RotateWithCamera(Transform cameraTransform)
    {
        if (cc.isStrafing && !cc.actions && !cc.lockMovement && rotateToCameraWhileStrafe)
        {
            // smooth align character with aim position
            if (tpCamera != null && tpCamera.lockTarget)
            {
                cc.RotateToTarget(tpCamera.lockTarget);
            }
            // rotate the camera around the character and align with when the char move
            else if (cc.input != Vector2.zero || (isAiming || aimTimming > 0))
            {
                cc.RotateWithAnotherTransform(cameraTransform);
            }
        }
    }

    #endregion

    #region Update Animations

    protected override void UpdateMeleeAnimations()
    {
        // disable the onlyarms layer and run the melee methods if the character is not using any shooter weapon
        if (!animator) return;
        // update MeleeManager Animator Properties
        if ((shooterManager == null || shooterManager.rWeapon == null) && meleeManager)
        {
            base.UpdateMeleeAnimations();
            // set the uppbody id (armsonly layer)
            animator.SetFloat("UpperBody_ID", 0, .2f, Time.deltaTime);
            // turn on the onlyarms layer to aim 
            onlyArmsLayerWeight = Mathf.Lerp(onlyArmsLayerWeight, 0, 6f * Time.deltaTime);
            animator.SetLayerWeight(onlyArmsLayer, onlyArmsLayerWeight);
            // reset aiming parameter
            animator.SetBool("IsAiming", false);
            isReloading = false;

        }
        // update ShooterManager Animator Properties
        else if (shooterManager && shooterManager.rWeapon)
            UpdateShooterAnimations();
        // reset Animator Properties
        else
        {
            // set the move set id (base layer) 
            animator.SetFloat("MoveSet_ID", 0, .2f, Time.deltaTime);
            // set the uppbody id (armsonly layer)
            animator.SetFloat("UpperBody_ID", 0, .2f, Time.deltaTime);
            // set if the character can aim or not (upperbody layer)
            animator.SetBool("CanAim", false);
            // character is aiming
            animator.SetBool("IsAiming", false);
            // turn on the onlyarms layer to aim 
            onlyArmsLayerWeight = Mathf.Lerp(onlyArmsLayerWeight, 0, 6f * Time.deltaTime);
            animator.SetLayerWeight(onlyArmsLayer, onlyArmsLayerWeight);
        }
    }

    protected virtual void UpdateShooterAnimations()
    {
        if (shooterManager == null) return;

        if ((!isAiming && aimTimming <= 0) && meleeManager)
        {
            // set attack id from the melee weapon (trigger fullbody atk animations)
            animator.SetInteger("AttackID", meleeManager.GetAttackID());
        }
        else
        {
            // set attack id from the shooter weapon (trigger shot layer animations)
            animator.SetInteger("AttackID", shooterManager.GetAttackID());
        }
        // turn on the onlyarms layer to aim 
        onlyArmsLayerWeight = Mathf.Lerp(onlyArmsLayerWeight, (isAiming || aimTimming > 0) ? 0f : shooterManager.rWeapon ? 1f : 0f, 6f * Time.deltaTime);
        animator.SetLayerWeight(onlyArmsLayer, onlyArmsLayerWeight);

        if (shooterManager.rWeapon != null && (isAiming || aimTimming > 0))
        {
            // set the move set id (base layer) 
            animator.SetFloat("MoveSet_ID", shooterManager.GetMoveSetID(), .2f, Time.deltaTime);
        }
        else if (shooterManager.rWeapon != null)
        {
            // set the move set id (base layer) 
            animator.SetFloat("MoveSet_ID", 0, .2f, Time.deltaTime);
        }
        // set the uppbody id (armsonly layer)
        animator.SetFloat("UpperBody_ID", shooterManager.GetUpperBodyID(), .2f, Time.deltaTime);
        // set if the character can aim or not (upperbody layer)
        animator.SetBool("CanAim", aimConditions);
        // character is aiming
        animator.SetBool("IsAiming", (isAiming || aimTimming > 0) && !isAttacking);
        // find states with the Reload tag
        isReloading = cc.IsAnimatorTag("Reload");
        // find states with the IsEquipping tag
        isEquipping = cc.IsAnimatorTag("IsEquipping");
    }

    protected override void UpdateCameraStates()
    {
        // CAMERA STATE - you can change the CameraState here, the bool means if you want lerp of not, make sure to use the same CameraState String that you named on TPCameraListData

        if (tpCamera == null)
        {
            tpCamera = FindObjectOfType<vThirdPersonCamera>();
            if (tpCamera == null)
                return;
            if (tpCamera)
            {
                tpCamera.SetMainTarget(this.transform);
                tpCamera.Init();
            }
        }

        if (changeCameraState)
            tpCamera.ChangeState(customCameraState, customlookAtPoint, true);
        else if (cc.isCrouching)
            tpCamera.ChangeState("Crouch", true);
        else if (cc.isStrafing && !isAiming)
            tpCamera.ChangeState("Strafing", true);
        else if (isAiming && shooterManager.rWeapon != null)        
            tpCamera.ChangeState("Aiming", true);
        else
            tpCamera.ChangeState("Default", true);
    }

    #endregion

    #region Update Aim

    protected virtual void UpdateAimPosition()
    {
        if (!shooterManager || shooterManager.rWeapon == null) return;

        var camT = isUsingScopeView && controlAimCanvas && controlAimCanvas.scopeCamera ? //Check if is using canvas scope view
                shooterManager.rWeapon.zoomScopeCamera ? /* if true, check if weapon has a zoomScopeCamera, 
                if true...*/
                shooterManager.rWeapon.zoomScopeCamera.transform : controlAimCanvas.scopeCamera.transform :
                /*else*/Camera.main.transform;

        var origin1 = camT.position;
        if (!(controlAimCanvas && controlAimCanvas.isScopeCameraActive && controlAimCanvas.scopeCamera))
            origin1 = camT.position;

        var vOrigin = origin1;
        vOrigin += controlAimCanvas && controlAimCanvas.isScopeCameraActive && controlAimCanvas.scopeCamera ? camT.forward : Vector3.zero;
        aimPosition = camT.position + camT.forward * 100f;
        if (!isUsingScopeView) lastAimDistance = 100f;

        if (shooterManager.raycastAimTarget && shooterManager.rWeapon.raycastAimTarget)
        {
            RaycastHit hit;
            Ray ray = new Ray(vOrigin, camT.forward);

            if (Physics.Raycast(ray, out hit, Camera.main.farClipPlane, shooterManager.aimTargetLayer))
            {
                if (hit.collider.transform.IsChildOf(transform))
                {
                    var collider = hit.collider;
                    var hits = Physics.RaycastAll(ray, Camera.main.farClipPlane, shooterManager.aimTargetLayer);
                    var dist = Camera.main.farClipPlane;
                    for (int i = 0; i < hits.Length; i++)
                    {
                        if (hits[i].distance < dist && hits[i].collider.gameObject != collider.gameObject && !hits[i].collider.transform.IsChildOf(transform))
                        {
                            dist = hits[i].distance;
                            hit = hits[i];
                        }
                    }
                }

                if (hit.collider)
                {
                    if (!isUsingScopeView)
                        lastAimDistance = Vector3.Distance(camT.position, hit.point);
                    aimPosition = hit.point;

                }
            }
            if (shooterManager.showCheckAimGizmos)
            {
                Debug.DrawLine(ray.origin, aimPosition);
            }            
        }
        if (isAiming)
            shooterManager.CameraSway();
    }  

    #endregion

    #region IK behaviour

    void OnDrawGizmos()
    {
        if (!shooterManager || shooterManager.rWeapon == null) return;

        var _ray = new Ray(rightUpperArm.position, aimPosition - (rightUpperArm.position));
        Gizmos.DrawRay(_ray.origin, _ray.direction * shooterManager.minDistanceToAim);
        var color = Gizmos.color;
        color = aimConditions ? Color.green : Color.red;
        color.a = 0.2f;
        Gizmos.color = color;
        Gizmos.DrawSphere(_ray.GetPoint(shooterManager.minDistanceToAim), shooterManager.checkAimRadius);
        Gizmos.DrawSphere(aimPosition, shooterManager.checkAimRadius);
    }

    protected virtual void UpdateAimBehaviour()
    {
        UpdateAimPosition();
        UpdateHeadTrack();
        RotateRightArm();
        RotateRightHand();
        UpdateLeftIK();
        if (isUsingScopeView && controlAimCanvas && controlAimCanvas.scopeCamera) UpdateAimPosition();
        CheckAimConditions();
        UpdateAimHud();
        ShotInput();
    }

    protected virtual void UpdateHeadTrack()
    {
        if (!shooterManager || !shooterManager.rWeapon || !shooterManager.rWeapon.gameObject.activeInHierarchy || !headTrack)
        {
            if (headTrack) headTrack.offsetSpine = Vector2.Lerp(headTrack.offsetSpine, Vector2.zero, headTrack.smooth * Time.deltaTime);
            return;
        }
        if (isAiming || aimTimming > 0f)
        {
            var offset = cc.isCrouching ? shooterManager.rWeapon.headTrackOffsetCrouch : shooterManager.rWeapon.headTrackOffset;
            headTrack.offsetSpine = Vector2.Lerp(headTrack.offsetSpine, offset, headTrack.smooth * Time.deltaTime);
        }
        else
        {
            headTrack.offsetSpine = Vector2.Lerp(headTrack.offsetSpine, Vector2.zero, headTrack.smooth * Time.deltaTime);
        }
    }

    protected virtual void UpdateLeftIK()
    {
        if (!shooterManager || !shooterManager.rWeapon || !shooterManager.rWeapon.gameObject.activeInHierarchy || !shooterManager.useLeftIK) return;
        bool useIkOnIdle = cc.input.magnitude < 0.1f ? shooterManager.rWeapon.useIkOnIdle : true;
        bool useIkStrafeMoving = new Vector2(animator.GetFloat("InputVertical"), animator.GetFloat("InputHorizontal")).magnitude > 0.1f && cc.isStrafing ? shooterManager.rWeapon.useIkOnStrafe : true;
        bool useIkFreeMoving = animator.GetFloat("InputVertical") > 0.1f && !cc.isStrafing ? shooterManager.rWeapon.useIkOnFree : true;
        bool useIkAttacking = isAttacking ? shooterManager.rWeapon.useIkAttacking : true;
        bool useIkConditions = !(!useIkOnIdle || !useIkStrafeMoving || !useIkFreeMoving || !useIkAttacking);

        // create left arm ik solver if equal null
        if (leftIK == null) leftIK = new vIKSolver(animator, AvatarIKGoal.LeftHand);
        if (leftIK != null)
        {
            // control weight of ik
            if (shooterManager.rWeapon && shooterManager.rWeapon.handIKTarget && Time.timeScale > 0 && !isReloading && !cc.actions && !cc.customAction && !isEquipping && (cc.isGrounded || (isAiming || aimTimming > 0f)) && !cc.lockMovement && useIkConditions)
                lIKWeight = Mathf.Lerp(lIKWeight, 1, 10f * Time.deltaTime);
            else
                lIKWeight = Mathf.Lerp(lIKWeight, 0, 10f * Time.deltaTime);

            if (lIKWeight <= 0) return;
            // update IK
            leftIK.SetIKWeight(lIKWeight);
            if (shooterManager && shooterManager.rWeapon && shooterManager.rWeapon.handIKTarget)
            {
                var _offset = (shooterManager.rWeapon.handIKTarget.forward * shooterManager.ikPositionOffset.z) + (shooterManager.rWeapon.handIKTarget.right * shooterManager.ikPositionOffset.x) + (shooterManager.rWeapon.handIKTarget.up * shooterManager.ikPositionOffset.y);
                leftIK.SetIKPosition(shooterManager.rWeapon.handIKTarget.position + _offset);
                var _rotation = Quaternion.Euler(shooterManager.ikRotationOffset);
                leftIK.SetIKRotation(shooterManager.rWeapon.handIKTarget.rotation * _rotation);
            }
        }
    }

    protected virtual void CheckAimConditions()
    {
        if (!shooterManager || shooterManager.rWeapon == null || !shooterManager.rWeapon.gameObject.activeInHierarchy) return;

        if (!shooterManager.hipfireShot && !IsAimAlignWithForward())
        {
            aimConditions = false;
        }
        else
        {
            var _ray = new Ray(rightUpperArm.position, Camera.main.transform.forward);
            RaycastHit hit;
            if (Physics.SphereCast(_ray, shooterManager.checkAimRadius, out hit, shooterManager.minDistanceToAim, shooterManager.blockAimLayer))
            {
                aimConditions = false;
            }
            else
                aimConditions = true;
        }

        aimWeight = Mathf.Lerp(aimWeight, aimConditions ? 1 : 0, 10 * Time.deltaTime);
    }

    protected virtual bool IsAimAlignWithForward()
    {
        var angle = Quaternion.LookRotation(aimPosition - shooterManager.rWeapon.muzzle.position, Vector3.up).eulerAngles - transform.eulerAngles;
        return ((angle.NormalizeAngle().y < 90 && angle.NormalizeAngle().y > -90));
    }

    protected virtual void RotateRightArm()
    {
        if (shooterManager && shooterManager.rWeapon && shooterManager.rWeapon.gameObject.activeInHierarchy && (isAiming || aimTimming > 0f) && aimConditions && shooterManager.rWeapon.alignRightUpperArmToAim)
        {
            var aimPoint = targetArmAlignmentPosition;
            Vector3 v = aimPoint - shooterManager.rWeapon.aimReference.position;
            Vector3 v2 = Quaternion.AngleAxis(-shooterManager.rWeapon.recoilUp, shooterManager.rWeapon.aimReference.right) * v;
            var orientation = shooterManager.rWeapon.aimReference.forward;

            rightRotationWeight = Mathf.Lerp(rightRotationWeight, !shooterManager.isShooting || shooterManager.rWeapon.ammoCount <= 0 ? 1f * aimWeight : 0f, 1f * Time.deltaTime);

            var r = Quaternion.FromToRotation(orientation, v) * rightUpperArm.rotation;
            var r2 = Quaternion.FromToRotation(orientation, v2) * rightUpperArm.rotation;
            Quaternion rot = Quaternion.Lerp(r2, r, rightRotationWeight);
            var angle = Vector3.Angle(aimPosition - shooterManager.rWeapon.muzzle.position, aimAngleReference.transform.forward);
            if ((!(angle > shooterManager.maxHandAngle || angle < -shooterManager.maxHandAngle)) || controlAimCanvas && controlAimCanvas.isScopeCameraActive)
                upperArmRotation = Quaternion.Lerp(upperArmRotation, rot, shooterManager.smoothHandRotation * Time.deltaTime);
            else upperArmRotation = rightUpperArm.rotation;
            if (!float.IsNaN(upperArmRotation.x) && !float.IsNaN(upperArmRotation.y) && !float.IsNaN(upperArmRotation.z))
                rightUpperArm.rotation = upperArmRotation;
        }
    }

    protected virtual void RotateRightHand()
    {
        if (shooterManager && shooterManager.rWeapon && shooterManager.rWeapon.gameObject.activeInHierarchy && shooterManager.rWeapon.alignRightHandToAim && (isAiming || aimTimming > 0f) && aimConditions)
        {
            var aimPoint = targetArmAlignmentPosition;
            Vector3 v = aimPoint - shooterManager.rWeapon.aimReference.position;
            Vector3 v2 = Quaternion.AngleAxis(-shooterManager.rWeapon.recoilUp, shooterManager.rWeapon.aimReference.right) * v;
            var orientation = shooterManager.rWeapon.aimReference.forward;
            if (!shooterManager.rWeapon.alignRightUpperArmToAim)
                rightRotationWeight = Mathf.Lerp(rightRotationWeight, !shooterManager.isShooting || shooterManager.rWeapon.ammoCount <= 0 ? 1f * aimWeight : 0f, 1f * Time.deltaTime);
            var r = Quaternion.FromToRotation(orientation, v) * rightHand.rotation;
            var r2 = Quaternion.FromToRotation(orientation, v2) * rightHand.rotation;
            Quaternion rot = Quaternion.Lerp(r2, r, rightRotationWeight);
            var angle = Vector3.Angle(aimPosition - shooterManager.rWeapon.muzzle.position, aimAngleReference.transform.forward);
            if ((!(angle > shooterManager.maxHandAngle || angle < -shooterManager.maxHandAngle)) || (controlAimCanvas && controlAimCanvas.isScopeCameraActive))
                handRotation = Quaternion.Lerp(handRotation, rot, shooterManager.smoothHandRotation * Time.deltaTime);
            else handRotation = Quaternion.Lerp(rightHand.rotation, rot, shooterManager.smoothHandRotation * Time.deltaTime); ;
            if (!float.IsNaN(handRotation.x) && !float.IsNaN(handRotation.y) && !float.IsNaN(handRotation.z))
                rightHand.rotation = handRotation;
            shooterManager.rWeapon.SetScopeLookTarget(aimPoint);
        }
    }

    protected virtual Vector3 targetArmAlignmentPosition
    {
        get
        {
            return isUsingScopeView && controlAimCanvas.scopeCamera ? Camera.main.transform.position + Camera.main.transform.forward * lastAimDistance : aimPosition;
        }
    }

    protected virtual Vector3 targetArmAligmentDirection
    {
        get
        {
            var t = controlAimCanvas && controlAimCanvas.isScopeCameraActive && controlAimCanvas.scopeCamera ? controlAimCanvas.scopeCamera.transform : Camera.main.transform;
            return t.forward;
        }
    }

    protected virtual void UpdateAimHud()
    {
        if (!shooterManager || shooterManager.rWeapon == null || !shooterManager.rWeapon.gameObject.activeInHierarchy || !controlAimCanvas) return;

        controlAimCanvas.SetAimCanvasID(shooterManager.rWeapon.scopeID);
        if (controlAimCanvas.scopeCamera && controlAimCanvas.scopeCamera.gameObject.activeSelf)
            controlAimCanvas.SetAimToCenter(true);
        else if (isAiming)
        {
            RaycastHit hit;
            if (Physics.Linecast(shooterManager.rWeapon.muzzle.position, aimPosition, out hit, shooterManager.blockAimLayer))
                controlAimCanvas.SetWordPosition(hit.point, aimConditions);
            else
                controlAimCanvas.SetWordPosition(aimPosition, aimConditions);

        }
        else
            controlAimCanvas.SetAimToCenter(true);

        if (shooterManager.rWeapon.scopeTarget)
        {
            var lookPoint = Camera.main.transform.position + (Camera.main.transform.forward * (isUsingScopeView ? lastAimDistance : 100f));
            controlAimCanvas.UpdateScopeCamera(shooterManager.rWeapon.scopeTarget.position, lookPoint, shooterManager.rWeapon.zoomScopeCamera ? 0 : shooterManager.rWeapon.scopeZoom);
        }
    }

    #endregion

}