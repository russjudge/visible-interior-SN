using BepInEx;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static HandReticle;

namespace VisibleLockerInterior
{
    internal class Controller
    {
        private const int BaselineItemsPerShelf = 8;
        private const string interiorName = "mod_LockerInterior";
        private const int Shelves = 6;
        private const float ShelfWidth = 1.072f;  //Will not change regardless of changes to number of slots in locker.
       
        private static int totalSlots = 48;
        //Shelves are at varying positions--they aren't consistent enough to use mathematics to calculate.
        private static readonly float[] ShelfPosition =
            { 1.58803f, 1.32938f, 0.966707f, 0.57335f, 0.288304f, 0.046258f };

        private const float zPos = -0.03f;

        public static void UpdateInterior(StorageContainer sc)
        {
            if ("Locker(Clone)" != sc.prefabRoot.name) return;
            Plugin.Log(BepInEx.Logging.LogLevel.Info, $"VisibleLockerInterior: UpdateInterior called for {sc.prefabRoot.name}. Storage Container height={sc.height}, width={sc.width}, internal container size={sc.container.sizeX}x{sc.container.sizeY}");
           

            //Changing to calculate positions instead of using hard-coded arrays.
            // This is to attempt to support mods that change locker size.
            //  For a test case, am using "Ramune's Customized Storage" https://www.nexusmods.com/subnautica/mods/2488

            
            var interior = GetInteriorInstance(sc);
            //Original hard-coded to 8 items on a row, but the storage has only 6 items on a row.
            //However, the visual of the free-standing locker shows only 6 rows, not 8, so to be able
            // to render 48 items, each shelf must have 8 items on it.
            //int totalSlots = sc.container.sizeX * sc.container.sizeY;

            int itemsPerShelf = totalSlots / Shelves;

            float itemRowSpacing = ShelfWidth / itemsPerShelf;
            


            var items = GetSortedItems(sc.storageRoot.gameObject);
            var dummies = GetSortedDummies(interior);
            for (int i = 0, j = 0; (i < items.Count && i < totalSlots) || (j < dummies.Count && j < totalSlots);)
            {
                float x = -((i % itemsPerShelf) * itemRowSpacing - (ShelfWidth / 2 - itemRowSpacing / 2));

                float y = ShelfPosition[(i / itemsPerShelf)];

                var targetPosition =
                    new Vector3(x, y, zPos);
                int cmp =
                    i == items.Count ? 1 :
                    j == dummies.Count ? -1 :
                    CompareTechType(
                        items[i].GetComponent<Pickupable>().GetTechType(),
                        dummies[j].GetComponent<VisibleLockerInteriorDummyData>().techType
                        );
                if (cmp == -1)
                {
                    var dummy = CreateDummy(interior, items[i]);
                    RepositionDummy(dummy.gameObject, targetPosition, itemsPerShelf);
                    i++;
                }
                else if (cmp == 0)
                {
                    RepositionDummy(dummies[j].gameObject, targetPosition, itemsPerShelf);
                    i++; j++;
                }
                else
                {
                    GameObject.Destroy(dummies[j].gameObject);
                    j++;
                }
            }
        }

     
        private static GameObject CreateDummy(GameObject interior, GameObject src)
        {
            var dummy = GameObject.Instantiate(src, interior.transform);
            var techType = src.GetComponent<Pickupable>()?.GetTechType() ?? TechType.None;
            var classId = src.GetComponent<PrefabIdentifier>()?.ClassId ?? "";
            dummy.SetActive(true);

            SanitizeObject(dummy, techType);
            if (techType == TechType.WiringKit || techType == TechType.AdvancedWiringKit)
            {
                Plugin.Log(BepInEx.Logging.LogLevel.Debug, $"Creating dummy, techType={techType}, classId = {classId}");
                Plugin.Log(BepInEx.Logging.LogLevel.Debug, $"gameObject Name={dummy.gameObject.name}");
                Plugin.Log(BepInEx.Logging.LogLevel.Debug, $"gameObject.transform.name={dummy.gameObject.transform.name}");
                
            }
            var dummyComp = dummy.AddComponent<VisibleLockerInteriorDummyData>();
            dummyComp.techType = techType;
            dummyComp.prefabId = classId;
            return dummy;
        }

        private static void RepositionDummy(GameObject dummy, Vector3 targetPosition, int itemsPerShelf)
        {
            var bounds = GetIdealBounds(dummy);

            const float magicNumber = 0.13f;  //Original programmer used this figure--for unknown reason.  That's why it's magic.


            dummy.transform.localRotation = GetIdealRotation(dummy);

            float xScale = 0.01625f * itemsPerShelf;

            //was hard-coded to: 0.13, 0.14, 0.27
            float scale = new[] {
                magicNumber / (bounds.size.x * (itemsPerShelf / BaselineItemsPerShelf)),
                (magicNumber + 0.01f) / (bounds.size.y * (itemsPerShelf / BaselineItemsPerShelf)),
                (magicNumber * 2 + 0.01f) / (bounds.size.z * (itemsPerShelf / BaselineItemsPerShelf))
            }.Min() * GetIdealDeltaScale(dummy);

            var offset = -bounds.center + bounds.extents.y * Vector3.up;

            dummy.transform.localScale = scale * Vector3.one;

            dummy.transform.localPosition = targetPosition + offset * scale;

            Plugin.Log(BepInEx.Logging.LogLevel.Info, $"Placed {dummy.name}, scale={scale}");
        }

