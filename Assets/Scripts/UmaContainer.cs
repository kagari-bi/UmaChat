using Gallop;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using UnityEngine;

public class UmaContainer : MonoBehaviour
{
    // ���MeshCollider����
    private MeshCollider meshCollider;

    public DataRow CharaData;
    public GameObject Body;
    public GameObject Tail;
    public GameObject Head;
    public GameObject Hair;

    public List<Texture2D> TailTextures = new List<Texture2D>();

    [Header("Animator")]
    public Animator UmaAnimator;
    public AnimatorOverrideController OverrideController;
    public Animator UmaFaceAnimator;
    public AnimatorOverrideController FaceOverrideController;
    public bool isAnimatorControl;

    [Header("Body")]
    public GameObject UpBodyBone;
    public Vector3 UpBodyPosition;
    public Quaternion UpBodyRotation;

    [Header("Face")]
    public FaceDrivenKeyTarget FaceDrivenKeyTarget;
    public FaceEmotionKeyTarget FaceEmotionKeyTarget;
    public FaceOverrideData FaceOverrideData;
    public GameObject HeadBone;
    public GameObject TrackTarget;
    public float EyeHeight;
    public bool EnableEyeTracking = true;
    public Material FaceMaterial;

    [Header("Cheek")]
    public Texture CheekTex_0;
    public Texture CheekTex_1;
    public Texture CheekTex_2;

    [Header("Manga")]
    public List<GameObject> LeftMangaObject = new List<GameObject>();
    public List<GameObject> RightMangaObject = new List<GameObject>();

    [Header("Tear")]
    public GameObject TearPrefab_0;
    public GameObject TearPrefab_1;
    public List<TearController> TearControllers = new List<TearController>();

    [Header("Generic")]
    public bool IsGeneric = false;
    public string VarCostumeIdShort, VarCostumeIdLong, VarSkin, VarHeight, VarSocks, VarBust;
    public List<Texture2D> GenericBodyTextures = new List<Texture2D>();

    [Header("Mini")]
    public bool IsMini = false;
    public List<Texture2D> MiniHeadTextures = new List<Texture2D>();

    [Header("Mob")]
    public bool IsMob = false;
    public DataRow MobDressColor;
    public DataRow MobHeadColor;
    public List<Texture2D> MobHeadTextures = new List<Texture2D>();

    [Header("Physics")]
    public bool EnablePhysics = true;
    public List<CySpringDataContainer> cySpringDataContainers;
    public GameObject PhysicsContainer;
    public float BodyScale = 1;

    [Header("Other")]
    public CharaShaderEffectData ShaderEffectData;

    public void Initialize()
    {
        // ���MeshCollider���
        meshCollider = gameObject.AddComponent<MeshCollider>();
        meshCollider.sharedMesh = GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh;

        TrackTarget = Camera.main.gameObject;
        UpBodyPosition = UpBodyBone.transform.localPosition;
        UpBodyRotation = UpBodyBone.transform.localRotation;

        //Models must be merged before handling extra morphs
        if (FaceDrivenKeyTarget)
            FaceDrivenKeyTarget.ChangeMorphWeight(FaceDrivenKeyTarget.MouthMorphs[3], 1);
    }

    // ��������ק����
    private Vector3 screenPoint;
    private Vector3 offset;
    private bool isMiddleMouseButtonDown = false;

    private void Update()
    {
        if (Input.GetMouseButtonDown(2))
        {
            OnMiddleMouseDown();
            isMiddleMouseButtonDown = true;
        }
        else if (Input.GetMouseButtonUp(2))
        {
            isMiddleMouseButtonDown = false;
        }

        if (isMiddleMouseButtonDown)
        {
            OnMiddleMouseDrag();
        }
    }

