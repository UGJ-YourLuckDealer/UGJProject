
using SaintsField;
using UnityEngine;
using UnityEngine.UI;

public class ImageOutline : MonoBehaviour
{
    const string OutlineAlphaShaderParam = "_OutlineAlpha";

    [SerializeField] Image _image;

    public void Show_Outline()
    {
        _image.material.SetFloat(OutlineAlphaShaderParam, 1f);
    }

    public void Hide()
    {
        _image.material.SetFloat(OutlineAlphaShaderParam, 0f);
    }
}
