using System;
using System.IO;
using UnityEngine;

namespace HarvestInc
{
    /// <summary>저장 파일 내용. 밭의 작물 상태는 저장하지 않는다(불러오면 새로 자란다).</summary>
    [Serializable]
    public sealed class SaveData
    {
        public int version;
        public long savedAtUtcTicks; // 오프라인 보상 계산용

        // 이번 판
        public double coins;
        public double totalEarned;
        public double fame;
        public int sickleLevel;
        public int fieldIndex;
        public bool[] unlocked;
        public MachineSave[] machines;

        // 개척해도 유지
        public double goldenSeeds;
        public int stage = 1;
        public int bestFieldIndex;
        public bool prestigeOpened;
        public int[] permLevels;
    }

    [Serializable]
    public sealed class MachineSave
    {
        public int count;
        public int damageLevel;
        public int cooldownLevel;
        public bool precision;
    }

    /// <summary>JSON 파일 하나에 저장한다. 위치: Application.persistentDataPath/save.json</summary>
    public static class SaveSystem
    {
        public const int Version = 1;

        public static string FilePath => Path.Combine(Application.persistentDataPath, "save.json");

        public static bool TryLoad(out SaveData data)
        {
            data = null;
            string path = FilePath;
            try
            {
                if (!File.Exists(path)) return false;
                data = JsonUtility.FromJson<SaveData>(File.ReadAllText(path));
                if (data == null || data.version < 1 || data.version > Version)
                    throw new InvalidDataException($"알 수 없는 저장 버전: {data?.version}");
                return true;
            }
            catch (Exception e)
            {
                // 깨진 파일은 덮어쓰기 전에 옆에 남겨 둔다
                Debug.LogWarning($"[Save] 불러오기 실패, 새 게임으로 시작: {e.Message}");
                try { File.Copy(path, path + ".bad", true); } catch { /* 백업 실패는 무시 */ }
                data = null;
                return false;
            }
        }

        public static bool Write(SaveData data)
        {
            string path = FilePath;
            string tmp = path + ".tmp";
            try
            {
                data.version = Version;
                data.savedAtUtcTicks = DateTime.UtcNow.Ticks;
                // 임시 파일에 다 쓴 다음 바꿔치기 → 쓰는 도중 앱이 꺼져도 기존 저장이 깨지지 않는다
                File.WriteAllText(tmp, JsonUtility.ToJson(data, true));
                File.Copy(tmp, path, true);
                File.Delete(tmp);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Save] 저장 실패: {e.Message}");
                return false;
            }
        }
    }
}