    private void OnMiddleMouseDown()
    {
        screenPoint = Camera.main.WorldToScreenPoint(transform.position);
        offset = transform.position - Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, screenPoint.z));
    }

    private void OnMiddleMouseDrag()
    {
        Vector3 curScreenPoint = new Vector3(Input.mousePosition.x, Input.mousePosition.y, screenPoint.z);
        Vector3 curPosition = Camera.main.ScreenToWorldPoint(curScreenPoint) + offset;
        transform.position = curPosition;
    }

    public void MergeModel()
    {
        if (!Body) return;

        List<Transform> bodybones = new List<Transform>(Body.GetComponentInChildren<SkinnedMeshRenderer>().bones);
        List<Transform> emptyBones = new List<Transform>();
        emptyBones.Add(Body.GetComponentInChildren<SkinnedMeshRenderer>().rootBone.Find("Tail_Ctrl"));
        while (Body.transform.childCount > 0)
        {
            var child = Body.transform.GetChild(0);
            child.SetParent(transform);
        }
        Body.SetActive(false); //for debugging


        //MergeHead
        if (Head)
        {
            var headskins = Head.GetComponentsInChildren<SkinnedMeshRenderer>();
            foreach (SkinnedMeshRenderer headskin in headskins)
            {
                emptyBones.AddRange(MergeBone(headskin, bodybones));
            }
            var eyes = new GameObject("Eyes");
            eyes.transform.SetParent(transform);
            while (Head.transform.childCount > 0)
            {
                var child = Head.transform.GetChild(0);
                child.SetParent(child.name.Contains("info") ? eyes.transform : transform);
            }
            Head.SetActive(false); //for debugging
        }


        //MergeTail
        if (Tail)
        {
            var tailskin = Tail.GetComponentInChildren<SkinnedMeshRenderer>();
            emptyBones.AddRange(MergeBone(tailskin, bodybones));
            while (Tail.transform.childCount > 0)
            {
                var child = Tail.transform.GetChild(0);
                child.SetParent(transform);
            }
            Tail.SetActive(false); //for debugging
        }


        emptyBones.ForEach(a => { if (a) Destroy(a.gameObject); });

        //MergeAvatar
        UmaAnimator = gameObject.AddComponent<Animator>();
        UmaAnimator.avatar = AvatarBuilder.BuildGenericAvatar(gameObject, gameObject.name);
        OverrideController = Instantiate(UmaViewerBuilder.Instance.OverrideController);
        UmaAnimator.runtimeAnimatorController = OverrideController;
    }

    public void MergeHairModel()
    {
        if (!Head || !Hair) return;

        List<Transform> bodybones = new List<Transform>(Head.GetComponentsInChildren<Transform>());
        List<Transform> emptyBones = new List<Transform>();

        var headHolder = Head.GetComponent<AssetHolder>();
        var hairHolder = Hair.GetComponent<AssetHolder>();

        headHolder._assetTable.list.AddRange(hairHolder._assetTable.list);
        headHolder._assetTableValue.list.AddRange(hairHolder._assetTableValue.list);
        
        //MergeHair
        var hairskins = Hair.GetComponentsInChildren<SkinnedMeshRenderer>();
        foreach (SkinnedMeshRenderer hairskin in hairskins)
        {
            hairskin.gameObject.transform.SetParent(Head.transform);
            emptyBones.AddRange(MergeBone(hairskin, bodybones));
        }
        Hair.gameObject.SetActive(false);

        emptyBones.ForEach(a => { if (a) Destroy(a.gameObject); });
    }

    public void SetHeight(int scale = 0)
    {
        if (scale == 0)
        {
            BodyScale = 1;
        }
        else if (scale == -1)
        {
            BodyScale = (Convert.ToInt32(CharaData[IsMob ? "chara_height" : "scale"]) / 160f);
        }
        else
        {
            BodyScale = (scale / 160f);
        }
        transform.Find("Position").localScale = new Vector3(BodyScale, BodyScale, BodyScale);
    }

    public Transform[] MergeBone(SkinnedMeshRenderer from, List<Transform> targetBones)
    {
        var rootbone = targetBones.FindLast(a => a.name.Equals(from.rootBone.name));
        if (rootbone) from.rootBone = rootbone;

        List<Transform> emptyBones = new List<Transform>();
        Transform[] tmpBone = new Transform[from.bones.Length];
        for (int i = 0; i < tmpBone.Length; i++)
        {
            var targetbone = targetBones.FindLast(a => a.name.Equals(from.bones[i].name));
            if (targetbone)
            {
                tmpBone[i] = targetbone;
                from.bones[i].position = targetbone.position;
                while (from.bones[i].transform.childCount > 0)
                {
                    from.bones[i].transform.GetChild(0).SetParent(targetbone);
                }
                emptyBones.Add(from.bones[i]);
            }
            else
            {
                tmpBone[i] = from.bones[i];
            }
        }
        from.bones = tmpBone;
        return emptyBones.ToArray();
    }

    public void LoadPhysics()
    {
        cySpringDataContainers = new List<CySpringDataContainer>(PhysicsContainer.GetComponentsInChildren<CySpringDataContainer>());
        var bones = new List<Transform>(GetComponentsInChildren<Transform>());
        var colliders = new List<GameObject>();

        foreach (CySpringDataContainer spring in cySpringDataContainers)
        {
            colliders.AddRange(spring.InitiallizeCollider(bones));
        }
        foreach (CySpringDataContainer spring in cySpringDataContainers)
        {
            spring.InitializePhysics(bones, colliders);
        }
    }

    public void SetDynamicBoneEnable(bool isOn)
    {
        if (IsMini) return;
        EnablePhysics = isOn;
        foreach (CySpringDataContainer cySpring in cySpringDataContainers)
        {
            cySpring.EnablePhysics(isOn);
        }
    }

    private void FixedUpdate()
    {
        if (!IsMini)
        {
            if (TrackTarget && EnableEyeTracking && !isAnimatorControl)
            {
                var targetPosotion = TrackTarget.transform.position - HeadBone.transform.up * EyeHeight;
                var deltaPos = HeadBone.transform.InverseTransformPoint(targetPosotion);
                var deltaRotation = Quaternion.LookRotation(deltaPos.normalized, HeadBone.transform.up).eulerAngles;
                if (deltaRotation.x > 180) deltaRotation.x -= 360;
                if (deltaRotation.y > 180) deltaRotation.y -= 360;

                var finalRotation = new Vector2(Mathf.Clamp(deltaRotation.y / 35, -1, 1), Mathf.Clamp(-deltaRotation.x / 25, -1, 1));//Limited to the angle of view 
                FaceDrivenKeyTarget.SetEyeRange(finalRotation.x, finalRotation.y, finalRotation.x, -finalRotation.y);
            }

            if (isAnimatorControl)
            {
                FaceDrivenKeyTarget.ProcessLocator();
            }

            if (FaceMaterial)
            {
                if (isAnimatorControl)
                {
                    FaceMaterial.SetVector("_FaceForward", Vector3.zero);
                    FaceMaterial.SetVector("_FaceUp", Vector3.zero);
                    FaceMaterial.SetVector("_FaceCenterPos", Vector3.zero);

                }
                else
                {
                    //Used to calculate facial shadows
                    FaceMaterial.SetVector("_FaceForward", HeadBone.transform.forward);
                    FaceMaterial.SetVector("_FaceUp", HeadBone.transform.up);
                    FaceMaterial.SetVector("_FaceCenterPos", HeadBone.transform.position);
                }
                FaceMaterial.SetFloat("_faceShadowEndY", HeadBone.transform.position.y);
            }

            TearControllers.ForEach(a => a.UpdateOffset());

        }
    }

    public void SetNextAnimationCut(string cutName)
    {
        var asset = UmaViewerMain.Instance.AbMotions.FirstOrDefault(a => a.Name.Equals(cutName));
        UmaViewerBuilder.Instance.RecursiveLoadAsset(asset);
    }

    public void SetEndAnimationCut()
    {
        UmaViewerUI.Instance.AnimationPause();
    }

    public void UpBodyReset()
    {
        if (UpBodyBone)
        {
            UpBodyBone.transform.localPosition = UpBodyPosition;
            UpBodyBone.transform.localRotation = UpBodyRotation;
        }
    }
}
