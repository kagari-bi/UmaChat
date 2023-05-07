using UnityEngine;
using UnityEngine.EventSystems;

public class DraggableDialogBox : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public GameObject dialogBox;
    private bool isDragging = false;
    private Vector3 offset;

    void Update()
    {
        if (isDragging)
        {
            // ����������λ�ò�����dialogBox��λ��
            Vector3 newPosition = Input.mousePosition + offset;
            dialogBox.transform.position = newPosition;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        // ����Ƿ�������м�
        if (eventData.button == PointerEventData.InputButton.Middle)
        {
            // ������������dialogBox��λ��ƫ��
            offset = dialogBox.transform.position - Input.mousePosition;
            isDragging = true;
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        // ����Ƿ��ɿ�����м�
        if (eventData.button == PointerEventData.InputButton.Middle)
        {
            isDragging = false;
        }
    }
}
