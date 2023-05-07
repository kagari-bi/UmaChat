using System.Linq;
using UnityEngine;
using System.IO;

public class AnimationChange : MonoBehaviour
{
    UmaViewerMain Main => UmaViewerMain.Instance;
    private UmaViewerBuilder Builder => UmaViewerBuilder.Instance;

    void Start()
    {
        //LoadAnimationForCurrentCharacter("anm_eve_type00_hurry01_loop");
    }

    public void LoadAnimationForCurrentCharacter(string animationName)
    {
        if (Builder.CurrentUMAContainer == null)
        {
            Debug.LogError("No character is currently loaded.");
            return;
        }

        // ���Ұ������趯������Դ��
        UmaDatabaseEntry animationEntry = Main.AbMotions.FirstOrDefault(entry => Path.GetFileName(entry.Name) == animationName);

        if (animationEntry == null)
        {
            Debug.LogError($"Animation '{animationName}' not found in the available assets.");
            return;
        }

        // ����Դ���м��ض�������
        AssetBundle bundle;

        // �����Դ���Ƿ��Ѿ����أ����û�м��أ��������Դ��
        if (!Main.LoadedBundles.TryGetValue(animationEntry.Name, out bundle))
        {
            // ���ذ���������������Դ��
            Builder.LoadAsset(animationEntry);

            if (Main.LoadedBundles.TryGetValue(animationEntry.Name, out bundle))
            {
                LoadAnimationClipFromBundle(animationName, animationEntry, bundle);
            }
            //else
            //{
            //    Debug.LogError($"Failed to find loaded AssetBundle: {animationEntry.Name}");
            //}
        }
        else
        {
            LoadAnimationClipFromBundle(animationName, animationEntry, bundle);
        }
    }

    private void LoadAnimationClipFromBundle(string animationName, UmaDatabaseEntry animationEntry, AssetBundle bundle)
    {
        AnimationClip animationClip = bundle.LoadAsset<AnimationClip>(animationEntry.Name);
        if (animationClip != null)
        {
            // ʹ�����е�LoadAnimation()�������ض�������
            Builder.LoadAnimation(animationClip);
        }
        else
        {
            Debug.LogError($"Failed to load AnimationClip from the AssetBundle: {animationEntry.Name}");
        }
    }
}