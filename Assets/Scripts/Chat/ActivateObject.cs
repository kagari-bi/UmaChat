using UnityEngine;
using UnityEngine.UI;

public class ActivateObject : MonoBehaviour
{
    // ����һ�������������Ա���Unity�༭���н������ӵ�GameObject
    public GameObject targetObject;

    private Button button;

    void Start()
    {
        // ��ȡButton���
        button = GetComponent<Button>();

        // ��ӵ���¼�
        button.onClick.AddListener(ActivateTargetObject);

        // ��ʼʱ����Ŀ�����
        targetObject.SetActive(false);
    }

    // ��ť����¼�������
    void ActivateTargetObject()
    {
        targetObject.SetActive(true);
    }
}
