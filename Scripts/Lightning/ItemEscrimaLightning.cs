using System.Collections.Generic;
using BS;
using DigitalRuby.ThunderAndLightning;
using UnityEngine;

namespace EscrimaLightning
{
    public class ItemEscrimaLightning : MonoBehaviour
    {
        protected Item thisStick;
        private static Item stickOne;
        private static Item stickTwo;
        public ItemModuleEscrimaLightning module;

        private static bool canActivate = true;
        private static bool isActivated = false;

        private LightningBoltPathScript boltScript;
        private List<LightningBoltPathScript> bolts;

        private Vector3 contactPoint;
        private GameObject contactObject;
        private AudioSource beamsSound;

        protected void Awake()
        {
            thisStick = GetComponent<Item>();
            if (stickOne == null) {
                stickOne = thisStick;
            }
            else if (stickTwo == null)
            {
                stickTwo = thisStick;
            }
            else
            {
                Debug.LogError("ERROR: More than two sticks are spawned. Some features will not work.");
            }
            
            thisStick.OnCollisionEvent += OnEscrimaCollision;
            module = thisStick.data.GetModule<ItemModuleEscrimaLightning>();

            boltScript = thisStick.GetComponentInChildren<LightningBoltPathScript>();
            bolts = new List<LightningBoltPathScript>();

            contactObject = new GameObject();
            beamsSound = thisStick.transform.Find("Lightning").GetComponentInChildren<AudioSource>();
            beamsSound.transform.parent = contactObject.transform;
        }

        private void OnEscrimaCollision(ref CollisionStruct collisionInstance)
        {
            string sourceColliderGroup = collisionInstance.sourceColliderGroup.name;
            string targetColliderGroup = "";
            if (collisionInstance.targetColliderGroup != null)
            {
                targetColliderGroup = collisionInstance.targetColliderGroup.name;
            }

            if (sourceColliderGroup.Equals(targetColliderGroup) && sourceColliderGroup.Contains("ShockerColliders"))
            {
                if (canActivate && !isActivated) Lightning();
            }
        }

        protected void OnCollisionExit(Collision collisionInfo)
        {
            if (isActivated)
            {
                CancelInvoke("StopLightning");
                StopLightning();
            }
        }

        protected void Update()
        {
            if (stickTwo != null)
            {
                Transform stickOneTip = stickOne.transform.Find("FlyRef");
                Transform stickTwoTip = stickTwo.transform.Find("FlyRef");
                contactPoint = Vector3.Lerp(stickOneTip.position, stickTwoTip.position, 0.5f);
                contactObject.transform.position = contactPoint;
            }
        }

        private void Lightning()
        {
            foreach (Creature npc in Creature.list)
            {
                if (npc != Creature.player && npc.state != Creature.State.Dead)
                {
                    Vector3 targetPosition = npc.ragdoll.GetPart(HumanBodyBones.Chest).transf.position;
                    Vector3 dir = (contactPoint - targetPosition).normalized;

                    RaycastHit raycastHit;
                    if (Physics.Raycast(contactPoint, dir, out raycastHit, module.hitRadius) && raycastHit.collider.tag != "Player")
                    {
                        Debug.Log(raycastHit.distance);

                        canActivate = false;
                        isActivated = true;

                        LightningFX(targetPosition, npc);

                        if (!npc.ragdoll.effects.Exists(e => e.id.Equals("EscrimaShock")))
                        {
                            EffectShock effectShock = (EffectShock)npc.ragdoll.effects.Find(e => e.id.Equals("Shock"));
                            effectShock.id = "EscrimaShock";
                            effectShock.minConsecutiveHit = 0;
                            npc.ragdoll.effects.Add(effectShock);
                        }
                        
                        CollisionStruct damager = CreateEnemyDamager(raycastHit.collider, dir, targetPosition, raycastHit.normal);
                        npc.health.Damage(ref damager);
                    }
                }
            }

            if (isActivated)
            {
                beamsSound.Play();
                Invoke("StopLightning", module.duration);
            }
        }

        private CollisionStruct CreateEnemyDamager(Collider targetCollider, Vector3 dir, Vector3 targetPosition, Vector3 contactNormal)
        {
            CollisionData collisionData = Catalog.current.GetCollisionData(Animator.StringToHash("SpellLightning (Instance)"), Animator.StringToHash(targetCollider.material.name));
            CollisionStruct collisionStruct = default;
            DamageStruct damageStruct = new DamageStruct(Damager.DamageType.Shock, module.damage);
            damageStruct.recoil = 0;
            damageStruct.knockOutDuration = 0;
            damageStruct.effectId = "EscrimaShock";
            damageStruct.effectRatio = 1;
            Item otherItem = targetCollider.attachedRigidbody ? targetCollider.attachedRigidbody.GetComponent<Item>() : null;
            collisionStruct.NewHit(null, targetCollider, dir, targetPosition, contactNormal, damageStruct, 1, collisionData, null, null, otherItem);
            collisionStruct.damageStruct.hitRagdollPart = collisionStruct.targetCollider.gameObject.GetComponent<RagdollPart>();
            return collisionStruct;
        }

        private void LightningFX(Vector3 targetPosition, Creature target)
        {
            LightningBoltPathScript bolt = Instantiate(boltScript);
            
            GameObject startObject = Instantiate(contactObject);
            startObject.transform.parent = contactObject.transform;
            GameObject targetObject = new GameObject();
            targetObject.transform.position = targetPosition;
            targetObject.transform.parent = target.ragdoll.GetPart(HumanBodyBones.Chest).transf;

            List<GameObject> path = new List<GameObject>();
            path.Add(startObject);
            path.Add(targetObject);
            bolt.LightningPath = path;

            bolts.Add(bolt);
        }

        private void StopLightning()
        {
            foreach (LightningBoltPathScript bolt in bolts)
            {
                foreach (GameObject point in bolt.LightningPath)
                {
                    Destroy(point);
                }
                Destroy(bolt);
            }
            bolts.Clear();

            isActivated = false;
            Invoke("Cooldown", module.cooldownTime);
        }

        private void Cooldown()
        {
            canActivate = true;
        }
    }
}