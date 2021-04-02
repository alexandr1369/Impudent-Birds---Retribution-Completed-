using UnityEngine;
using Pixelplacement;

public class BirdUI : MonoBehaviour
{
    private void Falling()
    {
        Vector3 parentPosition = transform.parent.position;
        Tween.Position(transform.parent, new Vector3(parentPosition.x, parentPosition.y + 2f, parentPosition.z), .5f, 0);
    }
    private void GettingUp()
    {
        Vector3 parentPosition = transform.parent.position;
        Tween.Position(transform.parent, new Vector3(parentPosition.x, parentPosition.y - 2f, parentPosition.z), .5f, 0);
    }
}
