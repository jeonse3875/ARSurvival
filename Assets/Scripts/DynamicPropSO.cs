using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "DynamicProp")]
public class DynamicPropSO : ScriptableObject
{
    public GameObject prefab;
    public float volumePerObj; // 오브젝트 1 : f 볼륨
    public float placingHeight; // 설치할 높이 align용
    public int minCount;
    public int maxCount;
    public Vector2 posXRange; // 설치할 좌우 위치 범위 -1f ~ 1f
    public Vector2 posZRange; // 설치할 앞뒤 위치 범위 -1f ~1f
    public float minGap; // 모든 방향으로의 최소 간격

    public int GetSpawnCount(float volume)
    {
        return Mathf.Clamp(Mathf.FloorToInt(volume/volumePerObj),minCount,maxCount);
    }

    public List<Vector3> GetSpawnPos(OBB arg, int count)
    {
        var result = new List<Vector3>();

        for (int i = 0; i < count; i++)
        {
            var relX = Random.Range(posXRange.x,posXRange.y);
            var relZ = Random.Range(posZRange.x,posZRange.y);
            var pos = arg.center + arg.axisX * arg.extent.x * relX + arg.axisZ * arg.extent.z * relZ;
            pos.y = placingHeight;

            bool isFarEnough = true;
            foreach(var prePos in result)
            {
                if (Vector3.Distance(prePos,pos) < minGap)
                {
                    isFarEnough = false;
                    break;
                }
            }

            if (isFarEnough) {result.Add(pos);}
        }

        return result;
    }
}