        private static void SanitizeObject(GameObject obj, TechType techType)
        {
            if (!obj.activeSelf || obj.name.StartsWith("x"))
            {
                GameObject.Destroy(obj);
                return;
            }

            var destroyList = new List<Component>();
            do
            {
                destroyList.Clear();
                destroyList.AddRange(obj.GetComponents<Collider>());
                destroyList.AddRange(obj.GetComponents<Rigidbody>());
                destroyList.AddRange(obj.GetComponents<ParticleSystem>());
                foreach (var r in obj.GetComponents<Renderer>())
                {
                    if (r is MeshRenderer || r is SkinnedMeshRenderer)
                    {
                        continue;
                    }
                    else
                    {
                        destroyList.Add(r);
                    }
                }
                foreach (var b in obj.GetComponents<Behaviour>())
                {
                    if (b is SkyApplier)
                    {
                        b.enabled = true;
                    }
                    else if (b is Animator && Quirk.MustHaveAnimator(techType))
                    {
                        b.enabled = false;
                    }
                    else
                    {
                        destroyList.Add(b);
                    }
                }
                destroyList.Reverse();
                foreach (var comp in destroyList)
                {
                    GameObject.DestroyImmediate(comp);
                }
            } while (destroyList.Count > 0);

            if (Quirk.IsKelp(techType))
                foreach (var r in obj.GetComponents<Renderer>())
                {
                    foreach (var m in r.materials)
                    {
                        m.DisableKeyword("FX_KELP");
                    }
                }

            foreach (Transform childTransform in obj.transform)
            {
                SanitizeObject(childTransform.gameObject, techType);
            }
        }

        private static Quaternion GetIdealRotation(GameObject dummy)
        {
            var comp = dummy.GetComponent<VisibleLockerInteriorDummyData>();
            return Quirk.overrideRotation.ContainsKey(comp.prefabId) ?
                Quirk.overrideRotation[comp.prefabId] :
                Quirk.GetIdealRotationByTechType(comp.techType);
        }

        private static Bounds GetIdealBounds(GameObject dummy)
        {
            var comp = dummy.GetComponent<VisibleLockerInteriorDummyData>();
            if (Quirk.overrideBounds.ContainsKey(comp.prefabId))
                return Quirk.overrideBounds[comp.prefabId];

            var currentRot = dummy.transform.rotation;

            var currentScale = dummy.transform.localScale;

            var currentPos = dummy.transform.localPosition;

            dummy.transform.rotation = GetIdealRotation(dummy);

            dummy.transform.localScale = Vector3.one;

            dummy.transform.position = Vector3.zero;
            var renderers = dummy.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
                return new Bounds(dummy.transform.position, Vector3.zero);
            var b = renderers[0].bounds;

            foreach (Renderer r in renderers)
            {
                if (r is MeshRenderer || r is SkinnedMeshRenderer)
                {
                    b.Encapsulate(r.bounds);
                }
            }
            dummy.transform.rotation = currentRot;

            dummy.transform.localScale = currentScale;

            dummy.transform.position = currentPos;
            return Quirk.overrideBounds[comp.prefabId] = b;
        }

        private static float GetIdealDeltaScale(GameObject dummy)
        {
            var comp = dummy.GetComponent<VisibleLockerInteriorDummyData>();
            return Quirk.overrideDeltaScale.ContainsKey(comp.prefabId) ?
                Quirk.overrideDeltaScale[comp.prefabId] :
                1;
        }

        private static GameObject GetInteriorInstance(StorageContainer sc)
        {
            var locker = sc.prefabRoot;
            var interiorTransform = locker.transform.Find(interiorName);
            if (interiorTransform) return interiorTransform.gameObject;
            var interiorObject = new GameObject(interiorName);
            interiorObject.transform.SetParent(locker.transform);
            interiorObject.transform.localRotation = Quaternion.identity;
            interiorObject.transform.localPosition = Vector3.zero;
            return interiorObject;
        }

        private static List<GameObject> GetSortedItems(GameObject storageRoot)
        {
            var result = new List<GameObject>(storageRoot.transform.childCount);
            foreach (Transform item in storageRoot.transform)
            {
                if (!item.gameObject.GetComponent<Pickupable>()) continue;
                result.Add(item.gameObject);
            }
            result.Sort(
                (g1, g2) =>
                CompareTechType(
                    g1.GetComponent<Pickupable>().GetTechType(),
                    g2.GetComponent<Pickupable>().GetTechType()
                )
            );
            return result;
        }

        private static List<GameObject> GetSortedDummies(GameObject lockerInterior)
        {
            var result = new List<GameObject>(lockerInterior.transform.childCount);
            foreach (Transform dummyTransform in lockerInterior.transform)
                result.Add(dummyTransform.gameObject);
            result.Sort(
                (g1, g2) =>
                CompareTechType(
                    g1.GetComponent<VisibleLockerInteriorDummyData>().techType,
                    g2.GetComponent<VisibleLockerInteriorDummyData>().techType
                )
            );
            return result;
        }

        private static int CompareTechType(TechType t1, TechType t2)
        {
            return t1.CompareTo(t2);

            //As of the August 2025 Subnautica update, "GetItemSize" is no longer available.
            //This method appears to only be used for sorting items in the locker interior,
            // so the impact of commenting out below should be minimal.

            // Original code retained for reference.
            //var size1 = CraftData.GetItemSize(t1);
            //var size2 = CraftData.GetItemSize(t2);
            //if (size1.Equals(size2)) return t1.CompareTo(t2);
            //var l1 = Math.Max(size1.x, size1.y);
            //var l2 = Math.Max(size2.x, size2.y);
            //if (l1 != l2) return l2.CompareTo(l1);
            //var a1 = size1.x * size1.y;
            //var a2 = size2.x * size2.y;
            //return a1 == a2 ? size2.y.CompareTo(size1.y) : a2.CompareTo(a1);
        }
    }

    internal class VisibleLockerInteriorDummyData : MonoBehaviour
    {
        public TechType techType;
        public string prefabId;
    }
   

}
