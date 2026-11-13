
using SaintsField;
using UnityEngine;
using UnityEngine.UI;

public class ImageOutline : MonoBehaviour
{
    const string OutlineAlphaShaderParam = "_OutlineAlpha";

    [SerializeField] Image _image;

    Material _materialInst;

    void Awake()
    {
        _materialInst = new Material(_image.material);
        _image.material = _materialInst;
    }

    public void Show_Outline()
    {
        _image.material.SetFloat(OutlineAlphaShaderParam, 1f);
    }

    public void Hide()
    {
        _image.material.SetFloat(OutlineAlphaShaderParam, 0f);
    }

    void OnDestroy()
    {
        if (_materialInst != null)
        {
            Destroy(_materialInst);
        }
    }
}
