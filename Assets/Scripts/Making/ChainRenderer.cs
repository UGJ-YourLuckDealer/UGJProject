using UnityEngine;

public class ChainRenderer : MonoBehaviour
{
    [SerializeField] Sprite _sprite;
    [SerializeField] Material _material;

    [SerializeField] float _headLength;
    [SerializeField] float _headWidth;

    [SerializeField] float _chainLength;
    [SerializeField] float _chainWidth;

    [SerializeField] int _chainCount;

    Matrix4x4[] _chainMatrices;

    void Start()
    {
        _chainMatrices = new Matrix4x4[_chainCount];
        for (int i = 0; i < _chainLength; i++)
        {
            _chainMatrices[i] = Matrix4x4.identity;
        }
    }

    void Update()
    {

    }
}