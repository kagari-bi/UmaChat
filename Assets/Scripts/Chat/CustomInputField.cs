using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;
using System.Text;
using System;
using UnityEngine.Audio;
using System.IO;
using System.Linq;

public class CustomInputField : MonoBehaviour
{
    public TMP_InputField inputField;
    public AudioSource audioSource;
    private string speaker_id;
    private const float typingSpeed = 0.2f;
    private const string serverUrl = "http://127.0.0.1:8000/chat/";
    private string user_id;
    UmaViewerMain Main => UmaViewerMain.Instance;
    private UmaViewerBuilder Builder => UmaViewerBuilder.Instance;

    void Start()
    {
        if (inputField != null)
        {
            inputField.lineType = TMP_InputField.LineType.MultiLineNewline;
            inputField.caretWidth = 0; // �� Caret Width ����Ϊ 0
        }

        // ��������İ�λ�ַ�����Ϊ user_id
        user_id = Guid.NewGuid().ToString("N").Substring(0, 8);

        // �ڳ����в������� GameObject
        GameObject[] allGameObjects = GameObject.FindObjectsOfType<GameObject>();
        // �������е� GameObject
        foreach (GameObject go in allGameObjects)
        {
            // �ж� GameObject �������Ƿ��� "Chara_" ��ͷ
            if (go.name.StartsWith("Chara_"))
            {
                speaker_id = go.name;
            }
        }
    }

    void Update()
    {
        if (inputField == null)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            {
                inputField.text += System.Environment.NewLine;
            }
            else
            {
                string user_question = inputField.text;
                StartCoroutine(SubmitForm(user_question));
            }
        }
    }

    IEnumerator SubmitForm(string user_question)
    {
        inputField.text = "����";

        // ��������
        UnityWebRequest www = new UnityWebRequest(serverUrl, "POST");
        www.SetRequestHeader("Content-Type", "application/json;charset=UTF-8");

        InputData inputData = new InputData { speaker_id = speaker_id, user_question = user_question, user_id = user_id };
        string jsonData = JsonUtility.ToJson(inputData);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
        www.uploadHandler = new UploadHandlerRaw(bodyRaw);
        www.downloadHandler = new DownloadHandlerBuffer();

        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.Log(www.error);
        }
        else
        {
            string response = www.downloadHandler.text;
            // �������ص��ı�����Ƶ����
            var jsonResponse = JsonUtility.FromJson<ResponseData>(response);
            string text = jsonResponse.answer;
            string audioData = jsonResponse.audio_base64;
            byte[] audioBytes = Convert.FromBase64String(audioData);
            string action = jsonResponse.action;
            string expression = jsonResponse.expression;
            float expression_weight = jsonResponse.expression_weight;

            inputField.text = "";

            bool textFinished = false;
            bool audioFinished = false;

            // ��ʼ���ŵ�һ������
            LoadAnimationForCurrentCharacter(action);

            StartCoroutine(TypeText(text, () => {
                textFinished = true;
                if (audioFinished)
                {
                    StartCoroutine(DelayedClearText(1.0f));
                    //// ��ʼ���ŵڶ�������
                    //LoadAnimationForCurrentCharacter("SecondAnimationName");
                }
            }));

            StartCoroutine(PlayAudio(audioBytes, () => {
                audioFinished = true;
                if (textFinished)
                {
                    StartCoroutine(DelayedClearText(1.0f));
                    //// ��ʼ���ŵڶ�������
                    //LoadAnimationForCurrentCharacter("SecondAnimationName");
                }
            }));
        }
    }

    [Serializable]
    public class InputData
    {
        public string speaker_id;
        public string user_question;
        public string user_id; // ��� user_id
    }

    IEnumerator TypeText(string text, Action onFinished)
    {
        inputField.text = "";
        foreach (char c in text)
        {
            inputField.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }
        onFinished?.Invoke();
    }

    IEnumerator PlayAudio(byte[] audioBytes, Action onFinished)
    {
        // ���ֽ�����ת��Ϊ float[]
        float[] floatArray = new float[audioBytes.Length / 4];
        Buffer.BlockCopy(audioBytes, 0, floatArray, 0, audioBytes.Length);

        // �� float[] ת��Ϊ AudioClip
        AudioClip audioClip = AudioClip.Create("responseAudio", floatArray.Length, 1, 22050, false);
        audioClip.SetData(floatArray, 0);

        // ������Ƶ
        audioSource.clip = audioClip;
        audioSource.Play();

        // �ȴ���Ƶ���Ž���
        yield return new WaitForSeconds(audioClip.length);

        onFinished?.Invoke();
    }

    IEnumerator DelayedClearText(float delay)
    {
        yield return new WaitForSeconds(delay);
        inputField.text = "";
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

[Serializable]
public class ResponseData
{
    public string answer;
    public string audio_base64;
    public string action;
    public string expression;
    public int expression_weight;
}