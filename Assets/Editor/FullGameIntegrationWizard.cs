using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using ForestSurvival.Combat;
using ForestSurvival.Inventory;
using ForestSurvival.Environment;

namespace ForestSurvival.EditorTools
{
    public static class FullGameIntegrationWizard
    {
        [MenuItem("Tools/Forest Survival/Setup Complete Gameplay World", false, 1)]
        public static void SetupCompleteGameplayWorld()
        {
            Debug.Log("<color=cyan>[Forest Survival]</color> Starting complete game systems integration...");

            SetupEnemyPrefabAndSpawn();
            SetupPlayerWeaponAndAudio();
            SetupInventorySystem();
            SetupDayNightEnvironment();

            // Save Scene
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            Debug.Log("<color=green>[Forest Survival] SUCCESS! Complete gameplay systems, audio, inventory, enemy and weapon configured!</color>");
        }

        private static void SetupPlayerWeaponAndAudio()
        {
            CustomPlayerController player = Object.FindAnyObjectByType<CustomPlayerController>();
            if (player == null)
            {
                Debug.LogWarning("[Forest Survival] No CustomPlayerController found in scene.");
                return;
            }

            Undo.RecordObject(player.gameObject, "Setup Weapon & Audio");

            // Setup Audio Source & Footstep / Attack clips
            AudioSource playerAudio = player.GetComponent<AudioSource>();
            if (playerAudio == null) playerAudio = player.gameObject.AddComponent<AudioSource>();

            // Load Footsteps
            string[] sfxGuids = AssetDatabase.FindAssets("Player_Footstep_ t:AudioClip", new[] { "Assets/StarterAssets" });
            var footstepList = new System.Collections.Generic.List<AudioClip>();
            foreach (var guid in sfxGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                if (clip != null) footstepList.Add(clip);
            }

            // Load swing & jump sounds
            AudioClip jumpClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/StarterAssets/ThirdPersonController/Character/Sfx/Player_Land.wav");
            AudioClip swingClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/FreeWeaponSounds/Handgun/Foley/handgun_weapon_equip.wav");

            SerializedObject soPlayer = new SerializedObject(player);
            SerializedProperty footProp = soPlayer.FindProperty("footstepSounds");
            if (footProp != null)
            {
                footProp.ClearArray();
                for (int i = 0; i < footstepList.Count; i++)
                {
                    footProp.InsertArrayElementAtIndex(i);
                    footProp.GetArrayElementAtIndex(i).objectReferenceValue = footstepList[i];
                }
            }
            if (soPlayer.FindProperty("jumpSound") != null) soPlayer.FindProperty("jumpSound").objectReferenceValue = jumpClip;
            if (soPlayer.FindProperty("attackSwingSound") != null) soPlayer.FindProperty("attackSwingSound").objectReferenceValue = swingClip;

            // Find Right Hand Bone
            Transform rightHand = FindBoneRecursive(player.transform, "RightHand");
            if (rightHand == null) rightHand = FindBoneRecursive(player.transform, "Hand.R");
            if (rightHand == null) rightHand = FindBoneRecursive(player.transform, "hand_r");

            if (rightHand != null)
            {
                // Check if staff already attached
                Transform existingStaff = rightHand.Find("PlayerWeapon_Staff");
                GameObject staffInstance = existingStaff != null ? existingStaff.gameObject : null;

                if (staffInstance == null)
                {
                    GameObject staffPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                        "Assets/3D Game Kit - Character Pack/Characters/Ellen/Weapons/Models/Staff.fbx");
                    if (staffPrefab != null)
                    {
                        staffInstance = Object.Instantiate(staffPrefab, rightHand);
                        staffInstance.name = "PlayerWeapon_Staff";
                        staffInstance.transform.localPosition = new Vector3(0.04f, 0.08f, 0f);
                        staffInstance.transform.localRotation = Quaternion.Euler(-90f, 90f, 0f);
                        staffInstance.transform.localScale = Vector3.one;
                    }
                }

                if (staffInstance != null)
                {
                    // Add BoxCollider for hitbox
                    BoxCollider col = staffInstance.GetComponent<BoxCollider>();
                    if (col == null) col = staffInstance.AddComponent<BoxCollider>();
                    col.isTrigger = true;
                    col.size = new Vector3(0.2f, 1.4f, 0.2f);
                    col.center = new Vector3(0f, 0.6f, 0f);

                    WeaponHitbox hitbox = staffInstance.GetComponent<WeaponHitbox>();
                    if (hitbox == null) hitbox = staffInstance.AddComponent<WeaponHitbox>();

                    AudioClip hitClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/StarterAssets/ThirdPersonController/Character/Sfx/Player_Land_01.wav");
                    SerializedObject soHitbox = new SerializedObject(hitbox);
                    SerializedProperty hitProp = soHitbox.FindProperty("hitSound");
                    if (hitProp != null) hitProp.objectReferenceValue = hitClip;
                    soHitbox.ApplyModifiedProperties();

                    if (soPlayer.FindProperty("equippedWeapon") != null)
                    {
                        soPlayer.FindProperty("equippedWeapon").objectReferenceValue = hitbox;
                    }
                }
            }

