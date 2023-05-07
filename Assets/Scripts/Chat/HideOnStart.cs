using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HideOnStart : MonoBehaviour
{
    // �ڴ��б������Ҫ�ڿ�ʼʱ���ص���Ϸ����
    [SerializeField] private List<GameObject> gameObjectsToHide;

    private void Start()
    {
        HideObjects();
    }

    private void HideObjects()
    {
        foreach (GameObject go in gameObjectsToHide)
        {
            if (go != null)
            {
                go.SetActive(false);
            }
        }
    }
}
