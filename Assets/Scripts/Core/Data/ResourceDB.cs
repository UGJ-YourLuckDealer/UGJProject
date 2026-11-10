using SaintsField;
using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// string을 기반으로 가져올 Resource를 등록하는 데이터
/// 하나의 파일만 사용할 것
/// </summary>
[CreateAssetMenu(fileName = "ResourceDB", menuName = "Scriptable Objects/ResourceDB")]
public class ResourceDB : ScriptableObject
{
    public SaintsDictionary<string, IAudioGenerator> sfxDict;
    public SaintsDictionary<string, AudioClip> bgmDict;
    public SaintsDictionary<string, GameObject> popupDict;
}