            soPlayer.ApplyModifiedProperties();
            Debug.Log("[Forest Survival] Player weapon & footsteps configured.");
        }

        private static void SetupEnemyPrefabAndSpawn()
        {
            // Build Chomper Animator Controller if needed
            string acPath = "Assets/_Project/Chomper_AI_Controller.controller";
            AnimatorController chomperAc = AssetDatabase.LoadAssetAtPath<AnimatorController>(acPath);

            if (chomperAc == null)
            {
                chomperAc = AnimatorController.CreateAnimatorControllerAtPath(acPath);
                chomperAc.AddParameter("Speed", AnimatorControllerParameterType.Float);
                chomperAc.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
                chomperAc.AddParameter("Hit", AnimatorControllerParameterType.Trigger);
                chomperAc.AddParameter("Die", AnimatorControllerParameterType.Trigger);

                // Load Chomper motion clips
                AnimationClip idleClip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/3D Game Kit - Character Pack/Characters/Chomper/@ChomperIdle.fbx");
                AnimationClip runClip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/3D Game Kit - Character Pack/Characters/Chomper/@ChomperRunForward.fbx");
                AnimationClip attackClip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/3D Game Kit - Character Pack/Characters/Chomper/@ChomperAttack.fbx");
                AnimationClip hitClip = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/3D Game Kit - Character Pack/Characters/Chomper/@ChomperHit1.fbx");

                var rootStateMachine = chomperAc.layers[0].stateMachine;

                var idleState = rootStateMachine.AddState("Idle");
                idleState.motion = idleClip;

                var runState = rootStateMachine.AddState("Run");
                runState.motion = runClip;

                var attackState = rootStateMachine.AddState("Attack");
                attackState.motion = attackClip;

                var hitState = rootStateMachine.AddState("Hit");
                hitState.motion = hitClip;

                // Transitions
                var tToRun = idleState.AddTransition(runState);
                tToRun.AddCondition(AnimatorConditionMode.Greater, 0.2f, "Speed");
                tToRun.duration = 0.15f;

                var tToIdle = runState.AddTransition(idleState);
                tToIdle.AddCondition(AnimatorConditionMode.Less, 0.2f, "Speed");
                tToIdle.duration = 0.15f;

                var tAnyAttack = rootStateMachine.AddAnyStateTransition(attackState);
                tAnyAttack.AddCondition(AnimatorConditionMode.If, 0, "Attack");
                tAnyAttack.duration = 0.1f;
                var tAttackExit = attackState.AddTransition(idleState);
                tAttackExit.hasExitTime = true;
                tAttackExit.exitTime = 0.85f;

                var tAnyHit = rootStateMachine.AddAnyStateTransition(hitState);
                tAnyHit.AddCondition(AnimatorConditionMode.If, 0, "Hit");
                tAnyHit.duration = 0.1f;
                var tHitExit = hitState.AddTransition(idleState);
                tHitExit.hasExitTime = true;
                tHitExit.exitTime = 0.8f;
            }

            // Spawn Chomper Enemies around player
            CustomPlayerController player = Object.FindAnyObjectByType<CustomPlayerController>();
            Vector3 center = player != null ? player.transform.position : Vector3.zero;

            GameObject chomperPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/3D Game Kit - Character Pack/Characters/Chomper/Prefabs/Chomper.prefab");

            if (chomperPrefab == null)
            {
                Debug.LogWarning("[Forest Survival] Chomper prefab not found.");
                return;
            }

            // Audio assets for monster
            AudioClip roarAudio = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Monster Roars/10. Attack Roar.wav");
            AudioClip painAudio = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Monster Roars/05. Chilling Roar.wav");
            AudioClip attackAudio = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Monster Roars/06. Battle Roar.wav");

            // Spawn 3 Chomper enemies if none exist
            EnemyAI[] existingEnemies = Object.FindObjectsByType<EnemyAI>(FindObjectsSortMode.None);
            if (existingEnemies.Length == 0)
            {
                Vector3[] offsets = new Vector3[]
                {
                    new Vector3(15f, 0f, 10f),
                    new Vector3(-14f, 0f, 18f),
                    new Vector3(8f, 0f, 25f)
                };

                for (int i = 0; i < offsets.Length; i++)
                {
                    Vector3 spawnPos = center + offsets[i];
                    RaycastHit hit;
                    if (Physics.Raycast(spawnPos + Vector3.up * 50f, Vector3.down, out hit, 100f))
                    {
                        spawnPos.y = hit.point.y + 0.1f;
                    }

                    GameObject enemyObj = PrefabUtility.InstantiatePrefab(chomperPrefab) as GameObject;
                    enemyObj.name = $"Enemy_Chomper_{i + 1}";
                    enemyObj.transform.position = spawnPos;

                    CharacterController cc = enemyObj.GetComponent<CharacterController>();
                    if (cc == null) cc = enemyObj.AddComponent<CharacterController>();
                    cc.center = new Vector3(0, 0.7f, 0);
                    cc.radius = 0.6f;
                    cc.height = 1.4f;

                    EnemyAI enemyAI = enemyObj.GetComponent<EnemyAI>();
                    if (enemyAI == null) enemyAI = enemyObj.AddComponent<EnemyAI>();

                    Animator anim = enemyObj.GetComponentInChildren<Animator>();
                    if (anim != null && chomperAc != null)
                    {
                        anim.runtimeAnimatorController = chomperAc;
                    }

                    SerializedObject soEnemy = new SerializedObject(enemyAI);
                    if (soEnemy.FindProperty("roarSound") != null) soEnemy.FindProperty("roarSound").objectReferenceValue = roarAudio;
                    if (soEnemy.FindProperty("attackSound") != null) soEnemy.FindProperty("attackSound").objectReferenceValue = attackAudio;
                    if (soEnemy.FindProperty("hitSound") != null) soEnemy.FindProperty("hitSound").objectReferenceValue = painAudio;
                    soEnemy.ApplyModifiedProperties();
                }
                Debug.Log("[Forest Survival] 3 Chomper enemies successfully spawned and equipped with Roar audio!");
            }
        }

