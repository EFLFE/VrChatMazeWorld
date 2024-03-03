using UdonSharp;
using UnityEngine;

public class BaseRoom : UdonSharpBehaviour
{
    [SerializeField] private GameObject topDoorBlocker;
    [SerializeField] private GameObject bottomDoorBlocker;
    [SerializeField] private GameObject leftDoorBlocker;
    [SerializeField] private GameObject rightDoorBlocker;

    public void OpenTop()
    {
        topDoorBlocker.SetActive(false);
    }

    public void OpenBottom()
    {
        bottomDoorBlocker.SetActive(false);
    }

    public void OpenLeft()
    {
        leftDoorBlocker.SetActive(false);
    }

    public void OpenRight()
    {
        rightDoorBlocker.SetActive(false);
    }

}