        private static void SetupInventorySystem()
        {
            SimpleInventory inv = Object.FindAnyObjectByType<SimpleInventory>();
            if (inv == null)
            {
                GameObject invObj = new GameObject("PlayerInventoryManager", typeof(SimpleInventory));
                AudioClip pickupAudio = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/FreeWeaponSounds/Handgun/Foley/03_insert_the_mag_handgun_reload_1.wav");
                SerializedObject soInv = new SerializedObject(invObj.GetComponent<SimpleInventory>());
                if (soInv.FindProperty("itemPickupSound") != null)
                {
                    soInv.FindProperty("itemPickupSound").objectReferenceValue = pickupAudio;
                }
                soInv.ApplyModifiedProperties();
            }
            Debug.Log("[Forest Survival] Quick Inventory bar configured.");
        }

        private static void SetupDayNightEnvironment()
        {
            DayNightCycle dnc = Object.FindAnyObjectByType<DayNightCycle>();
            if (dnc == null)
            {
                GameObject dncObj = new GameObject("DayNightCycleController", typeof(DayNightCycle));
            }
            Debug.Log("[Forest Survival] Day/Night environment controller set up.");
        }

        private static Transform FindBoneRecursive(Transform parent, string boneName)
        {
            if (parent.name.Equals(boneName, System.StringComparison.OrdinalIgnoreCase))
                return parent;

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform found = FindBoneRecursive(parent.GetChild(i), boneName);
                if (found != null) return found;
            }
            return null;
        }
    }
}
